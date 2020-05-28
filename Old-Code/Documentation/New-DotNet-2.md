# New .NET Core based setup

## Initial Setup

## Build

The top-level `Makefile` now uses `dotnet build` to build the module.

We are using the linker from `origin/master`, but with a custom `linker.csproj` that has the problematic conditionals at the top removed and the framework changed from `netcoreapp2.0` to `netframework2.0`.

## Build the Sample

Go to `Tests/DotNet`, then you can build the `EmptyTest` sample like this:

```
$ make -w V=1 dotnet-EmptyTest
make: Entering directory `/Workspace/linker-optimizer/Tests/DotNet'
/Applications/Xcode.app/Contents/Developer/usr/bin/make -C /Workspace/linker-optimizer standalone-build
make[1]: Entering directory `/Workspace/linker-optimizer'
dotnet build /Workspace/linker-optimizer/Mono.Linker.Optimizer
Microsoft (R) Build Engine version 16.3.0-preview-19456-02+ee8294b55 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 24.42 ms for /Workspace/linker-optimizer/Mono.Linker.Optimizer/linker/linker.csproj.
  Restore completed in 24.41 ms for /Workspace/linker-optimizer/external/linker/external/cecil/symbols/mdb/Mono.Cecil.Mdb.csproj.
  Restore completed in 24.42 ms for /Workspace/linker-optimizer/Mono.Linker.Optimizer/Mono.Linker.Optimizer.csproj.
  Restore completed in 24.42 ms for /Workspace/linker-optimizer/external/linker/external/cecil/Mono.Cecil.csproj.
  Restore completed in 24.41 ms for /Workspace/linker-optimizer/external/linker/external/cecil/symbols/pdb/Mono.Cecil.Pdb.csproj.
  You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview
  You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview
  You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview
  You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview
  Mono.Cecil -> /Workspace/linker-optimizer/external/linker/external/cecil/bin/Debug/netstandard2.0/Mono.Cecil.dll
  Mono.Cecil.Pdb -> /Workspace/linker-optimizer/external/linker/external/cecil/symbols/pdb/bin/Debug/netstandard2.0/Mono.Cecil.Pdb.dll
  Mono.Cecil.Mdb -> /Workspace/linker-optimizer/external/linker/external/cecil/symbols/mdb/bin/Debug/netstandard2.0/Mono.Cecil.Mdb.dll
  linker -> /Workspace/linker-optimizer/Mono.Linker.Optimizer/linker/bin/optimizer/netstandard2.0/linker.dll
  Mono.Linker.Optimizer -> /Workspace/linker-optimizer/Mono.Linker.Optimizer/bin/optimizer/netcoreapp2.0/Mono.Linker.Optimizer.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.02
make[1]: Leaving directory `/Workspace/linker-optimizer'
Running test EmptyTest
(cd EmptyTest && dotnet build)
Microsoft (R) Build Engine version 16.3.0-preview-19456-02+ee8294b55 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 85.55 ms for /Workspace/linker-optimizer/Tests/DotNet/EmptyTest/EmptyTest.csproj.
  You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview
  EmptyTest -> /Workspace/linker-optimizer/Tests/DotNet/EmptyTest/bin/Debug/netcoreapp3.0/EmptyTest.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.87
(cd EmptyTest && dotnet publish -r osx.10.14-x64 --self-contained true)
Microsoft (R) Build Engine version 16.3.0-preview-19456-02+ee8294b55 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 56.52 ms for /Workspace/linker-optimizer/Tests/DotNet/EmptyTest/EmptyTest.csproj.
  You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview
  EmptyTest -> /Workspace/linker-optimizer/Tests/DotNet/EmptyTest/bin/Debug/netcoreapp3.0/osx.10.14-x64/EmptyTest.dll
  EmptyTest -> /Workspace/linker-optimizer/Tests/DotNet/EmptyTest/bin/Debug/netcoreapp3.0/osx.10.14-x64/publish/
dotnet run -p /Workspace/linker-optimizer/Mono.Linker.Optimizer -f netcoreapp2.0 -- --optimizer EmptyTest/bin/Debug/netcoreapp3.0/osx.10.14-x64/publish/EmptyTest.dll --optimizer-report output/martin-report.xml --optimizer-options report-profile=dotnet,report-mode=actions+size+detailed  -out output -b true -c link -l none --dump-dependencies
Initializing Mono Linker Optimizer.
Cannot find `mscorlib.dll` is assembly list.
Mono Linker Optimizer finished in 00:00:02.3528801.

make: Leaving directory `/Workspace/linker-optimizer/Tests/DotNet'
```

