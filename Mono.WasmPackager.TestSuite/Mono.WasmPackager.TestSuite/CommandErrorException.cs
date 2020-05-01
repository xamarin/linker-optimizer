using System;
using Newtonsoft.Json.Linq;

namespace Mono.WasmPackager.TestSuite
{
	public class CommandErrorException : Exception
	{
		public string Command {
			get;
		}

		public JObject Error {
			get;
		}

		public CommandErrorException (string command, JObject error)
		{
			Command = command;
			Error = error;
		}
	}
}