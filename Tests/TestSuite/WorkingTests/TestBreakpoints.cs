using System.Threading.Tasks;
using Xunit;

namespace WorkingTests
{
	public class TestBreakpoints : SharedTests.TestBreakpoints
	{
		[Fact]
		public Task InsertHitAndResume () => SharedInsertHitAndResume ();

		[Fact]
		public Task InsertRemoveAndResume () => SharedInsertRemoveAndResume ();

		[Fact]
		public Task InsertRemoveAndInsertAgain () => SharedInsertRemoveAndInsertAgain ();

		[Fact]
		public Task StackTraceWhenHit () => SharedStackTraceWhenHit ();

		[Fact]
		public Task TestAllFrames () => SharedTestAllFrames ();

		[Fact]
		public Task TestSecondFrame () => SharedTestSecondFrame ();

		[Fact]
		public Task TestStepOver () => SharedStepOver ();

		[Fact]
		public Task JsBreakpoint () => SharedJsBreakpoint ();
	}
}
