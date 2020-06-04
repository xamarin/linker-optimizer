using System;
using System.Threading.Tasks;
using Xunit;

namespace NetCoreTests
{
	public class PuppeteerInspector : SharedTests.PuppeteerInspector
	{
		[Fact]
		public Task TestMessage () => SharedTestMessage ();

		[Fact]
		public Task TestSimpleMessage () => SharedTestSimpleMessage ();

		[Fact]
		public Task TestThrow () => SharedTestThrow ();

		[Fact]
		public Task TestBreakpoint () => SharedTestBreakpoint ();

		[Fact]
		public Task TestBreakpoint2 () => SharedTestBreakpoint2 ();

		[Fact]
		public Task GetVersion () => SharedGetVersion (new Version (5, 0));
	}
}
