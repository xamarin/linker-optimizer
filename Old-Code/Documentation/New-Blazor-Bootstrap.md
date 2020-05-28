# New Bootstrap

## Initial Setup

### Create Workspace

Using `/Workspace/Blazor` as root directory.

Checkout `aspnet/Blazor`:

```
$ pwd && git remote -v && git branch -vv
/Workspace/Blazor/Blazor
baulig	git@github.com:baulig/Blazor.git (fetch)
baulig	git@github.com:baulig/Blazor.git (push)
origin	https://github.com/aspnet/Blazor.git (fetch)
origin	https://github.com/aspnet/Blazor.git (push)
* martins-playground eae050d [baulig/martins-playground] X
  master             cce6ca2 [origin/master] Update branding to preview 8
```

Checkout `aspnet/Extensions`:

```
$ pwd && git remote -v && git branch -vv
/Workspace/Blazor/Extensions
baulig	git@github.com:baulig/Extensions.git (fetch)
baulig	git@github.com:baulig/Extensions.git (push)
origin	https://github.com/aspnet/Extensions.git (fetch)
origin	https://github.com/aspnet/Extensions.git (push)
* martins-playground 8c6d69570 [baulig/martins-playground] [master] Update dependencies from dotnet/core-setup (#1888)
  master             416befdba [origin/master] Update branding to preview 8
```

Checkout `aspnet/AspNetCore`:

```
$ !pw
pwd && git remote -v && git branch -vv
/Workspace/Blazor/AspNetCore
baulig	git@github.com:baulig/AspNetCore.git (fetch)
baulig	git@github.com:baulig/AspNetCore.git (push)
origin	https://github.com/aspnet/AspNetCore.git (fetch)
origin	https://github.com/aspnet/AspNetCore.git (push)
* martins-playground bd1f9254d8 [baulig/martins-playground] X
  master             7344c1ea8a [origin/master] Update branding to preview 8
```

Make sure you have the latest version, which contains Larry's initialization changes:
https://github.com/baulig/AspNetCore/commit/d0d19d078822de94888f602bdfc4c67f304cd210.

Install the latest .NET Dogfood SDK.

```
$ dotnet --info
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

### Build

To build this, first build the `Blazor` module, then `Extensions`, then `AspNetCore`.

```
(cd Blazor && ./build.sh)
(cd Extensions && ./build.sh)
(cd AspNetCore && ./build.sh)
(cd AspNetCore/src/Components && ./build.sh)
(cd AspNetCore/src/Components && ./build.sh --pack)
(cd AspNetCore && ./build.sh --pack)
```

