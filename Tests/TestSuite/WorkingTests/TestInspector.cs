using System.Threading.Tasks;
using Xunit;

namespace WorkingTests
{
	public class TestInspector : SharedTests.TestInspector
	{
		[Fact]
		public void TestScripts () => SharedTestScripts ();

		[Fact]
		public Task CreateGoodBreakpoint () => SharedCreateGoodBreakpoint ();

		[Fact]
		public Task GetBreakpoints () => SharedGetBreakpoints ();
	}
}
