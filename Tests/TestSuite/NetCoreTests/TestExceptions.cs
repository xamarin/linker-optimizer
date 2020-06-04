using System.Threading.Tasks;
using Xunit;

namespace NetCoreTests
{
	public class TestExceptions : SharedTests.TestExceptions
	{
		[Fact]
		public Task TestUnhandled () => SharedTestUnhandled ();
	}
}
