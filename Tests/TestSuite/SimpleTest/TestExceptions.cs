using System.Threading.Tasks;
using Xunit;

namespace SimpleTest
{
	public class TestExceptions : SharedTests.TestExceptions
	{
		[Fact]
		public Task TestUnhandled () => SharedTestUnhandled ();
	}
}
