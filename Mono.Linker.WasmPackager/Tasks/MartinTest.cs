using Microsoft.Build.Framework;

namespace Mono.Linker.WasmPackager
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
