using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Mono.WasmPackager.TestSuite
{
	public static class JsonHelper
	{
		//
		// Do not make this a property or cache the result.
		// This is also called from ProtocolObjectConverter, which modifies the returned object.
		//
		internal static JsonSerializerSettings CreateDefaultJsonSerializerSettings () => new JsonSerializerSettings {
			ContractResolver = new CamelCasePropertyNamesContractResolver (),
			NullValueHandling = NullValueHandling.Ignore,
			Converters = new [] {
				new StringEnumConverter (new CamelCaseNamingStrategy (), false)
			}
		};
	}
}
