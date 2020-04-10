using System;
using Newtonsoft.Json.Linq;

namespace WebAssembly.Net.Debugging
{
	internal struct SessionId {
		public readonly string sessionId;

		public SessionId (string sessionId)
		{
			this.sessionId = sessionId;
		}

		public override int GetHashCode ()
			=> sessionId?.GetHashCode () ?? 0;

		public override bool Equals (object obj)
			=> (obj is SessionId) ? ((SessionId) obj).sessionId == sessionId : false;

		public override string ToString ()
			=> $"session-{sessionId}";
	}

	internal struct MessageId {
		public readonly string sessionId;
		public readonly int id;

		public MessageId (string sessionId, int id)
		{
			this.sessionId = sessionId;
			this.id = id;
		}

		public static implicit operator SessionId (MessageId id)
			=> new SessionId (id.sessionId);

		public override string ToString ()
			=> $"msg-{sessionId}:::{id}";

		public override int GetHashCode ()
			=> (sessionId?.GetHashCode () ?? 0) ^ id.GetHashCode ();

		public override bool Equals (object obj)
			=> (obj is MessageId) ? ((MessageId) obj).sessionId == sessionId && ((MessageId) obj).id == id : false;
	}

	public struct Result {
		public JObject Value { get; private set; }
		public JObject Error { get; private set; }

		public bool IsOk => Value != null;
		public bool IsErr => Error != null;

		Result (JObject result, JObject error)
		{
			if (result != null && error != null)
				throw new ArgumentException ($"Both {nameof(result)} and {nameof(error)} arguments cannot be non-null.");

			bool resultHasError = String.Compare ((result? ["result"] as JObject)? ["subtype"]?. Value<string> (), "error") == 0;
			if (result != null && resultHasError) {
				this.Value = null;
				this.Error = result;
			} else {
				this.Value = result;
				this.Error = error;
			}
		}

		public static Result FromJson (JObject obj)
		{
			//Log ("protocol", $"from result: {obj}");
			return new Result (obj ["result"] as JObject, obj ["error"] as JObject);
		}

		public static Result Ok (JObject ok)
			=> new Result (ok, null);

		public static Result OkFromObject (object ok)
			=> Ok (JObject.FromObject(ok));

		public static Result Err (JObject err)
			=> new Result (null, err);

		public static Result Err (string message)
			=> Err (JObject.FromObject (new { message }));

		public static Result Exception (Exception e)
			=> new Result (null, JObject.FromObject (new { message = e.Message }));

		internal JObject ToJObject (MessageId target) {
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
