using Microsoft.Build.Framework;

namespace Mono.WasmPackager
{
	public class MartinTest : Microsoft.Build.Utilities.Task
	{
		public override bool Execute ()
		{
			Log.LogMessage (MessageImportance.High, "MARTIN TEST");
			return false;
		}
	}
}
