### WITH DYNAMIC AND OPTIMIZER

```
$ ls -la ./bin/Debug/netstandard2.0/dist/_framework/_bin/
total 4936
drwxr-xr-x  2 mabaul  admin      884 Jul 18 14:37 .
drwxr-xr-x  4 mabaul  admin      238 Jul 18 14:37 ..
-rw-r--r--  1 mabaul  admin    15872 Jul 18 14:37 EmptyBlazor.dll
-rw-r--r--  1 mabaul  admin     2284 Jul 18 14:37 EmptyBlazor.pdb
-rw-r--r--  1 mabaul  admin    32256 Jul 18 14:37 Microsoft.AspNetCore.Blazor.dll
-rw-r--r--  1 mabaul  admin    11776 Jul 18 14:37 Microsoft.AspNetCore.Components.Browser.dll
-rw-r--r--  1 mabaul  admin   100352 Jul 18 14:37 Microsoft.AspNetCore.Components.dll
-rw-r--r--  1 mabaul  admin     5120 Jul 18 14:37 Microsoft.AspNetCore.Metadata.dll
-rw-r--r--  1 mabaul  admin    13312 Jul 18 14:37 Microsoft.Extensions.DependencyInjection.Abstractions.dll
-rw-r--r--  1 mabaul  admin     3468 Jul 18 14:37 Microsoft.Extensions.DependencyInjection.Abstractions.pdb
-rw-r--r--  1 mabaul  admin    55296 Jul 18 14:37 Microsoft.Extensions.DependencyInjection.dll
-rw-r--r--  1 mabaul  admin    22576 Jul 18 14:37 Microsoft.Extensions.DependencyInjection.pdb
-rw-r--r--  1 mabaul  admin     6656 Jul 18 14:37 Microsoft.Extensions.Logging.Abstractions.dll
-rw-r--r--  1 mabaul  admin     7680 Jul 18 14:37 Microsoft.Extensions.Options.dll
-rw-r--r--  1 mabaul  admin    20480 Jul 18 14:37 Microsoft.JSInterop.dll
-rw-r--r--  1 mabaul  admin     8192 Jul 18 14:37 Mono.Security.dll
-rw-r--r--  1 mabaul  admin     6144 Jul 18 14:37 Mono.WebAssembly.Interop.dll
-rw-r--r--  1 mabaul  admin    10752 Jul 18 14:37 System.Buffers.dll
-rw-r--r--  1 mabaul  admin   304640 Jul 18 14:37 System.Core.dll
-rw-r--r--  1 mabaul  admin    78336 Jul 18 14:37 System.Memory.dll
-rw-r--r--  1 mabaul  admin    99328 Jul 18 14:37 System.Net.Http.dll
-rw-r--r--  1 mabaul  admin    32256 Jul 18 14:37 System.Numerics.Vectors.dll
-rw-r--r--  1 mabaul  admin     6144 Jul 18 14:37 System.Runtime.CompilerServices.Unsafe.dll
-rw-r--r--  1 mabaul  admin   215552 Jul 18 14:37 System.Text.Json.dll
-rw-r--r--  1 mabaul  admin   137216 Jul 18 14:37 System.dll
-rw-r--r--  1 mabaul  admin  1292288 Jul 18 14:37 mscorlib.dll
$ du -ck ./bin/Debug/netstandard2.0/dist/_framework/_bin/
2096	./bin/Debug/netstandard2.0/dist/_framework/_bin/
2096	total
```

### WITHOUT DYNAMIC AND WITH OPTIMIZER:

```
$ ls -la ./bin/Debug/netstandard2.0/dist/_framework/_bin/
total 4192
drwxr-xr-x  2 mabaul  admin      884 Jul 18 15:03 .
drwxr-xr-x  4 mabaul  admin      238 Jul 18 15:03 ..
-rw-r--r--  1 mabaul  admin    15872 Jul 18 15:02 EmptyBlazor.dll
-rw-r--r--  1 mabaul  admin     2284 Jul 18 15:02 EmptyBlazor.pdb
-rw-r--r--  1 mabaul  admin    32256 Jul 18 15:02 Microsoft.AspNetCore.Blazor.dll
-rw-r--r--  1 mabaul  admin    11776 Jul 18 15:02 Microsoft.AspNetCore.Components.Browser.dll
-rw-r--r--  1 mabaul  admin   100352 Jul 18 15:02 Microsoft.AspNetCore.Components.dll
-rw-r--r--  1 mabaul  admin     5120 Jul 18 15:02 Microsoft.AspNetCore.Metadata.dll
-rw-r--r--  1 mabaul  admin    13312 Jul 18 15:02 Microsoft.Extensions.DependencyInjection.Abstractions.dll
-rw-r--r--  1 mabaul  admin     3468 Jul 18 15:02 Microsoft.Extensions.DependencyInjection.Abstractions.pdb
-rw-r--r--  1 mabaul  admin    47104 Jul 18 15:02 Microsoft.Extensions.DependencyInjection.dll
-rw-r--r--  1 mabaul  admin    19504 Jul 18 15:02 Microsoft.Extensions.DependencyInjection.pdb
-rw-r--r--  1 mabaul  admin     6656 Jul 18 15:02 Microsoft.Extensions.Logging.Abstractions.dll
-rw-r--r--  1 mabaul  admin     7680 Jul 18 15:02 Microsoft.Extensions.Options.dll
-rw-r--r--  1 mabaul  admin    20480 Jul 18 15:02 Microsoft.JSInterop.dll
-rw-r--r--  1 mabaul  admin     8192 Jul 18 15:02 Mono.Security.dll
-rw-r--r--  1 mabaul  admin     6144 Jul 18 15:02 Mono.WebAssembly.Interop.dll
-rw-r--r--  1 mabaul  admin    10752 Jul 18 15:02 System.Buffers.dll
-rw-r--r--  1 mabaul  admin    51200 Jul 18 15:02 System.Core.dll
-rw-r--r--  1 mabaul  admin    78336 Jul 18 15:02 System.Memory.dll
-rw-r--r--  1 mabaul  admin    99328 Jul 18 15:02 System.Net.Http.dll
-rw-r--r--  1 mabaul  admin    32256 Jul 18 15:02 System.Numerics.Vectors.dll
-rw-r--r--  1 mabaul  admin     6144 Jul 18 15:02 System.Runtime.CompilerServices.Unsafe.dll
-rw-r--r--  1 mabaul  admin   215552 Jul 18 15:02 System.Text.Json.dll
-rw-r--r--  1 mabaul  admin   137216 Jul 18 15:02 System.dll
-rw-r--r--  1 mabaul  admin  1178624 Jul 18 15:02 mscorlib.dll
$ du -ck ./bin/Debug/netstandard2.0/dist/_framework/_bin/
2468	./bin/Debug/netstandard2.0/dist/_framework/_bin/
2468	total
```

