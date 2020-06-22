using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Xunit;

using Mono.WasmPackager.TestSuite;
using Mono.WasmPackager.TestSuite.Messaging;
using Mono.WasmPackager.TestSuite.Messaging.Debugger;
using Mono.WasmPackager.TestSuite.Messaging.Runtime;
using Mono.WasmPackager.DevServer;

namespace SharedTests
{
	public abstract class TestExceptionVariables : PuppeteerTestBase
	{
		const string Message = "MY ERROR";

		[Flags]
		enum TestFlags
		{
			None = 0,
			UsePuppeteer = 1,
			IsJs = 2,
			Caught = 4,
			Thrown = 8,
			Silent = 16
		}

		enum TestType
		{
			ExceptionInstance,
			InstanceMethod,
			InstanceProperty,
			CallFunction,
			Evaluate
		}

		struct TestParameters
		{
			public readonly SourceLocation Location;
			public readonly SourceLocation ThrownLocation;
			public readonly string Selector;
			public readonly TestFlags Flags;
			public readonly TestType Type;

			public bool UsePuppeteer => (Flags & TestFlags.UsePuppeteer) != 0;
			public bool Caught => (Flags & TestFlags.Caught) != 0;
			public bool Thrown => (Flags & TestFlags.Thrown) != 0;
			public bool IsJs => (Flags & TestFlags.IsJs) != 0;
			public bool Silent => (Flags & TestFlags.Silent) != 0;

			public string HelloName => IsJs ? "hello" : "Hello";
			public string NumberName => IsJs ? "number" : "Number";
			public string FooName => IsJs ? "foo" : "Foo";
			public string MessageName => IsJs ? "message" : "Message";
			public string StackName => IsJs ? "stack" : "Stack";
			public string ToStringName => IsJs ? "toString" : "ToString";

			public string ClassName => IsJs ? "MyError" : TestConstants.MyErrorClassName;
			public string InstanceClassName => IsJs ? "Foo" : TestConstants.MyInstanceClassName;
			public readonly string VariableName;
			public string ThrowErrorName => "throwError";
			public string ErrorPropName => "errorProperty";

			public string ExpectedMessageWithStack {
				get {
					var throwLocation = Type switch
					{
						TestType.InstanceMethod => $"{InstanceClassName}.{ThrowErrorName}",
						TestType.InstanceProperty => $"{InstanceClassName}.{(IsJs ? "get errorProperty" : "ErrorProperty")}",
						TestType.CallFunction => $"{InstanceClassName}.{ThrowErrorName}",
						TestType.Evaluate => $"{InstanceClassName}.{ThrowErrorName}",
						_ => Location.FunctionName
					};

					if (IsJs)
						return $"Error: {Message}\n    at {throwLocation} ";
					else if (Thrown)
						return $"Error: {Message}\n    at {TestConstants.VariablesClassName}.{throwLocation} () ";
					else // We only have a managed stack trace after the exception has been thrown.
						return $"Error: {Message}\n";
				}
			}

			public TestParameters (SourceLocation location, string selector, TestFlags flags, TestType type = TestType.ExceptionInstance)
			{
				Location = location;
				Selector = selector;
				Flags = flags;
				Type = type;

				VariableName = type switch
				{
					TestType.InstanceMethod => "foo",
					TestType.InstanceProperty => "foo",
					TestType.ExceptionInstance => "myError",
					TestType.CallFunction => "foo",
					TestType.Evaluate => "foo",
					_ => throw new ArgumentException ($"Invalid {nameof (type)} value: {type}.")
				};

				ThrownLocation = type switch
				{
					TestType.InstanceMethod => TestSettings.Locations.FooThrowError,
					TestType.InstanceProperty => TestSettings.Locations.FooErrorProperty,
					TestType.CallFunction => TestSettings.Locations.FooThrowError,
					TestType.Evaluate => TestSettings.Locations.FooThrowError,
					_ => null
				};
			}

			public TestParameters (SourceLocation location, string selector, bool usePuppeteer, TestFlags flags, TestType variable = TestType.ExceptionInstance)
				: this (location, selector, usePuppeteer ? flags | TestFlags.UsePuppeteer : flags, variable)
			{
			}

			public override string ToString () => $"[{GetType ().Name} Flags = {Flags}]";
		}

		// The .NET Test Explorer has some issues with [Theory]'s, so we're using multiple [Fact]'s instead.
		protected Task SharedJsException (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsVariables, TestConstants.Selectors.JsVariables, usePuppeteer, TestFlags.IsJs));

		protected Task SharedJsCaughtException (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsVariables, TestConstants.Selectors.JsVariables, usePuppeteer, TestFlags.Caught | TestFlags.IsJs));

		protected Task SharedJsSilentException (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsVariables, TestConstants.Selectors.JsVariables, usePuppeteer, TestFlags.Caught | TestFlags.Silent | TestFlags.IsJs));

		protected Task SharedJsThrownException (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsException, TestConstants.Selectors.JsException, usePuppeteer, TestFlags.Caught | TestFlags.Thrown | TestFlags.IsJs));

		protected Task SharedJsInstanceMethod (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsVariables, TestConstants.Selectors.JsVariables, usePuppeteer, TestFlags.IsJs, TestType.InstanceMethod));

		protected Task SharedJsInstanceProperty (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsVariables, TestConstants.Selectors.JsVariables, usePuppeteer, TestFlags.IsJs, TestType.InstanceProperty));

		protected Task SharedJsCaughtInstanceMethod (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsVariables, TestConstants.Selectors.JsVariables, usePuppeteer, TestFlags.Caught | TestFlags.IsJs, TestType.InstanceMethod));

		protected Task SharedJsCaughtInstanceProperty (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsVariables, TestConstants.Selectors.JsVariables, usePuppeteer, TestFlags.Caught | TestFlags.IsJs, TestType.InstanceProperty));

		protected Task SharedJsCallFunction (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsVariables, TestConstants.Selectors.JsVariables, usePuppeteer, TestFlags.Caught | TestFlags.IsJs, TestType.CallFunction));

		protected Task SharedJsEvaluate (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsVariables, TestConstants.Selectors.JsVariables, usePuppeteer, TestFlags.IsJs, TestType.Evaluate));

		protected Task SharedJsEvaluateCaught (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsVariables, TestConstants.Selectors.JsVariables, usePuppeteer, TestFlags.Caught | TestFlags.IsJs, TestType.Evaluate));

		protected Task SharedJsEvaluateSilent (bool usePuppeteer) => SharedTestException (
			new TestParameters (TestSettings.Locations.JsVariables, TestConstants.Selectors.JsVariables, usePuppeteer, TestFlags.Caught | TestFlags.Silent | TestFlags.IsJs, TestType.Evaluate));

		protected Task SharedManagedException () => SharedTestException (
			new TestParameters (TestSettings.Locations.ExceptionVariable, TestConstants.Selectors.ExceptionVariable, TestFlags.None));

		protected Task SharedManagedCaughtException () => SharedTestException (
			new TestParameters (TestSettings.Locations.ExceptionVariable, TestConstants.Selectors.ExceptionVariable, TestFlags.Caught));

		protected Task SharedManagedThrownException () => SharedTestException (
			new TestParameters (
				TestSettings.Locations.ThrowExceptionVariable, TestConstants.Selectors.ThrowExceptionVariable, TestFlags.Caught | TestFlags.Thrown));

		async Task SharedTestException (TestParameters parameters)
		{
			Assert.Equal (TestConstants.TextReady, await GetInnerHtml (TestConstants.Selectors.Output));

			Task secondPause = null;
			Task secondResume = null;
			BreakpointInfo breakpoint = null;

			if (parameters.Caught) {
				await SendBrowserOrProxy (parameters.UsePuppeteer, new DebuggerEnableRequest ());
				await SendBrowserOrProxy (parameters.UsePuppeteer, new SetPauseOnExceptionsRequest { State = PauseOnExceptionMode.All });
			}

			if (!parameters.Thrown) {
				breakpoint = await InsertBreakpoint (parameters.Location).ConfigureAwait (false);
			}

			void AssertStopReason (PausedNotification notification)
			{
				if (parameters.Thrown) {
					Assert.Empty (notification.HitBreakpoints);
					Assert.Equal (StoppedReason.Exception, notification.Reason);
				} else {
					AssertBreakpointHit (breakpoint, notification);
				}
			}

			async Task OnNotification (PausedNotification notification)
			{
				AssertStopReason (notification);

				var frame = notification.CallFrames [0];
				await AssertVariable (frame, parameters).ConfigureAwait (false);

				if (parameters.Thrown) {
					Assert.NotNull (notification.Data);
					var data = notification.Data.ToObject<PausedExceptionData> ();
					Assert.NotNull (data);
					AssertExceptionData (data, parameters);
				}

				if (!parameters.IsJs && parameters.Thrown) {
					// FIXME: we're stopping a second time in dotnet.js
					secondPause = WaitForPaused ().ContinueWith (OnSecondPause);
				}
			}

			async Task OnSecondPause (Task<PausedNotification> task)
			{
				var notification = task.Result;
				Assert.Equal (StoppedReason.Exception, notification.Reason);
				Assert.NotNull (notification.Data);
				var secondData = notification.Data.ToObject<PausedExceptionData> ();
				Assert.Equal (RemoteObjectType.Object, secondData.Type);
				Assert.Equal (RemoteObjectSubType.Error, secondData.SubType);
				Assert.Equal ("Error", secondData.ClassName);
				Assert.True (secondData.Uncaught);
				var topFrame = notification.CallFrames [0];
				Assert.Equal ("call_method", topFrame.FunctionName);
				var dotnetUrl = new Uri (ServerRoot, "dotnet.js");
				Assert.Equal (dotnetUrl.ToString (), notification.CallFrames [0].Url);
				secondResume = WaitForResumed ();
				await Resume ().ConfigureAwait (false);
			}

			await ClickWithPausedNotification (parameters.Selector, OnNotification).ConfigureAwait (false);

			if (!parameters.IsJs && parameters.Thrown) {
				await CheckedWait (secondPause).ConfigureAwait (false);
				await CheckedWait (secondResume).ConfigureAwait (false);
			}
		}

		async Task<GetPropertiesResponse> GetProperties (CallFrame frame)
		{
			var scope = frame.ScopeChain.First (s => s.Type == ScopeType.Local);

			var properties = await SendCommand (new GetPropertiesRequest { ObjectId = scope.Object.ObjectId }).ConfigureAwait (false);
			Assert.Null (properties.ExceptionDetails);
			Assert.Null (properties.InternalProperties);
			Assert.Null (properties.PrivateProperties);
			Assert.Null (properties.ObjectId);
			Assert.NotEmpty (properties.Result);
			return properties;
		}

		async Task AssertVariable (CallFrame frame, TestParameters parameters)
		{
			var properties = await GetProperties (frame).ConfigureAwait (false);
			var property = properties.Result.First (p => p.Name == parameters.VariableName);
			AssertProperty (parameters.VariableName, property, parameters.IsJs);

			switch (parameters.Type) {
			case TestType.InstanceMethod:
			case TestType.InstanceProperty:
				await AssertInstanceVariable (frame, property, parameters).ConfigureAwait (false);
				break;
			case TestType.CallFunction:
				await AssertCallFunction (property, parameters).ConfigureAwait (false);
				break;
			case TestType.Evaluate:
				await AssertEvaluate (property, parameters).ConfigureAwait (false);
				break;
			case TestType.ExceptionInstance:
				await AssertExceptionVariable (frame, property, parameters).ConfigureAwait (false);
				break;
			default:
				throw new ArgumentException ($"Invalid TestType: '{parameters.Type}'.");
			}
		}

		async Task AssertExceptionVariable (CallFrame frame, PropertyDescriptor property, TestParameters parameters)
		{
			AssertExceptionObject (property.Value, parameters);

			await AssertExceptionVariableDetails (property.Value.ObjectId, parameters).ConfigureAwait (false);

			await AssertSuccess (frame, parameters.VariableName, parameters).ConfigureAwait (false);

			// Unfortunately, expression evaluation is still very incomplete and don't support any object types.
			if (!parameters.IsJs)
				return;

			var errorResult = await SendBrowserOrProxy (parameters.UsePuppeteer, new EvaluateOnCallFrameRequest {
				CallFrameId = frame.CallFrameId,
				Expression = $"throw {parameters.VariableName}",
				Silent = parameters.Silent
			});
			AssertErrorResult (errorResult, parameters);

			var messageResult = await SendBrowserOrProxy (parameters.UsePuppeteer, new EvaluateOnCallFrameRequest {
				CallFrameId = frame.CallFrameId,
				Expression = $"{parameters.VariableName}.{parameters.MessageName}",
				Silent = parameters.Silent
			});
			AssertStringResult (messageResult, Message, parameters.IsJs);

			var toStringResult = await SendBrowserOrProxy (parameters.UsePuppeteer, new EvaluateOnCallFrameRequest {
				CallFrameId = frame.CallFrameId,
				Expression = $"{parameters.VariableName}.{parameters.ToStringName}()",
				Silent = parameters.Silent
			});
			AssertStringResult (toStringResult, $"Error: {Message}", parameters.IsJs);
		}

		async Task AssertInstanceVariable (CallFrame frame, PropertyDescriptor property, TestParameters parameters)
		{
			AssertInstanceObject (property.Value, parameters);
			await AssertInstanceVariableDetails (property.Value.ObjectId, parameters).ConfigureAwait (false);

			var instanceResponse = await SendBrowserOrProxy (parameters.UsePuppeteer, new EvaluateOnCallFrameRequest {
				CallFrameId = frame.CallFrameId,
				Expression = parameters.VariableName,
				Silent = parameters.Silent
			});

			Assert.NotNull (instanceResponse);
			Assert.NotNull (instanceResponse.Result);
			Assert.Null (instanceResponse.ExceptionDetails);
			AssertInstanceObject (instanceResponse.Result, parameters);

			var expression = parameters.Type switch
			{
				TestType.InstanceMethod => $"{parameters.VariableName}.{parameters.ThrowErrorName}()",
				TestType.InstanceProperty => $"{parameters.VariableName}.{parameters.ErrorPropName}",
				_ => throw new InvalidOperationException ()
			};

			var errorResult = await SendBrowserOrProxy (parameters.UsePuppeteer, new EvaluateOnCallFrameRequest {
				CallFrameId = frame.CallFrameId,
				Expression = expression,
				Silent = parameters.Silent
			});
			AssertErrorResult (errorResult, parameters);
		}

		async Task AssertCallFunction (PropertyDescriptor property, TestParameters parameters)
		{
			AssertInstanceObject (property.Value, parameters);
			await AssertInstanceVariableDetails (property.Value.ObjectId, parameters).ConfigureAwait (false);

			var callResult = await SendBrowserOrProxy (parameters.UsePuppeteer, new CallFunctionOnRequest {
				ObjectId = property.Value.ObjectId,
				FunctionDeclaration = "function() { new Foo().throwError(); }",
				Silent = parameters.Silent
			});

			Debug.WriteLine ($"CALL RESULT: {callResult}");
			Assert.NotNull (callResult.Result);
			Assert.NotNull (callResult.ExceptionDetails);
			AssertExceptionObject (callResult.Result, parameters);
			AssertExceptionDetails (callResult.ExceptionDetails, parameters);
		}

		async Task AssertEvaluate (PropertyDescriptor property, TestParameters parameters)
		{
			AssertInstanceObject (property.Value, parameters);
			await AssertInstanceVariableDetails (property.Value.ObjectId, parameters).ConfigureAwait (false);

			var evalResult = await SendBrowserOrProxy (parameters.UsePuppeteer, new EvaluateRequest {
				Expression = "new Foo ().throwError ()",
				Silent = parameters.Silent
			});

			Debug.WriteLine ($"EVAL RESULT: {evalResult}");
			Assert.NotNull (evalResult.Result);
			Assert.NotNull (evalResult.ExceptionDetails);

			AssertExceptionObject (evalResult.Result, parameters);
			AssertExceptionDetails (evalResult.ExceptionDetails, parameters);
		}

		async Task AssertExceptionVariableDetails (string objectId, TestParameters parameters)
		{
			var details = await SendCommand (new GetPropertiesRequest { ObjectId = objectId }).ConfigureAwait (false);
			Assert.Null (details.ExceptionDetails);
			Assert.Null (details.InternalProperties);
			Assert.Null (details.PrivateProperties);
			Assert.Null (details.ObjectId);
			Assert.NotEmpty (details.Result);

			var hello = details.Result.First (p => p.Name == parameters.HelloName);
			AssertProperty (parameters.HelloName, hello, parameters.IsJs);
			AssertStringObject ("World", parameters.IsJs, hello.Value);

			var foo = details.Result.First (p => p.Name == parameters.FooName);
			AssertProperty (parameters.FooName, foo, parameters.IsJs);
			AssertNumberObject (999, foo.Value);

			var message = details.Result.First (p => p.Name == parameters.MessageName);
			AssertProperty (parameters.MessageName, message, parameters.IsJs, enumerable: false);
			AssertStringObject (Message, parameters.IsJs, message.Value);

			var stack = details.Result.First (p => p.Name == parameters.StackName);
			AssertProperty (parameters.StackName, stack, parameters.IsJs, enumerable: false);
			Assert.Equal (RemoteObjectType.String, stack.Value.Type);
			Assert.Equal (RemoteObjectSubType.None, stack.Value.SubType);
			Assert.Null (stack.Value.ClassName);
			Assert.Null (stack.Value.ObjectId);

			// We only have a managed stack trace after the exception has been thrown.
			if (parameters.IsJs || parameters.Caught) {
				if (parameters.IsJs)
					Assert.Null (stack.Value.Description);
				Assert.StartsWith (parameters.ExpectedMessageWithStack, (string)stack.Value.Value);
			}
		}

		async Task AssertInstanceVariableDetails (string objectId, TestParameters parameters)
		{
			var details = await SendCommand (new GetPropertiesRequest { ObjectId = objectId }).ConfigureAwait (false);
			Assert.Null (details.ExceptionDetails);
			Assert.Null (details.InternalProperties);
			Assert.Null (details.PrivateProperties);
			Assert.Null (details.ObjectId);
			Assert.NotEmpty (details.Result);

			var number = details.Result.First (p => p.Name == parameters.NumberName);
			AssertProperty (parameters.NumberName, number, parameters.IsJs);
			AssertNumberObject (8888, number.Value);
		}

		async Task AssertSuccess (CallFrame frame, string name, TestParameters parameters)
		{
			var response = await SendBrowserOrProxy (parameters.UsePuppeteer, new EvaluateOnCallFrameRequest {
				CallFrameId = frame.CallFrameId,
				Expression = name,
				Silent = parameters.Silent
			});

			Assert.NotNull (response);
			Assert.NotNull (response.Result);
			Assert.Null (response.ExceptionDetails);
			AssertExceptionObject (response.Result, parameters);
		}

		void AssertExceptionDetails (ExceptionDetails details, TestParameters parameters)
		{
			Assert.NotNull (details);
			Assert.Equal ("Uncaught", details.Text);
			Assert.Null (details.Url);
			Assert.NotNull (details.ScriptId);
			AssertExceptionObject (details.Exception, parameters);

			if (parameters.ThrownLocation != null) {
				Assert.Equal (parameters.ThrownLocation.Line, details.LineNumber);
			} else {
				Assert.Equal (0, details.LineNumber);
				Assert.Equal (0, details.ColumnNumber);
			}
		}

		void AssertErrorResult (EvaluateOnCallFrameResponse response, TestParameters parameters)
		{
			Assert.NotNull (response);
			Assert.NotNull (response.Result);
			AssertExceptionObject (response.Result, parameters);
			AssertExceptionDetails (response.ExceptionDetails, parameters);
		}

		void AssertStringResult (EvaluateOnCallFrameResponse response, string name, bool isJs)
		{
			Assert.NotNull (response);
			Assert.NotNull (response.Result);
			Assert.Null (response.ExceptionDetails);
			AssertStringObject (name, isJs, response.Result);
		}

		void AssertProperty (string name, PropertyDescriptor prop, bool isJs, bool? enumerable = null)
		{
			Assert.Equal (name, prop.Name);
			if (enumerable ?? isJs)
				Assert.True (prop.Enumerable);
			else
				Assert.False (prop.Enumerable);
			if (isJs) {
				Assert.True (prop.Configurable);
				Assert.True (prop.IsOwn);
				Assert.True (prop.Writable);
			} else {
				Assert.False (prop.Configurable);
				Assert.False (prop.Enumerable);
				Assert.False (prop.IsOwn);
				Assert.False (prop.Writable);
			}
			Assert.Null (prop.Get);
			Assert.Null (prop.Set);
			Assert.Null (prop.Symbol);
			Assert.False (prop.WasThrown);
			Assert.NotNull (prop.Value);
		}

		void AssertExceptionObject (RemoteObject obj, TestParameters parameters)
		{
			Assert.Equal (RemoteObjectType.Object, obj.Type);
			Assert.Equal (RemoteObjectSubType.Error, obj.SubType);
			// The message needs to end with a newline or it won't be displayed.
			Assert.StartsWith (parameters.ExpectedMessageWithStack, obj.Description);
			Assert.Equal (parameters.ClassName, obj.ClassName);
			if (parameters.IsJs)
				Assert.NotNull (obj.ObjectId);
			else
				Assert.StartsWith ("dotnet:exception:", obj.ObjectId);
		}

		void AssertInstanceObject (RemoteObject obj, TestParameters parameters)
		{
			Assert.Equal (RemoteObjectType.Object, obj.Type);
			Assert.Equal (RemoteObjectSubType.None, obj.SubType);
			Assert.Equal (parameters.InstanceClassName, obj.ClassName);
			Assert.Equal (parameters.InstanceClassName, obj.Description);
			if (parameters.IsJs)
				Assert.NotNull (obj.ObjectId);
			else
				Assert.StartsWith ("dotnet:object:", obj.ObjectId);
		}

		void AssertStringObject (string value, bool isJs, RemoteObject obj)
		{
			Assert.Equal (RemoteObjectType.String, obj.Type);
			Assert.Equal (RemoteObjectSubType.None, obj.SubType);
			Assert.Null (obj.ClassName);
			if (isJs)
				Assert.Null (obj.Description);
			else
				Assert.Equal (value, obj.Description);
			Assert.Null (obj.ObjectId);
			Assert.Equal (value, obj.Value);
		}

		void AssertNumberObject (long value, RemoteObject obj)
		{
			Assert.Equal (RemoteObjectType.Number, obj.Type);
			Assert.Equal (RemoteObjectSubType.None, obj.SubType);
			Assert.Null (obj.ClassName);
			Assert.Equal (value.ToString (), obj.Description);
			Assert.Null (obj.ObjectId);
			Assert.Equal (value, obj.Value);
		}

		void AssertExceptionData (PausedExceptionData data, TestParameters parameters)
		{
			Assert.Equal (RemoteObjectType.Object, data.Type);
			Assert.Equal (RemoteObjectSubType.Error, data.SubType);
			Assert.Equal (parameters.ClassName, data.ClassName);
			// The message needs to end with a newline or it won't be displayed.
			Assert.StartsWith (parameters.ExpectedMessageWithStack, data.Description);
			Assert.True (data.Uncaught);
		}

		Task<T> SendBrowserOrProxy<T> (bool usePuppeteer, ProtocolRequest<T> request, string message = null)
			where T : ProtocolResponse
		{
			if (usePuppeteer)
				return SendPageCommand (request, message);
			else
				return SendCommand (request, message);
		}
	}
}
