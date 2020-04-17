using System;
using Newtonsoft.Json.Linq;

namespace WebAssembly.Net.Debugging
{
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
