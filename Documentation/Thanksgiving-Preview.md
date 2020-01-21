# Thanksgiving Preview

I'm going to head out into the long weekend shortly, but wanted to give you a status as well as a short preview of the packager work.

At the moment, it can build the app bundle and when you run it, it doesn't crash, but doesn't do anything either.  I don't know why this is happening.

## Setup

First, you need to checkout Mono from this PR and do the normal SDK build: https://github.com/mono/mono/pull/17939.

Then build the nugets:

```
$ cd sdks/wasm
$ make build-sdk
$ cd sdk/Mono.WebAssembly.Runtime.Aot
$ pwd
/Users/Workspace/mono-linker/sdks/wasm/sdk/Mono.WebAssembly.Runtime.Aot
$ dotnet pack
```

The resulting packages will be placed in `sdks/wasm/sdk/packages`.  You need to manually copy them all into the top-level `artifacts/` directory in this module.  There will be a NuGet feed shortly, but at the moment it needs to be done manually.

Then you need to edit `Mono.WasmPackager/Sdk/Sdk.props` and change the `EmscriptenSdkDir` in it to point to your Mono SDK build.

## Build

After that, you should be able to open the `packager.code-workspace` in Visual Studio Code and build it.  I recommend getting the "Task Runner" extension because you can then just select the "build" task in the "TestSdk" project.

The task definition is in `Tests/Packager/TestSdk/.vscode/tasks.json`

To do it manually, you need to do `dotnet pack` in the following directories (in this order) and clean the `artifacts` directory to force a rebuild:

* Mono.Linker.Optimizer
* Mono.Linker.CilStrip
* Mono.WasmPackager

Ignore the "Mono.Linker.Emscripten" directory for now, it doesn't work yet.

## Run

Once you've built everything, you can checkout the `Tests/Packager/TestSdk` sample and just do `dotnet build` in there.

At the moment, `dotnet run` doesn't work yet, to run it you need to do

```
(cd app && ~/.jsvu/sm runtime.js --run TestSdk.exe)
```

this will currently print

```
$ (cd app && ~/.jsvu/sm runtime.js --run TestSdk.exe)
Arguments: --run,TestSdk.exe
MONO_WASM: Loaded: TestSdk.exe
MONO_WASM: Loaded: mscorlib.dll
MONO_WASM: Loaded: aot-dummy.dll
MONO_WASM: Initializing mono runtime
MONO-WASM: Runtime is ready.
WASM: Initializing.....
```

## Future Work

The Mono SDK packages as well as those three projects mentioned above will shortly be available from a custom NuGet feed, so you could easily write tests even outside the `linker-optimizer` module.

Emscripten is still giving me a minor headache as I don't know how to best bundle that yet, considering it's size and frequent updates.

Happy Thanksgiving!

Martin
