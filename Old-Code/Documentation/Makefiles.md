# Makefile and build setup

The Makefiles are setup in a way to allow this module to be used either stand-alone or as part of a Mono checkout.

In either case, this module requires a fully built Mono tree.  So to use it stand-alone, you need to checkout Mono somewhere and do a full build.

## Standalone

To use this module in stand-alone mode, you need to set the `MONO_ROOT` environment variable to the root of your checked out and built Mono workspace (make sure to point it to your checkout, not to where you installed that Mono).

Standalone mode allows this module to easily be used with different versions corlib.

When building a stand-alone version of this library, you also need to update `external/linker` submodule.  There are no linker or cecil changes required and this module actually lives in it's own project.

In standalone mode, the module is called `Mono.Linker.Optimizer.exe` and is built using `msbuild` on the `Mono.Linker.Optimizer.sln`.

## Integrated

To use this module in integrated mode, use `git subtree` to put it into the `mcs/tools/linker/Martin` directory (you could change `Martin` to something else, but it needs to be a subdirectory of `mcs/tools/linker`/).

The `MONO_ROOT` will be detected automatically to point to the current checkout, so you don't need to set that environment variable.  We also do not use the `external/linker` submodule in this configuration, but directly reference the previously built `monolinker.exe`.

In integrated mode, the module is called `monolinker-optimizer.exe` and is built via the `monolinker-optimizer.exe.sources` via the usual Mono build logic.
