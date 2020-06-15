using System.Threading.Tasks;
using Xunit;

namespace WorkingTests
{
	public class TestExceptionVariables : SharedTests.TestExceptionVariables
	{
		[Fact]
		public Task TestJsExceptionBrowser () => SharedTestJsException (true);

		[Fact]
		public Task TestJsExceptionProxy () => SharedTestJsException (false);

		[Fact]
		public Task TestJsCaughtExceptionBrowser () => SharedTestJsCaughtException (true);

		[Fact]
		public Task TestJsCaughtExceptionProxy () => SharedTestJsCaughtException (false);

		[Fact]
		public Task TestJsSilentExceptionBrowser () => SharedTestJsSilentException (true);

		[Fact]
		public Task TestJsSilentExceptionProxy () => SharedTestJsSilentException (false);
	}
}
