using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using PuppeteerSharp;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.TestSuite.Messaging.Debugger;
using Mono.WasmPackager.DevServer;

namespace BlazorTests
{
	public class MartinTest : PuppeteerBlazorSample
	{
		[Fact]
		public async Task StartAndClickHome ()
		{
			await StartBlazorApp ().ConfigureAwait (false);

			var bodySelector = await QuerySelectorAsync (TestConstants.BodySelector);
			Assert.NotNull (bodySelector);

			await AssertSelectorVisible (TestConstants.BodySelector);

			await AssertSelectorVisible (TestConstants.NothingHereSelector, TestConstants.NothingHereText);
			await AssertSelectorNotVisible (TestConstants.WelcomeSelector);

			await HomeSelector.ClickAsync ();

			await AssertSelectorVisible (TestConstants.WelcomeSelector, TestConstants.WelcomeText);
			await AssertSelectorNotVisible (TestConstants.NothingHereSelector);

			Debug.WriteLine ("DONE!");
		}

		[Fact]
		public async Task StartAndClickControls ()
		{
			await StartBlazorApp ().ConfigureAwait (false);

			var bodySelector = await QuerySelectorAsync (TestConstants.BodySelector);
			Assert.NotNull (bodySelector);

			await AssertSelectorVisible (TestConstants.BodySelector);

			await AssertSelectorVisible (TestConstants.NothingHereSelector, TestConstants.NothingHereText);
			await AssertSelectorNotVisible (TestConstants.WelcomeSelector);

			await ControlsSelector.ClickAsync ();

			await AssertSelectorVisible (TestConstants.ControlsSelector, TestConstants.ControlsText);
			await AssertSelectorNotVisible (TestConstants.NothingHereSelector);

			Debug.WriteLine ("DONE!");
		}

		[Fact]
		public async Task InsertControlsBreakpoint ()
		{
			await StartBlazorApp ().ConfigureAwait (false);

			var bp = await InsertBreakpoint (TestSettings.Locations.ControlsBreakpoint);
			Debug.WriteLine ($"BREAKPOINT: {bp}");

			await ControlsSelector.ClickAsync ();

			await AssertSelectorVisible (TestConstants.ControlsSelector, TestConstants.ControlsText);
			await AssertSelectorNotVisible (TestConstants.NothingHereSelector);

			Debug.WriteLine ("DONE!");
		}
	}
}
