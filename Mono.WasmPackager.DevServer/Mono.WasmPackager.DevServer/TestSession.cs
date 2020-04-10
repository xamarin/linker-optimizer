using PuppeteerSharp;

namespace Mono.WasmPackager.DevServer
{
	public class TestSession
	{
		public Target Target {
			get;
		}

		public CDPSession Session {
			get;
		}

		public TestSession (Target target, CDPSession session)
		{
			Target = target;
			Session = session;
		}
	}
}
