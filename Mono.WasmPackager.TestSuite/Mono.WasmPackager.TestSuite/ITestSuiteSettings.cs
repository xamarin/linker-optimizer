namespace Mono.WasmPackager.TestSuite
{
	public interface ITestSuiteSettings
	{
		string TestSuite_SampleProject { get; }
		string DevServer_RootDir { get; }
		string DevServer_Arguments { get; }
		string DevServer_Assembly { get; }
	}
}
