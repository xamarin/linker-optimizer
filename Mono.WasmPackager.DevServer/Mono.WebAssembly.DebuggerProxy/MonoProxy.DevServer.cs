using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WebAssembly.Net.Debugging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Mono.WasmPackager.DevServer;
using P = PuppeteerSharp;

namespace WebAssembly.Net.Debugging {
	partial class MonoProxy : IMonoProxy {
		NewDevToolsProxy proxy;
		AbstractConnection ideConnection;
		AbstractConnection browserConnection;

		ILogger logger;

		internal MonoProxy (AbstractConnection browserConnection, AbstractConnection ideConnection, NewDevToolsProxy proxy, ILoggerFactory loggerFactory)
		{
			this.browserConnection = browserConnection;
			this.ideConnection = ideConnection;
			this.proxy = proxy;

			logger = loggerFactory.CreateLogger<MonoProxy> ();
			hideWebDriver = true;
		}

		Task IMonoProxy.SendEvent (SessionId sessionId, string method, JObject args, CancellationToken token)
		{
			return proxy.SendEvent (sessionId, method, args, token);
		}

		Task SendEvent (SessionId sessionId, string method, JObject args, CancellationToken token)
		{
			return proxy.SendEvent (sessionId, method, args, token);
		}

		Task<Result> SendCommand (SessionId id, string method, JObject args, CancellationToken token)
		{
			return proxy.SendCommand (id, method, args, token);
		}

		Task SendResponse (MessageId id, Result result, CancellationToken token)
		{
			return proxy.SendResponse (id, result, token);
		}

		internal void AcceptEvent (SessionId sessionId, ConnectionEventArgs eventArgs)
		{
			var result = AcceptEvent (sessionId, eventArgs.Message, eventArgs.Arguments);
			eventArgs.Handler = result.Handler;
			eventArgs.SkipEvent = result.IgnoreCommand;
			if (result.HasResult)
				throw new InvalidOperationException ();
		}

		CommandResult AcceptEvent (SessionId sessionId, string message, JObject args)
		{
			switch (message) {
			case "Runtime.consoleAPICalled": {
					var type = args["type"]?.ToString ();
					if (type == "debug") {
						if (args["args"]?[0]?["value"]?.ToString () == MonoConstants.RUNTIME_IS_READY && args["args"]?[1]?["value"]?.ToString () == "fe00e07a-5519-4dfe-b35a-f867dbaf2e28")
							return CommandResult.Async (token => RuntimeReady (sessionId, token));
					}
					break;
				}

			case "Runtime.executionContextCreated": {
					return CommandResult.Async (token => OnExecutionContextCreated (sessionId, message, args, token));
				}

			case "Debugger.paused": {
					//TODO figure out how to stich out more frames and, in particular what happens when real wasm is on the stack
					var top_func = args? ["callFrames"]? [0]? ["functionName"]?.Value<string> ();

					if (IsFireBreakpointFunction (top_func)) {
						return CommandResult.Async (token => OnBreakpointHit (sessionId, args, token));
					}
					break;
				}

			case "Debugger.breakpointResolved": {
					break;
				}

			case "Debugger.scriptParsed":{
					var url = args? ["url"]?.Value<string> () ?? "";

					switch (url) {
					case var _ when url == "":
					case var _ when url.StartsWith ("wasm://", StringComparison.Ordinal): {
							Log ("verbose", $"ignoring wasm: Debugger.scriptParsed {url}");
							return CommandResult.Complete;
						}
					}
					Log ("verbose", $"proxying Debugger.scriptParsed ({sessionId.sessionId}) {url} {args}");
					break;
				}
			}

			return CommandResult.Proxy;
		}

		internal void AcceptCommand (MessageId id, ConnectionEventArgs eventArgs)
		{
			var result = AcceptCommand (id, eventArgs.Message, eventArgs.Arguments);
			eventArgs.Handler = result.Handler;
			eventArgs.SkipEvent = result.IgnoreCommand;
			if (result.HasResult)
				eventArgs.Handler = token => SendResponse (id, result.Result, token);
		}

		CommandResult Async (MessageId id, string method, JObject args, ExecutionContext context, Func<MessageId, string, JObject, ExecutionContext, CancellationToken, Task<Result>> func)
		{
			return CommandResult.Async (async token => {
				var result = await func (id, method, args, context, token).ConfigureAwait (false);
				await proxy.SendResponse (id, result, token);
			});
		}

		CommandResult Async (MessageId id, string method, JObject args, ExecutionContext context, Func<CancellationToken, Task<Result>> func)
		{
			return CommandResult.Async (async token => {
				var result = await func (token).ConfigureAwait (false);
				await proxy.SendResponse (id, result, token);
			});
		}

		CommandResult Async (MessageId id, string method, JObject args, ExecutionContext context, Func<MessageId, string, JObject, ExecutionContext, CancellationToken, Task<bool>> func)
		{
			return CommandResult.Async (async token => {
				if (await func (id, method, args, context, token).ConfigureAwait (false))
					return;
				var res = await proxy.SendCommand (id, method, args, token);
				await proxy.SendResponse (id, res, token);
			});
		}

		CommandResult Async (MessageId id, string method, JObject args, ExecutionContext context, Func<CancellationToken, Task<bool>> func)
		{
			return CommandResult.Async (async token => {
				if (await func (token).ConfigureAwait (false))
					return;
				var res = await proxy.SendCommand (id, method, args, token);
				await proxy.SendResponse (id, res, token);
			});
		}

		CommandResult AcceptCommand (MessageId id, string method, JObject args)
		{
			if (!contexts.TryGetValue (id, out var context))
				return CommandResult.Proxy;

			switch (method) {
			case "Debugger.enable": {
					return Async (id, method, args, context, OnDebuggerEnable);
				}

			case "Debugger.getScriptSource": {
					return Async (id, method, args, context, OnGetScriptSource);
				}

			case "Runtime.compileScript": {
					return Async (id, method, args, context, OnCompileDotnetScript);
				}

			case "Debugger.getPossibleBreakpoints": {
					return Async (id, method, args, context, OnGetPossibleBreakpoints);
				}

			case "Debugger.setBreakpoint": {
					break;
				}

			case "Debugger.setBreakpointByUrl": {
					return Async (id, method, args, context, OnSetBreakpointByUrl);
				}

			case "Debugger.removeBreakpoint": {
					return Async (id, method, args, context, OnRemoveBreakpoint);
				}

			case "Debugger.resume": {
					return CommandResult.Async (token => OnResume (id, token));
				}

			case "Debugger.stepInto": {
					return CommandResult.Async (token => Step (id, StepKind.Into, token));
				}

			case "Debugger.stepOut": {
					return CommandResult.Async (token => Step (id, StepKind.Out, token));
				}

			case "Debugger.stepOver": {
					return CommandResult.Async (token => Step (id, StepKind.Over, token));
				}

			case "Runtime.getProperties": {
					if (!DotnetObjectId.TryParse (args? ["objectId"], out var objectId))
						objectId = null;

					return Async (id, method, args, context, token => RuntimeGetProperties (id, objectId, args, token));
				}

			case "Debugger.setPauseOnExceptions": {
					return Async (id, method, args, context, OnSetPauseOnExceptions);
				}
			}

			return CommandResult.Proxy;
		}

		async Task<Result> OnSetPauseOnExceptions (MessageId id, string method, JObject args, ExecutionContext context, CancellationToken token)
		{
			var resp = await SendCommand (id, method, args, token);
			if (!resp.IsOk)
				return resp;

			var state = resp.Value ["state"]?.ToString ();
			if (!await IsRuntimeAlreadyReadyAlready (id, token))
				throw new NotSupportedException ();

			var store = await RuntimeReady (id, token);
			Log ("protocol", $"TEST: {state} {store}");

			var res = await SendMonoCommand (id, MonoCommands.SetPauseOnExceptions (true), token);
			Log ("protocol", $"TEST #1: {res}");

			return Result.Ok (new JObject ());
		}

		void Log (string priority, string msg)
		{
			proxy.Log (priority, msg);
		}
	}
}
