using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Xunit;

namespace Mono.WasmPackager.TestSuite
{
	using Messaging.Debugger;

	public abstract class PuppeteerTestBase : InspectorTestBase
	{
		readonly Dictionary<string, BreakpointInfo> breakpoints;

		protected PuppeteerTestBase (Assembly caller = null)
			: base (caller ?? Assembly.GetCallingAssembly ())
		{
			breakpoints = new Dictionary<string, BreakpointInfo> ();
		}

		protected Task Resume () => SendCommand (new ResumeRequest ());

		protected Task StepOver () => SendCommand (new StepOverRequest ());

		protected Task StepOut () => SendCommand (new StepOutRequest ());

		protected Task<string> WaitForConsole (string message, bool regex = false)
		{
			var tcs = new TaskCompletionSource<string> ();
			var rx = regex ? new Regex (message, RegexOptions.Compiled) : null;

			void Handler (object sender, ConsoleEventArgs e)
			{
				if ((regex && rx.IsMatch (e.Message.Text)) || e.Message.Text.Equals (message)) {
					InternalPage.Console -= Handler;
					tcs.TrySetResult (e.Message.Text);
				}
			};

			InternalPage.Console += Handler;

			return CheckedWait (tcs.Task);
		}

		protected async Task<string> WaitForException (string exception, string message = null)
		{
			var consoleTcs = new TaskCompletionSource<string> ();
			var errorTcs = new TaskCompletionSource<string> ();
			var seenUnhandled = false;

			void Unexpected (string text)
			{
				InternalPage.Console -= ConsoleHandler;
				var exception = new InvalidOperationException ($"Received unexpected text '{text}'.");
				consoleTcs.TrySetException (exception);
				errorTcs.TrySetException (exception);
			}

			void ConsoleHandler (object sender, ConsoleEventArgs e)
			{
				if (e.Message.Text.Equals ("Unhandled Exception:")) {
					if (!seenUnhandled)
						seenUnhandled = true;
					else
						Unexpected (e.Message.Text);
					return;
				} else if (!seenUnhandled) {
					return;
				}

				if (!e.Message.Text.StartsWith (exception + ": ")) {
					Unexpected (e.Message.Text);
					return;
				}

				if (!string.IsNullOrEmpty (message)) {
					var end = e.Message.Text.Substring (exception.Length + 2);
					if (!end.Equals (message))
						Unexpected (e.Message.Text);
				}

				InternalPage.Console -= ConsoleHandler;
				consoleTcs.TrySetResult (e.Message.Text);
			};

			void ErrorHandler (object sender, ErrorEventArgs e)
			{
				var exception = new InvalidOperationException ($"Got unexpected 'Error' event: {e.Error}");
				InternalPage.Error -= ErrorHandler;
				consoleTcs.TrySetException (exception);
				errorTcs.TrySetException (exception);
			}

			void PageErrorHandler (object sender, PageErrorEventArgs e)
			{
				InternalPage.PageError -= PageErrorHandler;
				errorTcs.TrySetResult (e.Message);
			}

			InternalPage.Console += ConsoleHandler;
			InternalPage.Error += ErrorHandler;
			InternalPage.PageError += PageErrorHandler;

			var result = await CheckedWaitAll (consoleTcs.Task, errorTcs.Task).ConfigureAwait (false);
			return result [1];
		}

		protected async Task<string> ClickAndWaitForMessage (string selector, string message, bool regex = false)
		{
			var button = await QuerySelectorAsync (selector).ConfigureAwait (false);
			var wait = WaitForConsole (message, regex);
			var click = button.ClickAsync ();
			await CheckedWaitAll (wait, click).ConfigureAwait (false);
			return wait.Result;
		}

		protected async Task<string> ClickAndWaitForException (string selector, string exception, string message = null)
		{
			var button = await QuerySelectorAsync (selector).ConfigureAwait (false);
			var wait = WaitForException (exception, message);
			var click = button.ClickAsync ();
			await CheckedWaitAll (wait, click).ConfigureAwait (false);
			return wait.Result;
		}

		protected async Task ClickWithPausedNotification (string selector, Func<PausedNotification, Task> action)
		{
			var waitForPaused = WaitForPaused ();

			var button = await QuerySelectorAsync (selector).ConfigureAwait (false);
			var click = button.ClickAsync ();

			var pausedNotification = await waitForPaused.ConfigureAwait (false);
			await action (pausedNotification).ConfigureAwait (false);

			var waitForResumed = WaitForResumed ();
			await Resume ().ConfigureAwait (false);

			await CheckedWaitAll (waitForResumed, click).ConfigureAwait (false);
		}

		protected async Task<string> GetInnerHtml (string selector)
		{
			var handle = await QuerySelectorAsync (selector).ConfigureAwait (false);
			var property = await CheckedWait (handle.GetPropertyAsync ("innerHTML")).ConfigureAwait (false);
			var value = property.RemoteObject.Value;
			return value.Value<string> ();
		}

		protected async Task AssertInnerHtml (string selector, string expected)
		{
			var handle = await QuerySelectorAsync (selector).ConfigureAwait (false);
			Assert.NotNull (handle);
			var inner = await CheckedWait (handle.GetInnerHtml ()).ConfigureAwait (false);
			Assert.Equal (expected, inner);
		}

		protected async Task GetPossibleBreakpoints (string file)
		{
			var request = new GetPossibleBreakpointsRequest {
				Start = new Location {
					ScriptId = FileToId [$"dotnet://{Settings.DevServer_Assembly}/{file}"],
					LineNumber = 0,
					ColumnNumber = 0
				}
			};

			var response = await SendCommand (request).ConfigureAwait (false);
			Assert.True (response.Locations.Length > 1);
		}

		(string, string) LookupLocation (SourceLocation location)
		{
			string url;
			if (location.IsNative) {
				url = new Uri (ServerRoot, location.File).ToString ();
			} else {
				var fileUrl = $"dotnet://{Settings.DevServer_Assembly}/{location.FullPath}";
				url = FileToUrl [fileUrl];
			}

			var scriptId = UrlToScriptId [url];
			return (url, scriptId);
		}

		protected async Task<BreakpointInfo> InsertBreakpoint (SourceLocation location)
		{
			var (url, scriptId) = LookupLocation (location);

			var request = new InsertBreakpointRequest {
				LineNumber = location.Line - 1,
				Url = url
			};

			var result = await SendCommand (request).ConfigureAwait (false);
			Assert.EndsWith (url, result.BreakpointId);
			Assert.Single (result.Locations);
			Assert.Equal (location.Line - 1, result.Locations [0].LineNumber);
			Assert.Equal (scriptId, result.Locations [0].ScriptId);

			var breakpoint = new BreakpointInfo (location, url, scriptId, result.BreakpointId);
			breakpoints.Add (breakpoint.Id, breakpoint);
			return breakpoint;
		}

		protected async Task RemoveBreakpoint (BreakpointInfo breakpoint)
		{
			breakpoints.Remove (breakpoint.Id);
			var request = new RemoveBreakpointRequest { BreakpointId = breakpoint.Id };
			await SendCommand (request).ConfigureAwait (false);
		}

		protected void AssertBreakpointFrame (BreakpointInfo breakpoint, CallFrame frame)
		{
			AssertBreakpointFrame (breakpoint.Location, breakpoint.Url, breakpoint.ScriptId, frame);
		}

		protected void AssertBreakpointFrame (SourceLocation location, CallFrame frame)
		{
			var (url, scriptId) = LookupLocation (location);
			AssertBreakpointFrame (location, url, scriptId, frame);
		}

		void AssertBreakpointFrame (SourceLocation location, string url, string scriptId, CallFrame frame)
		{
			Assert.Equal (location.FunctionName, frame.FunctionName);
			Assert.Equal (url, frame.Url);
			Assert.NotNull (frame.Location);
			Assert.Equal (scriptId, frame.Location.ScriptId);
			Assert.Equal (location.Line, frame.Location.LineNumber + 1);
			if (location.Column != null)
				Assert.Equal (location.Column.Value, frame.Location.ColumnNumber + 1);
			Assert.True (frame.ScopeChain.Length > 0);
			var scope = frame.ScopeChain [0];
			Assert.Equal (location.FunctionName, scope.Name);
			Assert.Equal (ScopeType.Local, scope.Type);
			if (location.ScopeStart != null)
				Assert.Equal (location.ScopeStart.Value, scope.StartLocation.LineNumber + 1);
			if (location.ScopeEnd != null)
				Assert.Equal (location.ScopeEnd.Value, scope.EndLocation.LineNumber + 1);
			Assert.NotNull (scope.Object);
		}

		protected void AssertBreakpointHit (BreakpointInfo breakpoint, PausedNotification notification)
		{
			Assert.NotNull (notification.HitBreakpoints);
			Assert.Single (notification.HitBreakpoints);

			Assert.EndsWith (breakpoint.Location.File, notification.HitBreakpoints [0].ToString ());
			Assert.Equal (breakpoint.Id, notification.HitBreakpoints [0]);
			Assert.Equal (StoppedReason.Other, notification.Reason);
			Assert.True (notification.CallFrames.Length > 0);

			AssertBreakpointFrame (breakpoint, notification.CallFrames [0]);
		}
	}
}
