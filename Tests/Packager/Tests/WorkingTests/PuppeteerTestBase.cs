using System;
using System.Threading;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.DevServer;

namespace WorkingTests
{
	public abstract class PuppeteerTestBase : InspectorTestBase
	{
		// Keep in sync with the javascript side
		protected const string MessageText = "MESSAGE BUTTON CLICKED";
		protected const string MessageText2 = "MESSAGE BUTTON CLICKED - BACK FROM MANAGED";
		protected const string TextReady = "READY";
		protected const string TextMessage = "MESSAGE";
		protected const string ThrowMessage = "THROW";

		protected const int DefaultTimeout = 15;

		protected Task<string> WaitForConsole (string message, bool regex = false)
		{
			var tcs = new TaskCompletionSource<string> ();
			var rx = regex ? new Regex (message, RegexOptions.Compiled) : null;

			EventHandler<ConsoleEventArgs> handler = null;
			handler = (sender, e) => {
				if ((regex && rx.IsMatch (e.Message.Text)) || e.Message.Text.Equals (message)) {
					Page.Console -= handler;
					tcs.TrySetResult (e.Message.Text);
				}
			};

			Page.Console += handler;

			return tcs.Task;
		}

		protected async Task<string> WaitForException (string exception, string message = null)
		{
			var consoleTcs = new TaskCompletionSource<string> ();
			var errorTcs = new TaskCompletionSource<string> ();
			var seenUnhandled = false;

			EventHandler<ConsoleEventArgs> consoleHandler = null;
			EventHandler<ErrorEventArgs> errorHandler = null;
			EventHandler<PageErrorEventArgs> pageErrorHandler = null;

			Action<string> unexpected = text => {
				Page.Console -= consoleHandler;
				var exception = new InvalidOperationException ($"Received unexpected text '{text}'.");
				consoleTcs.TrySetException (exception);
				errorTcs.TrySetException (exception);
			};

			consoleHandler = (sender, e) => {
				if (e.Message.Text.Equals ("Unhandled Exception:")) {
					if (!seenUnhandled)
						seenUnhandled = true;
					else
						unexpected (e.Message.Text);
					return;
				} else if (!seenUnhandled) {
					return;
				}

				if (!e.Message.Text.StartsWith (exception + ": ")) {
					unexpected (e.Message.Text);
					return;
				}

				if (!string.IsNullOrEmpty (message)) {
					var end = e.Message.Text.Substring (exception.Length + 2);
					if (!end.Equals (message))
						unexpected (e.Message.Text);
				}

				Page.Console -= consoleHandler;
				consoleTcs.TrySetResult (e.Message.Text);
			};

			errorHandler = (sender, e) => {
				var exception = new InvalidOperationException ($"Got unexpected 'Error' event: {e.Error}");
				Page.Error -= errorHandler;
				consoleTcs.TrySetException (exception);
				errorTcs.TrySetException (exception);
			};

			pageErrorHandler += (sender, e) => {
				Page.PageError -= pageErrorHandler;
				errorTcs.TrySetResult (e.Message);
			};

			Page.Console += consoleHandler;
			Page.Error += errorHandler;
			Page.PageError += pageErrorHandler;

			var result = await Task.WhenAll (consoleTcs.Task, errorTcs.Task).ConfigureAwait (false);
			return result [1];
		}

		protected async Task<string> ClickAndWaitForMessage (string selector, string message, bool regex = false)
		{
			var button = await Page.QuerySelectorAsync (selector);
			var wait = WaitForConsole (message, regex);
			var click = button.ClickAsync ();
			await Task.WhenAll (wait, click).ConfigureAwait (false);
			return wait.Result;
		}

		protected async Task<string> ClickAndWaitForException (string selector, string exception, string message = null)
		{
			var button = await Page.QuerySelectorAsync (selector);
			var wait = WaitForException (exception, message);
			var click = button.ClickAsync ();
			await Task.WhenAll (wait, click).ConfigureAwait (false);
			return wait.Result;
		}

		protected async Task<string> GetInnerHtml (string selector)
		{
			var handle = await Page.QuerySelectorAsync (selector);
			var property = await handle.GetPropertyAsync ("innerHTML");
			var value = property.RemoteObject.Value;
			return value.Value<string> ();
		}

		protected async Task GetPossibleBreakpoints (string file)
		{
			var bp1_req = JObject.FromObject (new {
				start = JObject.FromObject (new {
					scriptId = FileToId [$"dotnet://{Settings.DevServer_Assembly}/{file}"],
					lineNumber = 0,
					columnNumber = 0
				})
			});

			var bp1_res = await SendCommand ("Debugger.getPossibleBreakpoints", bp1_req);
			Assert.True (bp1_res.IsOk);
		}

		protected async Task<string> InsertBreakpoint (string file, int line)
		{
			var request = JObject.FromObject (new
			{
				lineNumber = line,
				url = FileToUrl[$"dotnet://{Settings.DevServer_Assembly}/{file}"],
			});

			var result = await SendCommand ("Debugger.setBreakpointByUrl", request);
			Assert.True (result.IsOk);
			var breakpointId = (string)result.Value ["breakpointId"];
			Assert.EndsWith (file, breakpointId);
			Assert.Equal (1, result.Value["locations"]?.Value<JArray> ()?.Count);
			return breakpointId;
		}

		protected async Task RemoveBreakpoint (string breakpointId)
		{
			var request = JObject.FromObject (new
			{
				breakpointId = breakpointId
			});

			var result = await SendCommand ("Debugger.removeBreakpoint", request);
			Assert.True (result.IsOk);
		}
	}
}
