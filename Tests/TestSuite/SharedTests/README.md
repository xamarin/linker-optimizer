Test Overview
=============

Using the Test Explorer
-----------------------

In order to get Intellisense and Left-clicking on Tests in the Test Explorer to work, you need to select one of the test projects as Ominisharp's current project.

Use the `Omnisharp: Select Project` command in VS Code (`View` / `Command Palette`) then select one of `SimpleTest`, `WorkingTests` or `NetCoreTests` (this is still experimental).

SharedTests Setup
-----------------

This is set up the way it is to work around OmniSharp limitations, which does not support multiple workspaces yet.

When adding new tests, please make the class `abstract` and then use

```
protected async Task SharedInsertHitAndResume ()
{
    // your test goes here
}
```

In each of the actual test projects - `SimpleTest`, `WorkingTests` and `NetCoreTests`, we then add some shims:

```
using System.Threading.Tasks;
using Xunit;

namespace NetCoreTests
{
	public class TestBreakpoints : SharedTests.TestBreakpoints
	{
		[Fact]
		public Task InsertHitAndResume () => SharedInsertHitAndResume ();
	}
}
```

This ensures that in the test explorer will list them nicely in a tree-like format:

- SimpleTest
    - TestBreakpoints
        - SharedInsertHitAndResume
- WorkingTests
    - TestBreakpoints
        - SharedInsertHitAndResume
- NetCoreTests
    - TestBreakpoints
        - SharedInsertHitAndResume

If we instead put the `[Fact]` attribute onto a public base class method, it will look like this in the test explorer:

- SharedTests
    - TestBreakpoints
        - SharedInsertHitAndResume
        - SharedInsertHitAndResume
        - SharedInsertHitAndResume

and clicking on each of these will get you to the base class.

Making the base class abstract fixes that, but then we can't left-click on a test without getting this "Cannot find test method (no symbols)" error message.

This current setup certainly isn't perfect, but it's the best I could do to make the tests show up niceley in the test explorer and to make sure you could actually click on test methods.
