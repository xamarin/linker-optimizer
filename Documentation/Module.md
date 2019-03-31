# General Module Setup

This version of the module works lives in it's on project file and does not require any changes to either the linker or cecil.

It can either be built using the `Mono.Linker.Optimizer.sln` in Visual Studio for Mac or (when integrated into a Mono checkout), the `monolinker-optimizer.exe.sources`.  Either way, the new code lives in it's own library and references the pristine linker.

To make this work, the new module has it's own `Program.Main ()` entry point where it does some argument parsing and then call's the pristine linker's `Driver.Execute ()`.

All arguments for this module must come first on the command-line, custom argument parsing stops as soon as we encounter an unknown option and the remaining arguments are then passed on to the linker.

To enable the module, you need to pass

* `--martin <filename>`: Specify the main module to be linked.
If `<filename>.xml` exists, then it will be read as custom XML description file as if it were explicitly specified via `--martin-xml <filename>.xml`.
The optimizer won't actually read `<filename>`, but it needs to know the name of the main module.
The provided filename will be passed down to the pristine linker via `-a <filename>`, so you don't have to explicitly provide that `-a` argument.

There are also some additional options that you can use:

* `--martin-xml <filename>`: Read `<filename>` as custom XML description.
The file will be expected to contain _only_ definitions relevant for this module and it will not be passed down to the pristine linker.
Also please not that XML files provided via `-x` or `-i` will not be regocnized as argument processing will stop once we encounter the first unknown argument.

* `--martin-options <options>`: Command-separated list of options.
These options will be processed _after_ all XML files have been read (thus overriding anything specified in those XML files) and _before_ the `MARTIN_LINKER_OPTIONS` environment variable.

If the `--martin` argument has not been given, then the pristine linker will be invoked, without registering the optimizer.

Otherwise, we will register ourselves by adding a `--custom-step` argument to run our initialization step immediately after the `TypeMapStep`.
