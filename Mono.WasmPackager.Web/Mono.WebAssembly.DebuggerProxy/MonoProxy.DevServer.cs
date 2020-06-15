using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Mono.WasmPackager.DevServer;

namespace WebAssembly.Net.Debugging {
	partial class MonoProxy : IMonoProxy {
		readonly NewDevToolsProxy proxy;

		readonly ILogger logger;

		internal MonoProxy (NewDevToolsProxy proxy, ILoggerFactory loggerFactory)
		{
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

		internal ProxyCommand AcceptEvent (SessionId sessionId, ConnectionEventArgs eventArgs)
		{
			var message = eventArgs.Message;
			var args = eventArgs.Arguments;

			switch (message) {
			case "Runtime.consoleAPICalled": {
					var type = args["type"]?.ToString ();
					if (type == "debug") {
						if (args["args"]?[0]?["value"]?.ToString () == MonoConstants.RUNTIME_IS_READY && args["args"]?[1]?["value"]?.ToString () == "fe00e07a-5519-4dfe-b35a-f867dbaf2e28")
							return ProxyCommand.AsyncProxy (token => RuntimeReady (sessionId, token));
					}
					break;
				}

			case "Runtime.executionContextCreated": {
					return ProxyCommand.AsyncProxy (token => OnExecutionContextCreated (sessionId, message, args, token));
				}

			case "Debugger.paused": {
					//TODO figure out how to stich out more frames and, in particular what happens when real wasm is on the stack
					var top_func = args? ["callFrames"]? [0]? ["functionName"]?.Value<string> ();

					if (IsFireBreakpointFunction (top_func)) {
						return ProxyCommand.AsyncBool (token => OnBreakpointHit (sessionId, args, token));
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
							return ProxyCommand.Complete;
						}
					}
					Log ("verbose", $"proxying Debugger.scriptParsed ({sessionId.sessionId}) {url} {args}");
					break;
				}
			}

			return ProxyCommand.Proxy;
		}

		ProxyCommand Async (MessageId id, string method, JObject args, ExecutionContext context, Func<MessageId, string, JObject, ExecutionContext, CancellationToken, Task<Result>> func)
		{
			return ProxyCommand.Async (token => func (id, method, args, context, token));
		}

		internal ProxyCommand AcceptCommand (MessageId id, ConnectionEventArgs eventArgs)
		{
			if (!contexts.TryGetValue (id, out var context))
				return ProxyCommand.Proxy;

			var method = eventArgs.Message;
			var args = eventArgs.Arguments;

			switch (method) {
			case "Debugger.enable": {
					return Async (id, method, args, context, OnDebuggerEnable);
				}

			case "Debugger.getScriptSource": {
					return ProxyCommand.AsyncBool (token => OnGetScriptSource (id, method, args, context, token));
				}

			case "Runtime.compileScript": {
					return ProxyCommand.AsyncBool (token => OnCompileDotnetScript (id, method, args, context, token));
				}

			case "Debugger.getPossibleBreakpoints": {
					return Async (id, method, args, context, OnGetPossibleBreakpoints);
				}

			case "Debugger.setBreakpoint": {
					break;
				}

			case "Debugger.setBreakpointByUrl": {
					return ProxyCommand.AsyncBool (async token => {
						await OnSetBreakpointByUrl (id, method, args, context, token);
						return true;
					});
				}

			case "Debugger.removeBreakpoint": {
					return Async (id, method, args, context, OnRemoveBreakpoint);
				}

			case "Debugger.resume": {
					return ProxyCommand.AsyncProxy (token => OnResume (id, method, args, context, token));
				}

			case "Debugger.stepInto": {
					return ProxyCommand.AsyncBool (token => Step (id, StepKind.Into, token));
				}

			case "Debugger.stepOut": {
					return ProxyCommand.AsyncBool (token => Step (id, StepKind.Out, token));
				}

			case "Debugger.stepOver": {
					return ProxyCommand.AsyncBool (token => Step (id, StepKind.Over, token));
				}

			case "Runtime.getProperties": {
					if (!DotnetObjectId.TryParse (args? ["objectId"], out var objectId))
						break;

					return ProxyCommand.Async (token => RuntimeGetProperties (id, objectId, args, token));
				}

			case "Debugger.evaluateOnCallFrame": {
					if (!DotnetObjectId.TryParse (args? ["callFrameId"], out var objectId))
						break;

					switch (objectId.Scheme) {
					case "scope":
						return ProxyCommand.AsyncBool (token => OnEvaluateOnCallFrame (
								id, context,
								int.Parse (objectId.Value),
								args? ["expression"]?.Value<string> (), token));
					}
					break;
				}

			case "Debugger.setPauseOnExceptions": {
					return Async (id, method, args, context, OnSetPauseOnExceptions);
				}
			}

			return ProxyCommand.Proxy;
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
