# Quick Start

## Prerequisites

The Linker Optimizer does not use Mono anymore, but .NET Core.

All you need it .NET Core 3.  I am using

```
$ dotnet --info
.NET Core SDK (reflecting any global.json):
 Version:   3.0.100
 Commit:    04339c3a26

Runtime Environment:
 OS Name:     Mac OS X
 OS Version:  10.14
 OS Platform: Darwin
 RID:         osx.10.14-x64
 Base Path:   /usr/local/share/dotnet/sdk/3.0.100/
```

## Building the Optimizer

```
dotnet build Mono.Linker.Optimizer
```

## Building the Blazor Samples

```
dotnet build Tests/Blazor/<sample>
```

For instance

```
dotnet build Tests/Blazor/StandaloneApp
```

This will automatically generate XML output in `./Tests/Blazor/<sample>/optimizer-report.xml `.

## Updating the AspNet and Blazor dependencies

Open the top-level `Directory.Build.props` and you'll find this section at the bottom:

```
  <PropertyGroup Condition="'$(UseLocalBuild)' == 'false'">
    <LocalDotNet>dotnet</LocalDotNet>
    <AspNetVersion>3.0.0-preview9.19465.2</AspNetVersion>
    <AspNetCoreVersion>3.0.0-rc1.19457.4</AspNetCoreVersion>
    <EntityFrameworkVersion>3.0.0-rc1.19456.14</EntityFrameworkVersion>
    <BlazorVersion>3.0.0-preview9.19465.2</BlazorVersion>
  </PropertyGroup>
```
