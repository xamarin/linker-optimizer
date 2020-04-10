April 8th Preview
=================

NuGet Feed:
https://dev.azure.com/dnceng/public/_packaging?_a=feed&feed=mono-wasm-experiment

Starting Chrome Canary:

`/Applications/Google\ Chrome\ Canary.app/Contents/MacOS/Google\ Chrome\ Canary --incognito --remote-debugging-port=9222`

Requires Mono https://github.com/baulig/mono/tree/martin-packager-0408, commit
https://github.com/baulig/mono/commit/7fbc347baa397a841e52a9096da16022bb984a11
or use the published NuGet packages.

The Mono packages were made immediately after the 3.2-wasm branch was created and
are based on that branch.

Latest working tests are in Tests/Packager/Tests/WorkingTests; they all pass both
in VSCode with both the "run test" as well as the "debug test" setups - and also on
the command line via "dotnet test".

Tests/Packager/Tests/SimpleTest contains a few more unstable tests.

Most of Tests/Packager/Samples is outdated, the SimpleWeb and SimpleWeb2 samples
should work.