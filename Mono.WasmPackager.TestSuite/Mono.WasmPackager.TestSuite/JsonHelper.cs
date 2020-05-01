using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using PuppeteerSharp;
using Xunit;

namespace Mono.WasmPackager.TestSuite
{
	public static class JsonHelper
	{
		public static T ToObject<T> (this JObject obj, bool camelCase)
		{
			if (camelCase) {
				return obj.ToObject<T> (new JsonSerializerSettings {
					ContractResolver = new CamelCasePropertyNamesContractResolver (),
					NullValueHandling = NullValueHandling.Ignore
				});
			}

			return obj.ToObject<T> ();
		}

		public static T ToObject<T> (this JObject obj, JsonSerializerSettings jsonSerializerSettings)
			=> obj.ToObject<T> (JsonSerializer.Create (jsonSerializerSettings));

		public static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new JsonSerializerSettings {
			ContractResolver = new CamelCasePropertyNamesContractResolver (),
			NullValueHandling = NullValueHandling.Ignore,
			Converters = new [] {
				new StringEnumConverter (new CamelCaseNamingStrategy (), false)
			}
		};

		public static readonly JsonSerializer DefaultJsonSerializer = JsonSerializer.Create (DefaultJsonSerializerSettings);
	}
}
