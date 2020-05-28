# Bootstrapping the Blazor Sample

## Prerequisites

```
$ dotnet --version
3.0.100-preview6-012266
$ dotnet new -i Microsoft.AspNetCore.Blazor.Templates::3.0.0-preview6.19307.2
```

Create it with:

    $ dotnet new blazor -n HelloBlazor

## Build Mono WebAssembly

Make sure to create a `Make.config` because it would otherwise build the entire world.

```
$ cat ../Make.config
DISABLE_ANDROID = 1
DISABLE_IOS = 1
DISABLE_MAC = 1
DISABLE_WASM_CROSS = 1
DISABLE_LLVM = 1
DISABLE_DESKTOP = 1

# DISABLE_BCL = 1
```

After everything is built, do a `make` in `sdks/wasm`.  This seems to crash with dotnet preview7 (lastest master dogfood), but works fine with preview6.

### Building

#### Blazor

Checkout the Blazor module:

```
$ git remote -v
$ git remote -v
baulig	git@github.com:baulig/Blazor.git (fetch)
baulig	git@github.com:baulig/Blazor.git (push)
origin	https://github.com/aspnet/Blazor.git (fetch)
origin	https://github.com/aspnet/Blazor.git (push)
```

In this module, you will find a file called [`src/Microsoft.AspNetCore.Blazor.Mono/HowToUpgradeMono.md`](
https://github.com/aspnet/Blazor/blob/master/src/Microsoft.AspNetCore.Blazor.Mono/HowToUpgradeMono.md).

Updating:

```
$ cd src/Microsoft.AspNetCore.Blazor.Mono/
$ cp /Workspace/mono-linker/sdks/wasm/debug/mono.* ./incoming/wasm/
$ rm -f incoming/bcl/*.dll incoming/bcl/Facades/*.dll
$ cp -a /Workspace/mono-linker/sdks/out/wasm-bcl/wasm/*.dll ./incoming/bcl/
$ cp -a /Workspace/mono-linker/sdks/out/wasm-bcl/wasm/Facades/*.dll ./incoming/bcl/Facades/
$ rm ./incoming/bcl/nunitlite.dll
```

Then build:

```
$ ./build.sh 
Downloading 'https://dot.net/v1/dotnet-install.sh'
dotnet-install: Downloading link: https://dotnetcli.azureedge.net/dotnet/Sdk/3.0.100-preview6-012264/dotnet-sdk-3.0.100-preview6-012264-osx-x64.tar.gz
dotnet-install: Extracting zip from https://dotnetcli.azureedge.net/dotnet/Sdk/3.0.100-preview6-012264/dotnet-sdk-3.0.100-preview6-012264-osx-x64.tar.gz
dotnet-install: Adding to current process PATH: `/Workspace/Blazor/.dotnet`. Note: This change will be visible only when sourcing script.
dotnet-install: Installation finished successfully.
  Restore completed in 261.6 ms for /Users/mabaul/.nuget/packages/microsoft.dotnet.arcade.sdk/1.0.0-beta.19323.4/tools/Tools.proj.
dotnet-install: Downloading link: https://dotnetcli.azureedge.net/dotnet/Runtime/2.1.11/dotnet-runtime-2.1.11-osx-x64.tar.gz
dotnet-install: Extracting zip from https://dotnetcli.azureedge.net/dotnet/Runtime/2.1.11/dotnet-runtime-2.1.11-osx-x64.tar.gz
dotnet-install: Adding to current process PATH: `/Workspace/Blazor/.dotnet`. Note: This change will be visible only when sourcing script.
dotnet-install: Installation finished successfully.
  Restore completed in 5.35 sec for /Workspace/Blazor/src/Microsoft.AspNetCore.Blazor.Mono/Microsoft.AspNetCore.Blazor.Mono.csproj.
  Restore completed in 5.73 sec for /Workspace/Blazor/src/Microsoft.AspNetCore.Blazor.BuildTools/Microsoft.AspNetCore.Blazor.BuildTools.csproj.
  Microsoft.AspNetCore.Blazor.Mono -> /Workspace/Blazor/artifacts/bin/Microsoft.AspNetCore.Blazor.Mono/Debug/netstandard1.0/Microsoft.AspNetCore.Blazor.Mono.dll
  Creating optimized Mono WebAssembly build
  Microsoft.AspNetCore.Blazor.BuildTools -> /Workspace/Blazor/artifacts/bin/Microsoft.AspNetCore.Blazor.BuildTools/Debug/netcoreapp2.1/Microsoft.AspNetCore.Blazor.BuildTools.dll
  Creating optimized BCL build
  System.Net.Http.dll                      0.163 MB ==> 0.161 MB
  mscorlib.dll                             3.737 MB ==> 3.737 MB
  Successfully created package '/Workspace/Blazor/artifacts/packages/Debug/Shipping/Microsoft.AspNetCore.Blazor.Mono.0.10.0-dev.nupkg'.

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:12.88
```

This will create

`/Workspace/Blazor/artifacts/packages/Debug/Shipping/Microsoft.AspNetCore.Blazor.Mono.0.10.0-dev.nupkg`

#### AspNetCore

Now checkout AspNetCore:

```
$ git remote -v
baulig	git@github.com:baulig/AspNetCore.git (fetch)
baulig	git@github.com:baulig/AspNetCore.git (push)
origin	https://github.com/aspnet/AspNetCore.git (fetch)
origin	https://github.com/aspnet/AspNetCore.git (push)
```

Edit `eng/Versions.prop`:

```
diff --git a/eng/Versions.props b/eng/Versions.props
index e3fa0ec0f1..e35b2e5332 100644
--- a/eng/Versions.props
+++ b/eng/Versions.props
@@ -82,7 +82,7 @@
     <!-- Only listed explicitly to workaround https://github.com/dotnet/cli/issues/10528 -->
     <MicrosoftNETCorePlatformsPackageVersion>3.0.0-preview7.19312.3</MicrosoftNETCorePlatformsPackageVersion>
     <!-- Packages from aspnet/Blazor -->
-    <MicrosoftAspNetCoreBlazorMonoPackageVersion>0.10.0-preview7.19317.1</MicrosoftAspNetCoreBlazorMonoPackageVersion>
+    <MicrosoftAspNetCoreBlazorMonoPackageVersion>0.10.0-dev</MicrosoftAspNetCoreBlazorMonoPackageVersion>
     <!-- Packages from aspnet/Extensions -->
     <InternalAspNetCoreAnalyzersPackageVersion>3.0.0-preview7.19312.4</InternalAspNetCoreAnalyzersPackageVersion>
     <MicrosoftAspNetCoreAnalyzerTestingPackageVersion>3.0.0-preview7.19312.4</MicrosoftAspNetCoreAnalyzerTestingPackageVersion>
@@ -260,6 +260,7 @@
     <RestoreSources>
       $(RestoreSources);
       https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
+      /Workspace/Blazor/artifacts/packages/Debug/Shipping;
     </RestoreSources>
     <!-- In an orchestrated build, this may be overriden to other Azure feeds. -->
     <DotNetAssetRootUrl Condition="'$(DotNetAssetRootUrl)'==''">https://dotnetcli.blob.core.windows.net/dotnet/</DotNetAssetRootUrl>
```

Do a `./build.sh` and `./build.sh --pack` and get a coffee while it's running, the build will take approximately 9 minutes.

The `./build.sh --pack` might fail with

```
/Workspace/AspNetCore/.dotnet/sdk/3.0.100-preview5-011568/Sdks/NuGet.Build.Tasks.Pack/build/NuGet.Build.Tasks.Pack.targets(199,5): error : Could not find a part of the path '/Workspace/AspNetCore/src/Components/Browser.JS/dist/Debug'. [/Workspace/AspNetCore/src/Components/Blazor/Build/src/Microsoft.AspNetCore.Blazor.Build.csproj]
    0 Warning(s)
    1 Error(s)

```

Go to `src/Components` and run `./build.sh` there (that should take about three minutes).  Then go to the root directory and run the `./build.sh --pack` again (should take about six minutes).

#### Setting up and building the sample

Edit `EmptyBlazor.csproj` to and adjust the package sources and version:

```
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <RestoreAdditionalProjectSources>
      /Workspace/AspNetCore/artifacts/packages/Debug/Shipping;
      https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
      https://dotnet.myget.org/F/blazor-dev/api/v3/index.json;
    </RestoreAdditionalProjectSources>
    <LangVersion>7.3</LangVersion>
    <RazorLangVersion>3.0</RazorLangVersion>
    <BlazorLinkOnBuild>false</BlazorLinkOnBuild>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Blazor" Version="3.0.0-dev" />
    <PackageReference Include="Microsoft.AspNetCore.Blazor.Build" Version="3.0.0-dev" PrivateAssets="all" />
    <PackageReference Include="Microsoft.AspNetCore.Blazor.DevServer" Version="3.0.0-dev" PrivateAssets="all" />
  </ItemGroup>

</Project>
```

#### Error

Now the `dotnet restore` works, but the `dotnet build` fails:

```
$ dotnet build
Microsoft (R) Build Engine version 16.2.0-preview-19278-01+d635043bd for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 25.39 ms for /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj.
/usr/local/share/dotnet/sdk/3.0.100-preview6-012266/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(158,5): message NETSDK1057: You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview [/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj]
  EmptyBlazor -> /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/EmptyBlazor.dll
  It was not possible to find any compatible framework version
  The specified framework 'Microsoft.NETCore.App', version '3.0.0-preview7-27812-08' was not found.
    - The following frameworks were found:
        2.0.3 at [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
        2.0.9 at [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
        2.2.4 at [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
        2.2.5 at [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
        3.0.0-preview4-27511-06 at [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
        3.0.0-preview6-27813-07 at [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  
  You can resolve the problem by installing the specified framework and/or SDK.
  
  The .NET Core frameworks can be found at:
    - https://aka.ms/dotnet-download
/Users/mabaul/.nuget/packages/microsoft.aspnetcore.blazor.build/3.0.0-dev/targets/Blazor.MonoRuntime.targets(531,5): error MSB3073: The command "dotnet "/Users/mabaul/.nuget/packages/microsoft.aspnetcore.blazor.build/3.0.0-dev/targets/../tools/Microsoft.AspNetCore.Blazor.Build.dll" resolve-dependencies "/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/obj/Debug/netstandard2.0/EmptyBlazor.dll" --references "/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/obj/Debug/netstandard2.0/blazor/resolve-dependencies.txt" --base-class-library "/Users/mabaul/.nuget/packages/microsoft.aspnetcore.blazor.mono/0.10.0-dev/build/netstandard1.0/../../tools/mono/bcl/" --base-class-library "/Users/mabaul/.nuget/packages/microsoft.aspnetcore.blazor.mono/0.10.0-dev/build/netstandard1.0/../../tools/mono/bcl/Facades/" --output "/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/obj/Debug/netstandard2.0/blazor/resolved.assemblies.txt"" exited with code 150. [/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj]

Build FAILED.

/Users/mabaul/.nuget/packages/microsoft.aspnetcore.blazor.build/3.0.0-dev/targets/Blazor.MonoRuntime.targets(531,5): error MSB3073: The command "dotnet "/Users/mabaul/.nuget/packages/microsoft.aspnetcore.blazor.build/3.0.0-dev/targets/../tools/Microsoft.AspNetCore.Blazor.Build.dll" resolve-dependencies "/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/obj/Debug/netstandard2.0/EmptyBlazor.dll" --references "/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/obj/Debug/netstandard2.0/blazor/resolve-dependencies.txt" --base-class-library "/Users/mabaul/.nuget/packages/microsoft.aspnetcore.blazor.mono/0.10.0-dev/build/netstandard1.0/../../tools/mono/bcl/" --base-class-library "/Users/mabaul/.nuget/packages/microsoft.aspnetcore.blazor.mono/0.10.0-dev/build/netstandard1.0/../../tools/mono/bcl/Facades/" --output "/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/obj/Debug/netstandard2.0/blazor/resolved.assemblies.txt"" exited with code 150. [/Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj]
    0 Warning(s)
    1 Error(s)

Time Elapsed 00:00:38.31
```

#### Starting over with the latest dogfood build

Now using

```
$ dotnet --info
.NET Core SDK (reflecting any global.json):
 Version:   3.0.100-preview7-012629
 Commit:    1b14e8dfdf

Runtime Environment:
 OS Name:     Mac OS X
 OS Version:  10.14
 OS Platform: Darwin
 RID:         osx.10.14-x64
 Base Path:   /usr/local/share/dotnet/sdk/3.0.100-preview7-012629/

Host (useful for support):
  Version: 3.0.0-preview7-27826-04
  Commit:  5c4d829254

.NET Core SDKs installed:
  2.0.3 [/usr/local/share/dotnet/sdk]
  2.1.202 [/usr/local/share/dotnet/sdk]
  2.2.203 [/usr/local/share/dotnet/sdk]
  2.2.300 [/usr/local/share/dotnet/sdk]
  2.2.301-preview-010200 [/usr/local/share/dotnet/sdk]
  3.0.100-preview4-010713 [/usr/local/share/dotnet/sdk]
  3.0.100-preview6-012266 [/usr/local/share/dotnet/sdk]
  3.0.100-preview7-012629 [/usr/local/share/dotnet/sdk]

.NET Core runtimes installed:
  Microsoft.AspNetCore.All 2.2.1 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.All 2.2.4 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.All 2.2.5 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.App 2.2.4 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 2.2.5 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 3.0.0-preview6.19307.2 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 3.0.0-preview7.19325.7 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 2.0.3 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.0.9 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.2.4 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.2.5 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 3.0.0-preview4-27511-06 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 3.0.0-preview6-27813-07 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 3.0.0-preview7-27826-04 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]

```

Rebuilt everything and now I'm getting this error:

```
$ dotnet restore

Welcome to .NET Core 3.0!
---------------------
SDK Version: 3.0.100-preview7-012629

Telemetry
---------
The .NET Core tools collect usage data in order to help us improve your experience. The data is anonymous. It is collected by Microsoft and shared with the community. You can opt-out of telemetry by setting the DOTNET_CLI_TELEMETRY_OPTOUT environment variable to '1' or 'true' using your favorite shell.

Read more about .NET Core CLI Tools telemetry: https://aka.ms/dotnet-cli-telemetry

----------------
Explore documentation: https://aka.ms/dotnet-docs
Report issues and find source on GitHub: https://github.com/dotnet/core
Find out what's new: https://aka.ms/dotnet-whats-new
Learn about the installed HTTPS developer cert: https://aka.ms/aspnet-core-https
Use 'dotnet --help' to see available commands or visit: https://aka.ms/dotnet-cli-docs
Write your first app: https://aka.ms/first-net-core-app
--------------------------------------------------------------------------------------
  Restore completed in 2.03 sec for /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj.
draenor:EmptyBlazor mabaul$ 
draenor:EmptyBlazor mabaul$ dotnet build
Microsoft (R) Build Engine version 16.3.0-preview-19321-02+a5a222491 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 20.49 ms for /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/EmptyBlazor.csproj.
  You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview
  EmptyBlazor -> /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/EmptyBlazor.dll
  Writing boot data to: /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/obj/Debug/netstandard2.0/blazor/blazor.boot.json
  Blazor Build result -> 45 files in /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/bin/Debug/netstandard2.0/dist

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:05.56
draenor:EmptyBlazor mabaul$ 
draenor:EmptyBlazor mabaul$ 
draenor:EmptyBlazor mabaul$ dotnet run
Application startup exception: System.InvalidOperationException: Application assembly not found at /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/.
   at Microsoft.AspNetCore.Blazor.DevServer.Server.Startup.ResolveApplicationAssemblyFullPath(IWebHostEnvironment environment)
   at Microsoft.AspNetCore.Blazor.DevServer.Server.Startup.Configure(IApplicationBuilder app, IWebHostEnvironment environment, IConfiguration configuration)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Object[] arguments, Signature sig, Boolean constructor, Boolean wrapExceptions)
   at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
   at Microsoft.AspNetCore.Hosting.Internal.MethodInfoExtensions.InvokeWithoutWrappingExceptions(MethodInfo methodInfo, Object obj, Object[] parameters)
   at Microsoft.AspNetCore.Hosting.Internal.ConfigureBuilder.Invoke(Object instance, IApplicationBuilder builder)
   at Microsoft.AspNetCore.Hosting.Internal.ConfigureBuilder.<>c__DisplayClass4_0.<Build>b__0(IApplicationBuilder builder)
   at Microsoft.AspNetCore.Hosting.Internal.ConventionBasedStartup.Configure(IApplicationBuilder app)
   at Microsoft.AspNetCore.HostFilteringStartupFilter.<>c__DisplayClass0_0.<Configure>b__0(IApplicationBuilder app)
   at Microsoft.AspNetCore.Hosting.Internal.WebHost.BuildApplication()
crit: Microsoft.AspNetCore.Hosting.Internal.WebHost[6]
      Application startup exception
System.InvalidOperationException: Application assembly not found at /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/.
   at Microsoft.AspNetCore.Blazor.DevServer.Server.Startup.ResolveApplicationAssemblyFullPath(IWebHostEnvironment environment)
   at Microsoft.AspNetCore.Blazor.DevServer.Server.Startup.Configure(IApplicationBuilder app, IWebHostEnvironment environment, IConfiguration configuration)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Object[] arguments, Signature sig, Boolean constructor, Boolean wrapExceptions)
   at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
   at Microsoft.AspNetCore.Hosting.Internal.MethodInfoExtensions.InvokeWithoutWrappingExceptions(MethodInfo methodInfo, Object obj, Object[] parameters)
   at Microsoft.AspNetCore.Hosting.Internal.ConfigureBuilder.Invoke(Object instance, IApplicationBuilder builder)
   at Microsoft.AspNetCore.Hosting.Internal.ConfigureBuilder.<>c__DisplayClass4_0.<Build>b__0(IApplicationBuilder builder)
   at Microsoft.AspNetCore.Hosting.Internal.ConventionBasedStartup.Configure(IApplicationBuilder app)
   at Microsoft.AspNetCore.HostFilteringStartupFilter.<>c__DisplayClass0_0.<Configure>b__0(IApplicationBuilder app)
   at Microsoft.AspNetCore.Hosting.Internal.WebHost.BuildApplication()
Unhandled exception. System.InvalidOperationException: Application assembly not found at /Workspace/linker-optimizer/Tests/Blazor/EmptyBlazor/.
   at Microsoft.AspNetCore.Blazor.DevServer.Server.Startup.ResolveApplicationAssemblyFullPath(IWebHostEnvironment environment)
   at Microsoft.AspNetCore.Blazor.DevServer.Server.Startup.Configure(IApplicationBuilder app, IWebHostEnvironment environment, IConfiguration configuration)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Object[] arguments, Signature sig, Boolean constructor, Boolean wrapExceptions)
   at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
   at Microsoft.AspNetCore.Hosting.Internal.MethodInfoExtensions.InvokeWithoutWrappingExceptions(MethodInfo methodInfo, Object obj, Object[] parameters)
   at Microsoft.AspNetCore.Hosting.Internal.ConfigureBuilder.Invoke(Object instance, IApplicationBuilder builder)
   at Microsoft.AspNetCore.Hosting.Internal.ConfigureBuilder.<>c__DisplayClass4_0.<Build>b__0(IApplicationBuilder builder)
   at Microsoft.AspNetCore.Hosting.Internal.ConventionBasedStartup.Configure(IApplicationBuilder app)
   at Microsoft.AspNetCore.HostFilteringStartupFilter.<>c__DisplayClass0_0.<Configure>b__0(IApplicationBuilder app)
   at Microsoft.AspNetCore.Hosting.Internal.WebHost.BuildApplication()
   at Microsoft.AspNetCore.Hosting.Internal.WebHost.StartAsync(CancellationToken cancellationToken)
   at Microsoft.AspNetCore.Hosting.WebHostExtensions.RunAsync(IWebHost host, CancellationToken token, String startupMessage)
   at Microsoft.AspNetCore.Hosting.WebHostExtensions.RunAsync(IWebHost host, CancellationToken token, String startupMessage)
   at Microsoft.AspNetCore.Hosting.WebHostExtensions.RunAsync(IWebHost host, CancellationToken token)
   at Microsoft.AspNetCore.Hosting.WebHostExtensions.Run(IWebHost host)
   at Microsoft.AspNetCore.Blazor.DevServer.Commands.ServeCommand.Execute()
   at Microsoft.Extensions.CommandLineUtils.CommandLineApplication.Execute(String[] args)
   at Microsoft.AspNetCore.Blazor.DevServer.Program.Main(String[] args)
```

#### Cleaning up this mess

After building the Blazor module, we need to build the Extensions module.

```
$ git remote -v
baulig	git@github.com:baulig/Extensions.git (fetch)
baulig	git@github.com:baulig/Extensions.git (push)
origin	https://github.com/aspnet/Extensions.git (fetch)
origin	https://github.com/aspnet/Extensions.git (push)
```

In the `Versions.props`, we also need to edit all the version numbers from the `aspnet/Extensions` module like for instance

    <MonoWebAssemblyInteropPackageVersion>3.0.0-preview7.19312.4</MonoWebAssemblyInteropPackageVersion>

I'm not using the `darc` command, but do it manually.

Starting all over again, now using

```
l$ dotnet --info
.NET Core SDK (reflecting any global.json):
 Version:   3.0.100-preview7-012635
 Commit:    cd5572d30b

Runtime Environment:
 OS Name:     Mac OS X
 OS Version:  10.14
 OS Platform: Darwin
 RID:         osx.10.14-x64
 Base Path:   /usr/local/share/dotnet/sdk/3.0.100-preview7-012635/

Host (useful for support):
  Version: 3.0.0-preview7-27826-04
  Commit:  5c4d829254

.NET Core SDKs installed:
  2.0.3 [/usr/local/share/dotnet/sdk]
  2.1.202 [/usr/local/share/dotnet/sdk]
  2.2.203 [/usr/local/share/dotnet/sdk]
  2.2.300 [/usr/local/share/dotnet/sdk]
  2.2.301-preview-010200 [/usr/local/share/dotnet/sdk]
  3.0.100-preview4-010713 [/usr/local/share/dotnet/sdk]
  3.0.100-preview6-012266 [/usr/local/share/dotnet/sdk]
  3.0.100-preview7-012629 [/usr/local/share/dotnet/sdk]
  3.0.100-preview7-012635 [/usr/local/share/dotnet/sdk]

.NET Core runtimes installed:
  Microsoft.AspNetCore.All 2.2.1 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.All 2.2.4 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.All 2.2.5 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.All]
  Microsoft.AspNetCore.App 2.2.4 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 2.2.5 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 3.0.0-preview6.19307.2 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 3.0.0-preview7.19325.7 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.AspNetCore.App 3.0.0-preview7.19325.8 [/usr/local/share/dotnet/shared/Microsoft.AspNetCore.App]
  Microsoft.NETCore.App 2.0.3 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.0.9 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.2.4 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 2.2.5 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 3.0.0-preview4-27511-06 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 3.0.0-preview6-27813-07 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  Microsoft.NETCore.App 3.0.0-preview7-27826-04 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]
  ```

  And I've completely wiped my NuGet cache.

  