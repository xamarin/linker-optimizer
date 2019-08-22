# New .NET Core based setup

## Initial Setup

Follow the steps layed out in `New-Blazor-Bootstrap.md` to initially checkout and build things.

## Build

In the top-level directory (I'm using `/Workspace/linker-optimizer`), do a `dotnet build` to build the module.

## Build the Sample

Go to `Tests/Blazor/EmptyBlazor`, then you can simply build with

```
$ dotnet build
Microsoft (R) Build Engine version 16.3.0-preview-19325-02+eca7818b1 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 112.75 ms for /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj.
  You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview
  EmptyBlazor -> /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/EmptyBlazor.dll
  Reading XML description from /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/optimizer.xml.
  Reading XML description from /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/../../Corlib/corlib-api.xml.
  Reading XML description from /Workspace/linker-optimizer/Tests/corlib-nunit.xml.
  Processing embedded resource linker descriptor: mscorlib.xml
  Duplicate preserve in resource mscorlib.xml in mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e of System.Threading.WasmRuntime (All).  Duplicate uses (All)
  Initializing Mono Linker Optimizer.
  Preprocessor mode: Full.
  Type Mono.ValueTuple has no fields to preserve
  BB SCAN FAILED: System.Boolean System.Text.Json.JsonPropertyInfoCommon`4/<CreateGenericTRuntimePropertyIEnumerable>d__26::MoveNext()
  BB SCAN FAILED: System.Boolean System.Text.Json.JsonPropertyInfoCommon`4/<CreateGenericTDeclaredPropertyIEnumerable>d__27::MoveNext()
  BB SCAN FAILED: System.Boolean System.Text.Json.JsonPropertyInfoCommon`4/<CreateGenericIEnumerableFromDictionary>d__28::MoveNext()
  BB SCAN FAILED: System.Void System.Text.Json.JsonSerializer::ReadCore(System.Text.Json.JsonSerializerOptions,System.Text.Json.Utf8JsonReader&,System.Text.Json.ReadStack&)
  BB SCAN FAILED: System.Boolean System.Text.Json.JsonSerializer::Write(System.Text.Json.Utf8JsonWriter,System.Int32,System.Text.Json.JsonSerializerOptions,System.Text.Json.WriteStack&)
  BB SCAN FAILED: System.Boolean Microsoft.Extensions.Internal.ParameterDefaultValue::TryGetDefaultValue(System.Reflection.ParameterInfo,System.Object&)
  BB SCAN FAILED: System.Object Microsoft.Extensions.DependencyInjection.ActivatorUtilities/ConstructorMatcher::CreateInstance(System.IServiceProvider)
  BB SCAN FAILED: System.Boolean Microsoft.Extensions.Internal.ParameterDefaultValue::TryGetDefaultValue(System.Reflection.ParameterInfo,System.Object&)
  BB SCAN FAILED: System.Object Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver::VisitConstructor(Microsoft.Extensions.DependencyInjection.ServiceLookup.ConstructorCallSite,Microsoft.Extensions.DependencyInjection.ServiceLookup.RuntimeResolverContext)
  BB SCAN FAILED: System.Boolean Microsoft.AspNetCore.Components.ComponentResolver/<EnumerateAssemblies>d__1::MoveNext()
  BB SCAN FAILED: System.Boolean Microsoft.AspNetCore.Components.Reflection.MemberAssignment/<GetPropertiesIncludingInherited>d__0::MoveNext()
  BB SCAN FAILED: System.Boolean Microsoft.AspNetCore.Components.Forms.EditContext/<GetValidationMessages>d__20::MoveNext()
  BB SCAN FAILED: System.Boolean Microsoft.AspNetCore.Components.Forms.EditContext/<GetValidationMessages>d__21::MoveNext()
  BB SCAN FAILED: System.Boolean Microsoft.AspNetCore.Components.Forms.FieldState/<GetValidationMessages>d__7::MoveNext()
  BB SCAN FAILED: System.Threading.Tasks.Task System.Net.Http.HttpContent::LoadIntoBufferAsync(System.Int64,System.Threading.CancellationToken)
  BB SCAN FAILED: System.Boolean System.Security.Claims.ClaimsIdentity/<get_Claims>d__51::MoveNext()
  BB SCAN FAILED: System.Boolean System.Security.Claims.ClaimsPrincipal/<get_Claims>d__36::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<OfTypeIterator>d__32`1::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<CastIterator>d__34`1::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<ExceptIterator>d__57`1::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<GroupJoinIterator>d__66`4::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<IntersectIterator>d__77`1::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<JoinIterator>d__81`4::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<SelectIterator>d__154`2::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<SelectManyIterator>d__163`2::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<SelectManyIterator>d__165`3::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<SelectManyIterator>d__167`3::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<SkipWhileIterator>d__177`1::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<SkipWhileIterator>d__179`1::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<SkipLastIterator>d__181`1::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<TakeWhileIterator>d__204`1::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<TakeWhileIterator>d__206`1::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<WhereIterator>d__228`1::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Enumerable/<ZipIterator>d__236`3::MoveNext()
  BB SCAN FAILED: System.Boolean System.Linq.Expressions.Compiler.CompilerScope/<GetVariablesIncludingMerged>d__37::MoveNext()
  BB SCAN FAILED: System.Boolean System.Net.Http.Headers.HttpHeaders/<GetHeaderStrings>d__23::MoveNext()
  BB SCAN FAILED: System.Boolean System.Net.Http.Headers.HttpHeaders/<GetEnumeratorCore>d__28::MoveNext()
  BB SCAN FAILED: System.Boolean System.Net.Http.Headers.HttpHeaderValueCollection`1/<GetEnumerator>d__21::MoveNext()
  Output action:     Link assembly: EmptyBlazor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
  Output action:     Save assembly: System.Threading.Tasks.Extensions, Version=4.2.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
  Output action:     Save assembly: System.Text.Json, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
  Output action:     Save assembly: System.Runtime.CompilerServices.Unsafe, Version=4.0.5.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  Output action:     Save assembly: System.Numerics.Vectors, Version=4.1.4.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  Output action:     Save assembly: System.Memory, Version=4.0.1.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
  Output action:     Save assembly: System.ComponentModel.Annotations, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  Output action:     Save assembly: System.Buffers, Version=4.0.3.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
  Output action:     Save assembly: Mono.WebAssembly.Interop, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:     Save assembly: Microsoft.JSInterop, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:     Save assembly: Microsoft.Extensions.Primitives, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:     Save assembly: Microsoft.Extensions.Options, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:     Save assembly: Microsoft.Extensions.Logging.Abstractions, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:     Save assembly: Microsoft.Extensions.DependencyInjection.Abstractions, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:     Save assembly: Microsoft.Extensions.DependencyInjection, Version=3.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:     Save assembly: Microsoft.Bcl.AsyncInterfaces, Version=1.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
  Output action:     Save assembly: Microsoft.AspNetCore.Metadata, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:     Save assembly: Microsoft.AspNetCore.Components.Browser, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:     Save assembly: Microsoft.AspNetCore.Components, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:     Save assembly: Microsoft.AspNetCore.Blazor, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:     Save assembly: Microsoft.AspNetCore.Authorization, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60
  Output action:   Delete assembly: netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
  Output action:     Link assembly: mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
  Output action:     Link assembly: System, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
  Output action:     Link assembly: Mono.Security, Version=2.0.5.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756
  Output action:   Delete assembly: System.Xml, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
  Output action:   Delete assembly: System.Numerics, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
  Output action:     Link assembly: System.Core, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
  Output action:   Delete assembly: System.Data, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
  Output action:   Delete assembly: System.Drawing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
  Output action:   Delete assembly: System.IO.Compression, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
  Output action:   Delete assembly: System.IO.Compression.FileSystem, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
  Output action:   Delete assembly: System.ComponentModel.Composition, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
  Output action:     Link assembly: System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  Output action:   Delete assembly: System.Runtime.Serialization, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e
  Output action:   Delete assembly: System.Transactions, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
  Output action:   Delete assembly: System.Web.Services, Version=2.0.5.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  Output action:   Delete assembly: System.Xml.Linq, Version=2.0.5.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
  Output action:   Delete assembly: System.ServiceModel.Internals, Version=0.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
  Output action:   Delete assembly: System.Runtime, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
  Mono Linker Optimizer finished in 00:00:09.9683790.
  
  Writing boot data to: /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/obj/Debug/netstandard2.0/blazor/blazor.boot.json
  Blazor Build result -> 32 files in /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/dist

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:11.35
```

The output will be something like this:

```
$ ls -lR bin/Debug/netstandard2.0/dist/_framework
total 1504
drwxr-xr-x  29 martin  wheel     928 Jul 11 01:31 _bin
-rw-r--r--   1 martin  wheel     971 Jul 11 01:31 blazor.boot.json
-rwxr--r--   1 martin  staff  640084 Jul  1 21:51 blazor.server.js
-rwxr--r--   1 martin  staff  122800 Jul  9 19:22 blazor.webassembly.js
drwxr-xr-x   4 martin  wheel     128 Jul 11 01:31 wasm

bin/Debug/netstandard2.0/dist/_framework/_bin:
total 6944
-rw-r--r--  1 martin  wheel    15360 Jul 11 01:31 EmptyBlazor.dll
-rw-r--r--  1 martin  wheel     2172 Jul 11 01:31 EmptyBlazor.pdb
-rw-r--r--  1 martin  wheel    33792 Jul 11 01:31 Microsoft.AspNetCore.Authorization.dll
-rw-r--r--  1 martin  wheel    33792 Jul 11 01:31 Microsoft.AspNetCore.Blazor.dll
-rw-r--r--  1 martin  wheel    12288 Jul 11 01:31 Microsoft.AspNetCore.Components.Browser.dll
-rw-r--r--  1 martin  wheel   197632 Jul 11 01:31 Microsoft.AspNetCore.Components.dll
-rw-r--r--  1 martin  wheel     5120 Jul 11 01:31 Microsoft.AspNetCore.Metadata.dll
-rw-r--r--  1 martin  wheel    11776 Jul 11 01:31 Microsoft.Bcl.AsyncInterfaces.dll
-rw-r--r--  1 martin  wheel    28160 Jul 11 01:31 Microsoft.Extensions.DependencyInjection.Abstractions.dll
-rw-r--r--  1 martin  wheel    53760 Jul 11 01:31 Microsoft.Extensions.DependencyInjection.dll
-rw-r--r--  1 martin  wheel    39424 Jul 11 01:31 Microsoft.Extensions.Logging.Abstractions.dll
-rw-r--r--  1 martin  wheel    40960 Jul 11 01:31 Microsoft.Extensions.Options.dll
-rw-r--r--  1 martin  wheel    28672 Jul 11 01:31 Microsoft.Extensions.Primitives.dll
-rw-r--r--  1 martin  wheel    22528 Jul 11 01:31 Microsoft.JSInterop.dll
-rw-r--r--  1 martin  wheel     8192 Jul 11 01:31 Mono.Security.dll
-rw-r--r--  1 martin  wheel     6144 Jul 11 01:31 Mono.WebAssembly.Interop.dll
-rw-r--r--  1 martin  wheel    11776 Jul 11 01:31 System.Buffers.dll
-rw-r--r--  1 martin  wheel    70144 Jul 11 01:31 System.ComponentModel.Annotations.dll
-rw-r--r--  1 martin  wheel   381440 Jul 11 01:31 System.Core.dll
-rw-r--r--  1 martin  wheel   132096 Jul 11 01:31 System.Memory.dll
-rw-r--r--  1 martin  wheel   100864 Jul 11 01:31 System.Net.Http.dll
-rw-r--r--  1 martin  wheel   146944 Jul 11 01:31 System.Numerics.Vectors.dll
-rw-r--r--  1 martin  wheel     7680 Jul 11 01:31 System.Runtime.CompilerServices.Unsafe.dll
-rw-r--r--  1 martin  wheel   273408 Jul 11 01:31 System.Text.Json.dll
-rw-r--r--  1 martin  wheel    16384 Jul 11 01:31 System.Threading.Tasks.Extensions.dll
-rw-r--r--  1 martin  wheel   276992 Jul 11 01:31 System.dll
-rw-r--r--  1 martin  wheel  1552896 Jul 11 01:31 mscorlib.dll

bin/Debug/netstandard2.0/dist/_framework/wasm:
total 18272
-rwxr--r--  1 martin  staff   490603 Jul  9 20:34 mono.js
-rwxr--r--  1 martin  staff  8862636 Jun 27 17:34 mono.wasm
```

Then run with

```
$ dotnet run
Hosting environment: Production
Content root path: /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor
Now listening on: http://localhost:5000
Now listening on: https://localhost:5001
Application started. Press Ctrl+C to shut down.
```

### Configuring the Project

If you look at the `EmptyBlazor.csproj`, you'll see the following:

```
  <ItemGroup>
    <LinkOptimizerXmlDescriptors Include="optimizer.xml" />
    <LinkOptimizerXmlDescriptors Include="$(MSBuildThisFileDirectory)\..\..\corlib-nunit.xml" />
  </ItemGroup>

  <PropertyGroup>
    <LinkerOptimizerOptions>report-profile=wasm,report-mode=actions+size+detailed</LinkerOptimizerOptions>
    <LinkerOptimizerReport>martin-report.xml</LinkerOptimizerReport>
    <LinkerOptimizerExtraLinkerArguments>--verbose</LinkerOptimizerExtraLinkerArguments>
    <LinkerOptimizerEnabled>true</LinkerOptimizerEnabled>
  </PropertyGroup>
```

You can disable the optimizer by changing the `LinkerOptimizerEnabled` property in there.
