using System.IO;
using Microsoft.Build.Framework;

namespace Mono.Linker.WasmPackager
{
	public class GetFirstItem : Microsoft.Build.Utilities.Task
	{
		[Required]
		public ITaskItem[] Input {
			get; set;
		}

		[Output]
		public ITaskItem Output {
			get; set;
		}

		public override bool Execute ()
		{
			Output = Input[0];
			return true;
		}
	}
}
