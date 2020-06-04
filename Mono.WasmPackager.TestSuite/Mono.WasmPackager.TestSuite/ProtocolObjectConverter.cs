using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Mono.WasmPackager.TestSuite
{
	using Messaging;

	public class ProtocolObjectConverter : JsonConverter<ProtocolObject>
	{
		public override ProtocolObject ReadJson (
			JsonReader reader, Type objectType, ProtocolObject existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (!hasExistingValue)
				existingValue = (ProtocolObject)Activator.CreateInstance (objectType, null);
			if (reader is JTokenReader tokenReader)
				existingValue.OriginalJToken = tokenReader.CurrentToken;
			serializer.Populate (reader, existingValue);
			return existingValue;
		}

		//
		// We register this via [JsonConverter (typeof (ProtocolObjectConverter))] on the ProtocolObject class
		// to make sure we can use easily use ToObject<T> () everywhere.
		//
		// To make WriteJson() work properly (without introducing a recursive loop), we need to create a special
		// JsonSerializer with a custom IContractResolver that "clears out" the converter.
		//
		// One interesting side-effect of this is that we do not need to explicitly specify a serializer or
		// serialization settings when calling JObject.FromObject ().
		//

		public override void WriteJson (JsonWriter writer, ProtocolObject value, JsonSerializer serializer)
		{
			if (value.OriginalJToken != null)
				value.OriginalJToken.WriteTo (writer);
			else
				innerSerializer.Value.Serialize (writer, value);
		}

		static readonly Lazy<JsonSerializer> innerSerializer = new Lazy<JsonSerializer> (() => {
			var settings = JsonHelper.CreateDefaultJsonSerializerSettings ();
			settings.ContractResolver = new ResolverProxy ();
			return JsonSerializer.Create (settings);
		});

		class ResolverProxy : CamelCasePropertyNamesContractResolver
		{
			readonly ConcurrentDictionary<Type, JsonContract> contractCache = new ConcurrentDictionary<Type, JsonContract> ();

			public override JsonContract ResolveContract (Type type)
			{
				if (!typeof (ProtocolObject).IsAssignableFrom (type))
					return base.ResolveContract (type);

				//
				// The base implementation in CamelCasePropertyNamesContractResolver uses a static
				// dictionary to map types to contracts.  To avoid interfering with the default
				// serialization via JObject.FromObject() we must not modify the JsonContract object
				// that's being returned from the base class.
				//
				// Instead, we call CreateObjectContract() to create a new contract instance, then
				// do our own caching.
				//

				return contractCache.GetOrAdd (type, _ => {
					var contract = CreateObjectContract (type);
					if (!(contract.Converter is ProtocolObjectConverter))
						throw new InvalidOperationException ();
					contract.Converter = null;
					return contract;
				});
			}
		}
	}
}
