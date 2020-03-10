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
using P = PuppeteerSharp;

namespace Mono.WasmPackager.DevServer
{
	public class NewMonoProxy : NewDevToolsProxy, IMonoProxy
	{
		DebugStore store;
		List<Breakpoint> breakpoints = new List<Breakpoint> ();
		List<Frame> current_callstack;
		bool runtime_ready;
		int local_breakpoint_id;
		int ctx_id;
		JObject aux_ctx_data;

		NewMonoProxy (AbstractConnection browserConnection, AbstractConnection ideConnection)
			: base (browserConnection, ideConnection)
		{ }

		public static NewMonoProxy Create (P.CDPSession session, WebSocketManager manager)
		{
			var browserConnection = new PuppeteerConnection (session, "browser");
			var ideConnection = new ServerWebSocketConnection (manager, session.SessionId, "ide");
			return new NewMonoProxy (browserConnection, ideConnection);
		}

		public static NewMonoProxy Create (Uri endpoint, WebSocketManager manager, string sessionId = null)
		{
			var browserConnection = new ClientWebSocketConnection (endpoint, sessionId, "brower");
			var ideConnection = new ServerWebSocketConnection (manager, sessionId, "ide");
			return new NewMonoProxy (browserConnection, ideConnection);
		}

		internal Task<Result> SendMonoCommand (SessionId id, MonoCommands cmd, CancellationToken token)
			=> SendCommand (id, "Runtime.evaluate", JObject.FromObject (cmd), token);

		protected override void AcceptEvent (SessionId sessionId, ConnectionEventArgs eventArgs)
		{
			var args = eventArgs.Arguments;
			switch (eventArgs.Message) {
			case "Runtime.executionContextCreated": {
					var ctx = args? ["context"];
					var aux_data = ctx? ["auxData"] as JObject;
					if (aux_data != null) {
						var is_default = aux_data ["isDefault"]?.Value<bool> ();
						if (is_default == true) {
							var id = new MessageId { id = ctx ["id"].Value<int> (), sessionId = sessionId.sessionId };
							eventArgs.Handler = token => OnDefaultContext (id, aux_data, token);
						}
					}
					break;
				}
			case "Debugger.paused": {
					//TODO figure out how to stich out more frames and, in particular what happens when real wasm is on the stack
					var top_func = args? ["callFrames"]? [0]? ["functionName"]?.Value<string> ();
					if (top_func == "mono_wasm_fire_bp" || top_func == "_mono_wasm_fire_bp")
						eventArgs.Handler = token => OnBreakpointHit (sessionId, args, token);
					if (top_func == MonoConstants.RUNTIME_IS_READY)
						eventArgs.Handler = token => OnRuntimeReady (new SessionId { sessionId = sessionId.sessionId }, token);
					break;
				}
			case "Debugger.scriptParsed": {
					if (args? ["url"]?.Value<string> ()?.StartsWith ("wasm://") == true) {
						// Console.WriteLine ("ignoring wasm event");
						eventArgs.SkipEvent = true;
					}
					break;
				}
			case "Debugger.enabled": {
					if (store == null)
						eventArgs.Handler = token => LoadStore (new SessionId { sessionId = args? ["sessionId"]?.Value<string> () }, token);
					break;
				}
			case "Runtime.consoleAPICalled":
				break;
			default:
				Debug.WriteLine ($"MONO PROXY EVENT: {eventArgs.Message}");
				break;
			}
		}

		protected override void AcceptCommand (MessageId id, ConnectionEventArgs eventArgs)
		{
			var args = eventArgs.Arguments;
			switch (eventArgs.Message) {
			case "Target.attachToTarget":
				break;
			case "Target.attachToBrowserTarget":
				break;
			case "Debugger.getScriptSource": {
					var script_id = args? ["scriptId"]?.Value<string> ();
					if (script_id.StartsWith ("dotnet://", StringComparison.InvariantCultureIgnoreCase))
						eventArgs.Handler = token => OnGetScriptSource (id, script_id, token);
					break;
				}

			case "Runtime.compileScript": {
					var exp = args? ["expression"]?.Value<string> ();
					if (exp.StartsWith ("//dotnet:", StringComparison.InvariantCultureIgnoreCase))
						eventArgs.Handler = token => OnCompileDotnetScript (id, token);
					break;
				}

			case "Debugger.getPossibleBreakpoints": {
					var start = SourceLocation.Parse (args? ["start"] as JObject);
					//FIXME support variant where restrictToFunction=true and end is omitted
					var end = SourceLocation.Parse (args? ["end"] as JObject);
					if (start != null)
						eventArgs.Handler = token => GetPossibleBreakpoints (id, start, end, token);
					break;
				}

			case "Debugger.setBreakpointByUrl": {
					Log ("verbose", $"BP req {args}");
					var bp_req = BreakPointRequest.Parse (args, store);
					if (bp_req != null)
						eventArgs.Handler = token => SetBreakPoint (id, bp_req, token);
					break;
				}

			case "Debugger.removeBreakpoint": {
					eventArgs.Handler = token => RemoveBreakpoint (id, args, token);
					break;
				}

			case "Debugger.resume": {
					eventArgs.Handler = token => OnResume (token);
					break;
				}

			case "Debugger.stepInto": {
					if (this.current_callstack != null)
						eventArgs.Handler = token => Step (id, StepKind.Into, token);
					break;
				}

			case "Debugger.stepOut": {
					if (this.current_callstack != null)
						eventArgs.Handler = token => Step (id, StepKind.Out, token);
					break;
				}

			case "Debugger.stepOver": {
					if (this.current_callstack != null)
						eventArgs.Handler = token => Step (id, StepKind.Over, token);
					break;
				}

			case "Runtime.getProperties": {
					var objId = args? ["objectId"]?.Value<string> ();
					if (objId.StartsWith ("dotnet:")) {
						var parts = objId.Split (new char [] { ':' });
						if (parts.Length < 3) {
							eventArgs.SkipEvent = true;
							return;
						}
						switch (parts [1]) {
						case "scope": {
								eventArgs.Handler = token => GetScopeProperties (id, int.Parse (parts [2]), token);
								break;
							}
						case "object": {
								eventArgs.Handler = token => GetDetails (id, MonoCommands.GetObjectProperties (int.Parse (parts [2])), token);
								break;
							}
						case "array": {
								eventArgs.Handler = token => GetDetails (id, MonoCommands.GetArrayValues (int.Parse (parts [2])), token);
								break;
							}
						}
						eventArgs.SkipEvent = true;
					}
					break;
				}
			}
		}

		async Task OnRuntimeReady (SessionId sessionId, CancellationToken token)
		{
			Log ("info", "RUNTIME READY, PARTY TIME");
			await RuntimeReady (sessionId, token);
			// await SendCommand (sessionId, "Debugger.resume", new JObject (), token);
			await SendEvent (sessionId, "Mono.runtimeReady", new JObject (), token);
		}

		//static int frame_id=0;
		async Task OnBreakpointHit (SessionId sessionId, JObject args, CancellationToken token)
		{
			//FIXME we should send release objects every now and then? Or intercept those we inject and deal in the runtime
			var res = await SendMonoCommand (sessionId, MonoCommands.GetCallStack (), token);
			var orig_callframes = args? ["callFrames"]?.Values<JObject> ();

			if (res.IsErr) {
				//Give up and send the original call stack
				await SendEvent (sessionId, "Debugger.paused", args, token);
				return;
			}

			//step one, figure out where did we hit
			var res_value = res.Value? ["result"]? ["value"];
			if (res_value == null || res_value is JValue) {
				//Give up and send the original call stack
				await SendEvent (sessionId, "Debugger.paused", args, token);
				return;
			}

			Log ("verbose", $"call stack (err is {res.Error} value is:\n{res.Value}");
			var bp_id = res_value? ["breakpoint_id"]?.Value<int> ();
			Log ("verbose", $"We just hit bp {bp_id}");
			if (!bp_id.HasValue) {
				//Give up and send the original call stack
				await SendEvent (sessionId, "Debugger.paused", args, token);
				return;
			}
			var bp = this.breakpoints.FirstOrDefault (b => b.RemoteId == bp_id.Value);

			var src = bp == null ? null : store.GetFileById (bp.Location.Id);

			var callFrames = new List<JObject> ();
			foreach (var frame in orig_callframes) {
				var function_name = frame ["functionName"]?.Value<string> ();
				var url = frame ["url"]?.Value<string> ();
				if ("mono_wasm_fire_bp" == function_name || "_mono_wasm_fire_bp" == function_name) {
					var frames = new List<Frame> ();
					int frame_id = 0;
					var the_mono_frames = res.Value? ["result"]? ["value"]? ["frames"]?.Values<JObject> ();

					foreach (var mono_frame in the_mono_frames) {
						var il_pos = mono_frame ["il_pos"].Value<int> ();
						var method_token = mono_frame ["method_token"].Value<int> ();
						var assembly_name = mono_frame ["assembly_name"].Value<string> ();

						var asm = store.GetAssemblyByName (assembly_name);
						if (asm == null) {
							Log ("info", $"Unable to find assembly: {assembly_name}");
							continue;
						}

						var method = asm.GetMethodByToken (method_token);

						if (method == null) {
							Log ("info", $"Unable to find il offset: {il_pos} in method token: {method_token} assembly name: {assembly_name}");
							continue;
						}

						var location = method?.GetLocationByIl (il_pos);

						// When hitting a breakpoint on the "IncrementCount" method in the standard
						// Blazor project template, one of the stack frames is inside mscorlib.dll
						// and we get location==null for it. It will trigger a NullReferenceException
						// if we don't skip over that stack frame.
						if (location == null) {
							continue;
						}

						Log ("info", $"frame il offset: {il_pos} method token: {method_token} assembly name: {assembly_name}");
						Log ("info", $"\tmethod {method.Name} location: {location}");
						frames.Add (new Frame (method, location, frame_id));

						callFrames.Add (JObject.FromObject (new {
							functionName = method.Name,
							callFrameId = $"dotnet:scope:{frame_id}",
							functionLocation = method.StartLocation.ToJObject (),

							location = location.ToJObject (),

							url = store.ToUrl (location),

							scopeChain = new [] {
								new {
									type = "local",
									@object = new {
										@type = "object",
										className = "Object",
										description = "Object",
										objectId = $"dotnet:scope:{frame_id}",
									},
									name = method.Name,
									startLocation = method.StartLocation.ToJObject (),
									endLocation = method.EndLocation.ToJObject (),
								}}
						}));

						++frame_id;
						this.current_callstack = frames;

					}
				} else if (!(function_name.StartsWith ("wasm-function", StringComparison.InvariantCulture)
					|| url.StartsWith ("wasm://wasm/", StringComparison.InvariantCulture))) {
					callFrames.Add (frame);
				}
			}

			var bp_list = new string [bp == null ? 0 : 1];
			if (bp != null)
				bp_list [0] = $"dotnet:{bp.LocalId}";

			var o = JObject.FromObject (new {
				callFrames = callFrames,
				reason = "other", //other means breakpoint
				hitBreakpoints = bp_list,
			});

			await SendEvent (sessionId, "Debugger.paused", o, token);
		}

		async Task OnDefaultContext (MessageId ctx_id, JObject aux_data, CancellationToken token)
		{
			Log ("verbose", "Default context created, clearing state and sending events");

			//reset all bps
			foreach (var b in this.breakpoints) {
				b.State = BreakPointState.Pending;
			}
			this.runtime_ready = false;
			this.ctx_id = ctx_id.id;
			this.aux_ctx_data = aux_data;

			Log ("verbose", "checking if the runtime is ready");
			var res = await SendMonoCommand (ctx_id, MonoCommands.IsRuntimeReady (), token);
			var is_ready = res.Value? ["result"]? ["value"]?.Value<bool> ();
			//Log ("verbose", $"\t{is_ready}");
			if (is_ready.HasValue && is_ready.Value == true) {
				Log ("verbose", "RUNTIME LOOK READY. GO TIME!");
				await OnRuntimeReady (ctx_id, token);
			}
		}

		async Task OnResume (CancellationToken token)
		{
			//discard frames
			this.current_callstack = null;
			await Task.CompletedTask;
		}

		async Task Step (MessageId msg_id, StepKind kind, CancellationToken token)
		{
			var res = await SendMonoCommand (msg_id, MonoCommands.StartSingleStepping (kind), token);

			await SendResponse (msg_id, Result.Ok (new JObject ()), token);

			this.current_callstack = null;

			await SendCommand (msg_id, "Debugger.resume", new JObject (), token);
		}

		static string FormatFieldName (string name)
		{
			if (name.Contains ("k__BackingField")) {
				return name.Replace ("k__BackingField", "")
					.Replace ("<", "")
					.Replace (">", "");
			}
			return name;
		}

		async Task GetDetails (MessageId msg_id, MonoCommands cmd, CancellationToken token)
		{
			var res = await SendMonoCommand (msg_id, cmd, token);

			//if we fail we just buble that to the IDE (and let it panic over it)
			if (res.IsErr) {
				await SendResponse (msg_id, res, token);
				return;
			}

			try {
				var values = res.Value? ["result"]? ["value"]?.Values<JObject> ().ToArray () ?? Array.Empty<JObject> ();
				var var_list = new List<JObject> ();

				// Trying to inspect the stack frame for DotNetDispatcher::InvokeSynchronously
				// results in a "Memory access out of bounds", causing 'values' to be null,
				// so skip returning variable values in that case.
				for (int i = 0; i < values.Length; i += 2) {
					string fieldName = FormatFieldName ((string)values [i] ["name"]);
					var value = values [i + 1]? ["value"];
					if (((string)value ["description"]) == null)
						value ["description"] = value ["value"]?.ToString ();

					var_list.Add (JObject.FromObject (new {
						name = fieldName,
						value
					}));

				}
				var response = JObject.FromObject (new {
					result = var_list
				});

				await SendResponse (msg_id, Result.Ok (response), token);
			} catch (Exception e) {
				Log ("verbose", $"failed to parse {res.Value} - {e.Message}");
				await SendResponse (msg_id, Result.Exception (e), token);
			}
		}

		async Task GetScopeProperties (MessageId msg_id, int scope_id, CancellationToken token)
		{
			var scope = this.current_callstack.FirstOrDefault (s => s.Id == scope_id);
			var vars = scope.Method.GetLiveVarsAt (scope.Location.CliLocation.Offset);

			var var_ids = vars.Select (v => v.Index).ToArray ();
			var res = await SendMonoCommand (msg_id, MonoCommands.GetScopeVariables (scope.Id, var_ids), token);

			//if we fail we just buble that to the IDE (and let it panic over it)
			if (res.IsErr) {
				await SendResponse (msg_id, res, token);
				return;
			}

			try {
				var values = res.Value? ["result"]? ["value"]?.Values<JObject> ().ToArray ();

				var var_list = new List<JObject> ();
				int i = 0;
				// Trying to inspect the stack frame for DotNetDispatcher::InvokeSynchronously
				// results in a "Memory access out of bounds", causing 'values' to be null,
				// so skip returning variable values in that case.
				while (values != null && i < vars.Length && i < values.Length) {
					var value = values [i] ["value"];
					if (((string)value ["description"]) == null)
						value ["description"] = value ["value"]?.ToString ();

					var_list.Add (JObject.FromObject (new {
						name = vars [i].Name,
						value
					}));
					i++;
				}
				//Async methods are special in the way that local variables can be lifted to generated class fields
				//value of "this" comes here either
				while (i < values.Length) {
					String name = values [i] ["name"].ToString ();

					if (name.IndexOf (">", StringComparison.Ordinal) > 0)
						name = name.Substring (1, name.IndexOf (">", StringComparison.Ordinal) - 1);

					var value = values [i + 1] ["value"];
					if (((string)value ["description"]) == null)
						value ["description"] = value ["value"]?.ToString ();

					var_list.Add (JObject.FromObject (new {
						name,
						value
					}));
					i = i + 2;
				}
				var o = JObject.FromObject (new {
					result = var_list
				});
				await SendResponse (msg_id, Result.Ok (o), token);
			} catch (Exception exception) {
				Log ("verbose", $"Error resolving scope properties {exception.Message}");
				await SendResponse (msg_id, Result.Exception (exception), token);
			}
		}

		async Task<Result> EnableBreakPoint (SessionId sessionId, Breakpoint bp, CancellationToken token)
		{
			var asm_name = bp.Location.CliLocation.Method.Assembly.Name;
			var method_token = bp.Location.CliLocation.Method.Token;
			var il_offset = bp.Location.CliLocation.Offset;

			var res = await SendMonoCommand (sessionId, MonoCommands.SetBreakpoint (asm_name, method_token, il_offset), token);
			var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

			if (ret_code.HasValue) {
				bp.RemoteId = ret_code.Value;
				bp.State = BreakPointState.Active;
				//Log ("verbose", $"BP local id {bp.LocalId} enabled with remote id {bp.RemoteId}");
			}

			return res;
		}

		async Task LoadStore (SessionId sessionId, CancellationToken token)
		{
			var loaded_pdbs = await SendMonoCommand (sessionId, MonoCommands.GetLoadedFiles (), token);
			var the_value = loaded_pdbs.Value? ["result"]? ["value"];
			var the_pdbs = the_value?.ToObject<string []> ();

			store = new DebugStore ();
			await store.Load (this, sessionId, the_pdbs, token);
		}

		async Task RuntimeReady (SessionId sessionId, CancellationToken token)
		{
			if (store == null)
				await LoadStore (sessionId, token);

			foreach (var s in store.AllSources ()) {
				var ok = JObject.FromObject (new {
					scriptId = s.SourceId.ToString (),
					url = s.Url,
					executionContextId = this.ctx_id,
					hash = s.DocHashCode,
					executionContextAuxData = this.aux_ctx_data,
					dotNetUrl = s.DotNetUrl,
				});
				//Log ("verbose", $"\tsending {s.Url}");
				await SendEvent (sessionId, "Debugger.scriptParsed", ok, token);
			}

			var clear_result = await SendMonoCommand (sessionId, MonoCommands.ClearAllBreakpoints (), token);
			if (clear_result.IsErr) {
				Log ("verbose", $"Failed to clear breakpoints due to {clear_result}");
			}

			runtime_ready = true;

			foreach (var bp in breakpoints) {
				if (bp.State != BreakPointState.Pending)
					continue;
				var res = await EnableBreakPoint (sessionId, bp, token);
				var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

				//if we fail we just buble that to the IDE (and let it panic over it)
				if (!ret_code.HasValue) {
					//FIXME figure out how to inform the IDE of that.
					Log ("info", $"FAILED TO ENABLE BP {bp.LocalId}");
					bp.State = BreakPointState.Disabled;
				}
			}
		}

		async Task<bool> RemoveBreakpoint (MessageId msg_id, JObject args, CancellationToken token)
		{
			var bpid = args? ["breakpointId"]?.Value<string> ();
			if (bpid?.StartsWith ("dotnet:") != true)
				return false;

			var the_id = int.Parse (bpid.Substring ("dotnet:".Length));

			var bp = breakpoints.FirstOrDefault (b => b.LocalId == the_id);
			if (bp == null) {
				Log ("info", $"Could not find dotnet bp with id {the_id}");
				return false;
			}

			breakpoints.Remove (bp);
			//FIXME verify result (and log?)
			var res = await RemoveBreakPoint (msg_id, bp, token);

			return true;
		}


		async Task<Result> RemoveBreakPoint (SessionId sessionId, Breakpoint bp, CancellationToken token)
		{
			var res = await SendMonoCommand (sessionId, MonoCommands.RemoveBreakpoint (bp.RemoteId), token);
			var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

			if (ret_code.HasValue) {
				bp.RemoteId = -1;
				bp.State = BreakPointState.Disabled;
			}

			return res;
		}

		async Task SetBreakPoint (MessageId msg_id, BreakPointRequest req, CancellationToken token)
		{
			var bp_loc = store?.FindBestBreakpoint (req);
			Log ("info", $"BP request for '{req}' runtime ready {runtime_ready} location '{bp_loc}'");
			if (bp_loc == null) {
				Log ("info", $"Could not resolve breakpoint request: {req}");
				await SendResponse (msg_id, Result.Err (JObject.FromObject (new {
					code = (int)MonoErrorCodes.BpNotFound,
					message = $"C# Breakpoint at {req} not found."
				})), token);
				return;
			}

			Breakpoint bp = null;
			if (!runtime_ready) {
				bp = new Breakpoint (bp_loc, local_breakpoint_id++, BreakPointState.Pending);
			} else {
				bp = new Breakpoint (bp_loc, local_breakpoint_id++, BreakPointState.Disabled);

				var res = await EnableBreakPoint (msg_id, bp, token);
				var ret_code = res.Value? ["result"]? ["value"]?.Value<int> ();

				//if we fail we just buble that to the IDE (and let it panic over it)
				if (!ret_code.HasValue) {
					await SendResponse (msg_id, res, token);
					return;
				}
			}

			var locations = new List<JObject> ();

			locations.Add (JObject.FromObject (new {
				scriptId = bp_loc.Id.ToString (),
				lineNumber = bp_loc.Line,
				columnNumber = bp_loc.Column
			}));

			breakpoints.Add (bp);

			var ok = JObject.FromObject (new {
				breakpointId = $"dotnet:{bp.LocalId}",
				locations = locations,
			});

			await SendResponse (msg_id, Result.Ok (ok), token);
		}

		async Task<bool> GetPossibleBreakpoints (MessageId msg_id, SourceLocation start, SourceLocation end, CancellationToken token)
		{
			var bps = store.FindPossibleBreakpoints (start, end);
			if (bps == null)
				return false;

			var loc = new List<JObject> ();
			foreach (var b in bps) {
				loc.Add (b.ToJObject ());
			}

			var o = JObject.FromObject (new {
				locations = loc
			});

			await SendResponse (msg_id, Result.Ok (o), token);

			return true;
		}

		async Task OnCompileDotnetScript (MessageId msg_id, CancellationToken token)
		{
			var o = JObject.FromObject (new { });

			await SendResponse (msg_id, Result.Ok (o), token);
		}

		async Task OnGetScriptSource (MessageId msg_id, string script_id, CancellationToken token)
		{
			var id = new SourceId (script_id);
			var src_file = store.GetFileById (id);

			var res = new StringWriter ();
			//res.WriteLine ($"//{id}");

			try {
				var uri = new Uri (src_file.Url);
				if (uri.IsFile && File.Exists (uri.LocalPath)) {
					using (var f = new StreamReader (File.Open (uri.LocalPath, FileMode.Open))) {
						await res.WriteAsync (await f.ReadToEndAsync ());
					}

					var o = JObject.FromObject (new {
						scriptSource = res.ToString ()
					});

					await SendResponse (msg_id, Result.Ok (o), token);
				} else if (src_file.SourceUri.IsFile && File.Exists (src_file.SourceUri.LocalPath)) {
					using (var f = new StreamReader (File.Open (src_file.SourceUri.LocalPath, FileMode.Open))) {
						await res.WriteAsync (await f.ReadToEndAsync ());
					}

					var o = JObject.FromObject (new {
						scriptSource = res.ToString ()
					});

					await SendResponse (msg_id, Result.Ok (o), token);
				} else if (src_file.SourceLinkUri != null) {
					var doc = await new WebClient ().DownloadStringTaskAsync (src_file.SourceLinkUri);
					await res.WriteAsync (doc);

					var o = JObject.FromObject (new {
						scriptSource = res.ToString ()
					});

					await SendResponse (msg_id, Result.Ok (o), token);
				} else {
					var o = JObject.FromObject (new {
						scriptSource = $"// Unable to find document {src_file.SourceUri}"
					});

					await SendResponse (msg_id, Result.Ok (o), token);
				}
			} catch (Exception e) {
				var o = JObject.FromObject (new {
					scriptSource = $"// Unable to read document ({e.Message})\n" +
								$"Local path: {src_file?.SourceUri}\n" +
								$"SourceLink path: {src_file?.SourceLinkUri}\n"
				});

				await SendResponse (msg_id, Result.Ok (o), token);
			}
		}
	}
}
