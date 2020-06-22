using System.Threading.Tasks;
using Xunit;

namespace WorkingTests
{
	public class TestExceptionVariables : SharedTests.TestExceptionVariables
	{
		[Fact]
		public Task JsExceptionBrowser () => SharedJsException (true);

		[Fact]
		public Task JsExceptionProxy () => SharedJsException (false);

		[Fact]
		public Task JsCaughtExceptionBrowser () => SharedJsCaughtException (true);

		[Fact]
		public Task JsCaughtExceptionProxy () => SharedJsCaughtException (false);

		[Fact]
		public Task JsSilentExceptionBrowser () => SharedJsSilentException (true);

		[Fact]
		public Task JsSilentExceptionProxy () => SharedJsSilentException (false);

		[Fact]
		public Task JsThrownExceptionBrowser () => SharedJsThrownException (true);

		[Fact]
		public Task JsThrownExceptionProxy () => SharedJsThrownException (false);

		[Fact]
		public Task JsInstanceMethodBrowser () => SharedJsInstanceMethod (true);

		[Fact]
		public Task JsInstanceMethodProxy () => SharedJsInstanceMethod (false);

		[Fact]
		public Task JsInstancePropertyBrowser () => SharedJsInstanceProperty (true);

		[Fact]
		public Task JsInstancePropertyProxy () => SharedJsInstanceProperty (false);

		[Fact]
		public Task JsCaughtInstanceMethodBrowser () => SharedJsCaughtInstanceMethod (true);

		[Fact]
		public Task JsCaughtInstanceMethodProxy () => SharedJsCaughtInstanceMethod (false);

		[Fact]
		public Task JsCaughtInstancePropertyBrowser () => SharedJsCaughtInstanceProperty (true);

		[Fact]
		public Task JsCaughtInstancePropertyProxy () => SharedJsCaughtInstanceProperty (false);

		[Fact]
		public Task JsCallFunctionBrowser () => SharedJsCallFunction (true);

		[Fact]
		public Task JsCallFunctionProxy () => SharedJsCallFunction (false);

		[Fact]
		public Task JsEvaluateBrowser () => SharedJsEvaluate (true);

		[Fact]
		public Task JsEvaluateProxy () => SharedJsEvaluate (false);

		[Fact]
		public Task JsEvaluateCaughtBrowser () => SharedJsEvaluateCaught (true);

		[Fact]
		public Task JsEvaluateCaughtProxy () => SharedJsEvaluateCaught (false);

		[Fact]
		public Task JsEvaluateSilentBrowser () => SharedJsEvaluateSilent (true);

		[Fact]
		public Task JsEvaluateSilentProxy () => SharedJsEvaluateSilent (false);

		[Fact]
		public Task ManagedException () => SharedManagedException ();

		[Fact]
		public Task ManagedCaughtException () => SharedManagedCaughtException ();

		[Fact]
		public Task ManagedThrownException () => SharedManagedThrownException ();
	}
}
