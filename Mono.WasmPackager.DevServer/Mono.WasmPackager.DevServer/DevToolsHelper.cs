using System;
using Newtonsoft.Json.Linq;
using WebAssembly.Net.Debugging;

namespace WebAssembly.Net.Debugging
{
	internal class MonoCommands {
		public string expression { get; set; }
		public string objectGroup { get; set; } = "mono-debugger";
		public bool includeCommandLineAPI { get; set; } = false;
		public bool silent { get; set; } = false;
		public bool returnByValue { get; set; } = true;

		public MonoCommands (string expression)
			=> this.expression = expression;

		public static MonoCommands GetCallStack ()
			=> new MonoCommands ("MONO.mono_wasm_get_call_stack()");

		public static MonoCommands IsRuntimeReady ()
			=> new MonoCommands ("MONO.mono_wasm_runtime_is_ready");

		public static MonoCommands StartSingleStepping (StepKind kind)
			=> new MonoCommands ($"MONO.mono_wasm_start_single_stepping ({(int)kind})");

		public static MonoCommands GetLoadedFiles ()
			=> new MonoCommands ("MONO.mono_wasm_get_loaded_files()");

		public static MonoCommands ClearAllBreakpoints ()
			=> new MonoCommands ("MONO.mono_wasm_clear_all_breakpoints()");

		public static MonoCommands GetObjectProperties (int objectId)
			=> new MonoCommands ($"MONO.mono_wasm_get_object_properties({objectId})");

		public static MonoCommands GetArrayValues (int objectId)
			=> new MonoCommands ($"MONO.mono_wasm_get_array_values({objectId})");

		public static MonoCommands GetScopeVariables (int scopeId, params int[] vars)
			=> new MonoCommands ($"MONO.mono_wasm_get_variables({scopeId}, [ {string.Join (",", vars)} ])");

		public static MonoCommands SetBreakpoint (string assemblyName, int methodToken, int ilOffset)
			=> new MonoCommands ($"MONO.mono_wasm_set_breakpoint (\"{assemblyName}\", {methodToken}, {ilOffset})");

		public static MonoCommands RemoveBreakpoint (int breakpointId)
			=> new MonoCommands ($"MONO.mono_wasm_remove_breakpoint({breakpointId})");
	}

	public enum MonoErrorCodes {
		BpNotFound = 100000,
	}

	internal class MonoConstants {
		public const string RUNTIME_IS_READY = "mono_wasm_runtime_ready";
	}

	class Frame {
		public Frame (MethodInfo method, SourceLocation location, int id)
		{
			this.Method = method;
			this.Location = location;
			this.Id = id;
		}

		public MethodInfo Method { get; private set; }
		public SourceLocation Location { get; private set; }
		public int Id { get; private set; }
	}

	class Breakpoint {
		public SourceLocation Location { get; private set; }
		public int LocalId { get; private set; }
		public int RemoteId { get; set; }
		public BreakPointState State { get; set; }

		public Breakpoint (SourceLocation loc, int localId, BreakPointState state)
		{
			this.Location = loc;
			this.LocalId = localId;
			this.State = state;
		}
	}

	enum BreakPointState {
		Active,
		Disabled,
		Pending
	}

	enum StepKind {
		Into,
		Out,
		Over
	}
	
	public class SessionId {
		public string sessionId;
	}

	public class MessageId : SessionId {
		public int id;
	}

	public struct Result {
		public JObject Value { get; private set; }
		public JObject Error { get; private set; }

		public bool IsOk => Value != null;
		public bool IsErr => Error != null;

		Result (JObject result, JObject error)
		{
			this.Value = result;
			this.Error = error;
		}

		public static Result FromJson (JObject obj)
		{
			//Log ("protocol", $"from result: {obj}");
			return new Result (obj ["result"] as JObject, obj ["error"] as JObject);
		}

		public static Result Ok (JObject ok)
			=> new Result (ok, null);

		public static Result Err (JObject err)
			=> new Result (null, err);

		public static Result Exception (Exception e)
			=> new Result (null, JObject.FromObject (new { message = e.Message }));

		public JObject ToJObject (MessageId target) {
			if (IsOk) {
				return JObject.FromObject (new {
					target.id,
					target.sessionId,
					result = Value
				});
			} else {
				return JObject.FromObject (new {
					target.id,
					target.sessionId,
					error = Error
				});
			}
		}
	}
	
	static class DevToolsHelper
	{
		public static Result ResultFromJObject (JObject result)
		{
			return Result.FromJson (result);
		}

		public static JObject ToJObject (SessionId sessionId, string method, JObject args)
		{
			return JObject.FromObject (new {
				sessionId.sessionId,
				method,
				@params = args
			});
		}
	}
}
