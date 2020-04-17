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
			var args = eventArgs.Arguments;
			switch (eventArgs.Message) {
			case "Runtime.consoleAPICalled": {
					var type = args["type"]?.ToString ();
					if (type == "debug") {
						if (args["args"]?[0]?["value"]?.ToString () == MonoConstants.RUNTIME_IS_READY && args["args"]?[1]?["value"]?.ToString () == "fe00e07a-5519-4dfe-b35a-f867dbaf2e28")
							eventArgs.Handler = token => RuntimeReady (sessionId, token);
					}
					break;
				}

			case "Runtime.executionContextCreated": {
					var ctx = args? ["context"];
					var aux_data = ctx? ["auxData"] as JObject;
					var id = ctx ["id"].Value<int> ();
					if (aux_data != null) {
						var is_default = aux_data ["isDefault"]?.Value<bool> ();
						if (is_default == true) {
							eventArgs.Handler = token => OnDefaultContext (sessionId, new ExecutionContext { Id = id, AuxData = aux_data }, token);
						}
					}
					eventArgs.SkipEvent = true;
					break;
				}

			case "Debugger.paused": {
					//TODO figure out how to stich out more frames and, in particular what happens when real wasm is on the stack
					var top_func = args? ["callFrames"]? [0]? ["functionName"]?.Value<string> ();

					if (top_func == "mono_wasm_fire_bp" || top_func == "_mono_wasm_fire_bp") {
						eventArgs.Handler = token => OnBreakpointHit (sessionId, args, token);
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
							eventArgs.SkipEvent = true;
							break;
						}
					}
					Log ("verbose", $"proxying Debugger.scriptParsed ({sessionId.sessionId}) {url} {args}");
					break;
				}
			}
		}

		internal void AcceptCommand (MessageId id, ConnectionEventArgs eventArgs)
		{
			if (!contexts.TryGetValue (id, out var context))
				return;

			var args = eventArgs.Arguments;
			var method = eventArgs.Message;
			switch (method) {
			case "Debugger.enable": {
					eventArgs.Handler = token => OnDebuggerEnable (id, method, args, context, token);
					break;
				}

			case "Debugger.getScriptSource": {
					var script = args? ["scriptId"]?.Value<string> ();
					eventArgs.Handler = token => OnGetScriptSource (id, script, token);
					break;
				}

			case "Runtime.compileScript": {
					var exp = args? ["expression"]?.Value<string> ();
					if (exp.StartsWith ("//dotnet:", StringComparison.Ordinal)) {
						eventArgs.Handler = token => OnCompileDotnetScript (id, token);
					}
					break;
				}

			case "Debugger.getPossibleBreakpoints": {
					eventArgs.Handler = token => OnGetPossibleBreakpoints (id, method, args, token);
					break;
				}

			case "Debugger.setBreakpoint": {
					break;
				}

			case "Debugger.setBreakpointByUrl": {
					eventArgs.Handler = token => OnSetBreakpointByUrl (id, method, args, context, token);
					break;
				}

			case "Debugger.removeBreakpoint": {
					eventArgs.Handler = token => OnRemoveBreakpoint (id, method, args, context, token);
					break;
				}

			case "Debugger.resume": {
					eventArgs.Handler = token => OnResume (id, token);
					break;
				}

			case "Debugger.stepInto": {
					eventArgs.Handler = token => Step (id, StepKind.Into, token);
					break;
				}

			case "Debugger.stepOut": {
					eventArgs.Handler = token => Step (id, StepKind.Out, token);
					break;
				}

			case "Debugger.stepOver": {
					eventArgs.Handler = token => Step (id, StepKind.Over, token);
					break;
				}

			case "Runtime.getProperties": {
					var objId = args? ["objectId"]?.Value<string> ();
					if (!objId.StartsWith ("dotnet:", StringComparison.Ordinal))
						break;
					eventArgs.Handler = token => OnGetProperties (id, method, args, token);
					break;
				}
			}
		}

		void Log (string priority, string msg)
		{
			proxy.Log (priority, msg);
		}
	}
}
