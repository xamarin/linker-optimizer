using System;
using Newtonsoft.Json;

namespace Mono.WasmPackager.TestSuite.Messaging
{
	public abstract class ProtocolRequest<T> : ProtocolObject
		where T : ProtocolResponse
	{
		[JsonIgnore]
		public Type Type => typeof (T);

		[JsonIgnore]
		public abstract string Command { get; }
	}
}
