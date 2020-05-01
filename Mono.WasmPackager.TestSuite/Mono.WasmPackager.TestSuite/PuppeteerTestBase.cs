using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Xunit;

namespace Mono.WasmPackager.TestSuite
{
	using Messaging.Debugger;

	public abstract class PuppeteerTestBase : InspectorTestBase
	{
		protected PuppeteerTestBase (Assembly caller = null)
			: base (caller ?? Assembly.GetCallingAssembly ())
		{
		}

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
			var request = new GetPossibleBreakpointsRequest {
				Start = new Location {
					ScriptId = FileToId [$"dotnet://{Settings.DevServer_Assembly}/{file}"],
					LineNumber = 0,
					ColumnNumber = 0
				}
			};

			var response = await SendCommand<GetPossibleBreakpointsResponse> ("Debugger.getPossibleBreakpoints", request);
			Assert.True (response.Locations.Length > 1);
		}

		protected Task<string> InsertBreakpoint (SourceLocation location) => InsertBreakpoint (location.File, location.Line);

		protected async Task<string> InsertBreakpoint (string file, int line)
		{
			var fileUrl = $"dotnet://{Settings.DevServer_Assembly}/{file}";
			var request = new InsertBreakpointRequest {
				LineNumber = line,
				Url = FileToUrl [fileUrl]
			};

			var result = await SendCommand<InsertBreakpointResponse> ("Debugger.setBreakpointByUrl", request);
			Assert.EndsWith (file, result.BreakpointId);
			Assert.Single (result.Locations);
			Assert.Equal (line, result.Locations [0].LineNumber);
			Assert.Equal (FileToId [fileUrl], result.Locations [0].ScriptId);
			return result.BreakpointId;
		}

		protected async Task RemoveBreakpoint (string breakpointId)
		{
			var request = new RemoveBreakpointRequest { BreakpointId = breakpointId };
			await SendCommand<RemoveBreakpointResponse> ("Debugger.removeBreakpoint", request);
		}

		protected void AssertBreakpointFrame (SourceLocation location, CallFrame frame)
		{
			var scriptId = FileToId [$"dotnet://{Settings.DevServer_Assembly}/{location.File}"];

			Assert.Equal (location.FunctionName, frame.FunctionName);
			Assert.EndsWith (location.File, frame.Url);
			Assert.NotNull (frame.Location);
			Assert.Equal (scriptId, frame.Location.ScriptId);
			Assert.Equal (location.Line, frame.Location.LineNumber);
			if (location.Column != null)
				Assert.Equal (location.Column.Value, frame.Location.ColumnNumber);
			Assert.True (frame.ScopeChain.Length > 0);
			var scope = frame.ScopeChain [0];
			Assert.Equal (location.FunctionName, scope.Name);
			Assert.Equal (ScopeType.Local, scope.Type);
			if (location.ScopeStart != null)
				Assert.Equal (location.ScopeStart.Value, scope.StartLocation.LineNumber);
			if (location.ScopeEnd != null)
				Assert.Equal (location.ScopeEnd.Value, scope.EndLocation.LineNumber);
			Assert.NotNull (scope.Object);
		}
	}
}
