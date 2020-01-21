using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Mono.WasmPackager
{
	static class LoggingHelper
	{
		internal static void LogArray (this TaskLoggingHelper logger, string[] array, string message, 
					       MessageImportance importance = MessageImportance.Normal)
		{
			foreach (var item in array)
				logger.LogMessage (importance, $"  {message}: {item}");
		}
	}
}
