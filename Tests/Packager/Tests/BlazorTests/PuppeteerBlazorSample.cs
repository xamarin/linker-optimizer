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
	public abstract class PuppeteerBlazorSample : PuppeteerTestBase
	{
		protected override bool Headless => false;

		protected ElementHandle HomeSelector {
			get; private set;
		}

		protected ElementHandle CounterSelector {
			get; private set;
		}

		protected ElementHandle FetchSelector {
			get; private set;
		}

		public async Task StartBlazorApp ()
		{
			Debug.WriteLine ("Starting Blazor App");

			var titleSelector = await Page.QuerySelectorAsync (TestConstants.TitleSelector);
			Assert.NotNull (titleSelector);

			var titleText = await titleSelector.GetInnerHtml ();
			Assert.Equal (TestConstants.TitleText, titleText);

			await AssertInnerHtml (TestConstants.TitleSelector, TestConstants.TitleText);
			await AssertInnerHtml (TestConstants.NavBarTitleSelector, TestConstants.NavBarTitleText);
			await AssertInnerHtml (TestConstants.NothingHereSelector, TestConstants.NothingHereText);

			HomeSelector = await Page.QuerySelectorAsync (TestConstants.HomeButton);
			Assert.NotNull (HomeSelector);

			CounterSelector = await Page.QuerySelectorAsync (TestConstants.CounterButton);
			Assert.NotNull (CounterSelector);

			FetchSelector = await Page.QuerySelectorAsync (TestConstants.FetchButton);
		}

		protected Task AssertSelectorVisible (string selector, string text = null) => AssertSelectorVisible (selector, true, text);

		protected Task AssertSelectorNotVisible (string selector) => AssertSelectorVisible (selector, false);

		async Task AssertSelectorVisible (string selector, bool visible, string text = null)
		{
			var handle = await Page.QuerySelectorAsync (selector);
			if (visible)
				Assert.NotNull (handle);
			else {
				Assert.Null (handle);
				return;
			}

			if (text != null) {
				var inner = await handle.GetInnerHtml ();
				Assert.Equal (text, inner);
			}
		}
	}
}
