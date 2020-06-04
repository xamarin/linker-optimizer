using System.Threading.Tasks;
using Xunit;

namespace WorkingTests
{
	public class TestExceptions : SharedTests.TestExceptions
	{
		[Fact]
		public Task TestUnhandled () => SharedTestUnhandled ();
	}
}
