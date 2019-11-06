Full build output:

```
$ make -w V=1 build-aot-sample
make: Entering directory `/Users/Workspace/mono-linker/sdks/wasm'
MONO_PATH=/Users/Workspace/mono-linker/mcs/class/lib/build /Users/Workspace/mono-linker/sdks/builds/bcl/runtime/mono-wrapper /Users/Workspace/mono-linker/external/roslyn-binaries/Microsoft.Net.Compilers/3.3.0/csc.exe /debug:portable /noconfig /nostdlib /nologo /langversion:latest -out:hello.exe /r:/Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm/mscorlib.dll /r:/Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm/System.Core.dll /r:/Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm/System.dll /r:/Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm/System.Net.Http.dll /r:/Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm/Facades/netstandard.dll /r:/Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm/System.IO.Compression.dll /r:/Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm/Facades/System.Memory.dll  hello.cs
mono --debug packager.exe --emscripten-sdkdir=/Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk --mono-sdkdir=/Users/Workspace/mono-linker/sdks/out -appdir=bin/aot-sample --nobinding --builddir=obj/aot-sample --aot --template=runtime-tests.js --pinvoke-libs=libfoo hello.exe
ninja -v -C obj/aot-sample
ninja: Entering directory `obj/aot-sample'
[1/35] mkdir -p /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample
[2/35] mkdir -p /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/managed
[3/35] mkdir -p aot-in
[4/35] mkdir -p ilstrip-out
[5/35] cp /Users/Workspace/mono-linker/sdks/wasm/hello.exe linker-in/hello.exe
[6/35] mkdir -p linker-out
[7/35] cp /Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm/mscorlib.dll linker-in/mscorlib.dll
[8/35] if cmp -s runtime.js /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/runtime.js ; then : ; else cp runtime.js /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/runtime.js ; fi
[9/35] if cmp -s mono-config.js /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/mono-config.js ; then : ; else cp mono-config.js /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/mono-config.js ; fi
[10/35] if cmp -s driver-gen.c.in driver-gen.c ; then : ; else cp driver-gen.c.in driver-gen.c ; fi
[11/35] if cmp -s /Users/Workspace/mono-linker/sdks/wasm/src/zlib-helper.c zlib-helper.c ; then : ; else cp /Users/Workspace/mono-linker/sdks/wasm/src/zlib-helper.c zlib-helper.c ; fi
[12/35] if cmp -s /Users/Workspace/mono-linker/sdks/wasm/src/driver.c driver.c ; then : ; else cp /Users/Workspace/mono-linker/sdks/wasm/src/driver.c driver.c ; fi
[13/35] if cmp -s /Users/Workspace/mono-linker/sdks/wasm/src/pinvoke-tables-default.h pinvoke-tables-default.h ; then : ; else cp /Users/Workspace/mono-linker/sdks/wasm/src/pinvoke-tables-default.h pinvoke-tables-default.h ; fi
[14/35] /Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/emsdk construct_env emsdk_env.sh
Adding directories to PATH:
PATH += /Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk
PATH += /Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/upstream/emscripten
PATH += /Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/node/12.9.1_64bit/bin

Setting environment variables:
EMSDK = /Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk
EM_CONFIG = /Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/.emscripten
EM_CACHE = /Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/.emscripten_cache
EMSDK_NODE = /Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/node/12.9.1_64bit/bin/node

[15/35] echo > aot-dummy.cs; csc /deterministic /out:linker-out/aot-dummy.dll /target:library aot-dummy.cs
Microsoft (R) Visual C# Compiler version 3.3.0-beta2-19381-14 (ef3a7a38)
Copyright (C) Microsoft Corporation. All rights reserved.

[16/35] if cmp -s linker-out/aot-dummy.dll /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/managed/aot-dummy.dll ; then : ; else cp linker-out/aot-dummy.dll /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/managed/aot-dummy.dll ; fi
[17/35] bash -c 'source ./emsdk_env.sh && emcc -Oz -g -s USE_ZLIB=1 -s DISABLE_EXCEPTION_CATCHING=0 -s ASSERTIONS=1 -s WASM=1 -s ALLOW_MEMORY_GROWTH=1 -s BINARYEN=1 -s TOTAL_MEMORY=134217728 -s ALIASING_FUNCTION_POINTERS=0 -s NO_EXIT_RUNTIME=1 -s ERROR_ON_UNDEFINED_SYMBOLS=1 -s "EXTRA_EXPORTED_RUNTIME_METHODS=['ccall', 'cwrap', 'setValue', 'getValue', 'UTF8ToString']" -s "EXPORTED_FUNCTIONS=['___cxa_is_pointer_type', '___cxa_can_catch']" -s "DEFAULT_LIBRARY_FUNCS_TO_INCLUDE=['setThrew', 'memset']" -I/Users/Workspace/mono-linker/sdks/out/wasm-runtime-release/include/mono-2.0 -I/Users/Workspace/mono-linker/sdks/out/wasm-runtime-release/include/support -c -o zlib-helper.o zlib-helper.c'
[18/35] mono /Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm_tools/monolinker.exe -out ./linker-out -l none --deterministic --explicit-reflection --disable-opt unreachablebodies --exclude-feature com --exclude-feature remoting --exclude-feature etw -a linker-in/hello.exe -d linker-in -d /Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm -d /Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm/Facades -c link -u link  || exit 1; for f in linker-out/hello.exe linker-out/mscorlib.dll; do if test ! -f $f; then echo > empty.cs; csc /deterministic /nologo /out:$f /target:library empty.cs; else touch $f; fi; done
[19/35] if cmp -s linker-out/mscorlib.dll aot-in/mscorlib.dll ; then : ; else cp linker-out/mscorlib.dll aot-in/mscorlib.dll ; fi
[20/35] mono /Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm_tools/wasm-tuner.exe --gen-pinvoke-table System.Native,libfoo linker-out/hello.exe linker-out/mscorlib.dll linker-out/aot-dummy.dll > pinvoke-table.h
[21/35] cp linker-out/hello.exe ilstrip-out/hello.exe; mono /Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm_tools/mono-cil-strip.exe ilstrip-out/hello.exe
Mono CIL Stripper

Assembly ilstrip-out/hello.exe stripped
[22/35] if cmp -s ilstrip-out/hello.exe /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/managed/hello.exe ; then : ; else cp ilstrip-out/hello.exe /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/managed/hello.exe ; fi
[23/35] MONO_PATH=./aot-in:./linker-out /Users/Workspace/mono-linker/sdks/out/wasm-cross-release/bin/wasm32-unknown-none-mono-sgen --debug  --aot=dedup-skip,llvmonly,asmonly,no-opt,static,direct-icalls,deterministic,llvm-path=/Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/upstream/bin,,depfile=./linker-out/hello.exe.depfile,llvm-outfile=./hello.exe.bc.tmp ./linker-out/hello.exe
Mono Ahead of Time compiler - compiling assembly /Users/Workspace/mono-linker/sdks/wasm/obj/aot-sample/linker-out/hello.exe
Compiled: 6/6
Output file: '/var/folders/1h/csdp37293890y0fw8x72tsch0000gq/T/mono_aot_R9DrJs'.
Linking symbol: 'mono_aot_module_hello_info'.
LLVM output file: './hello.exe.bc.tmp'.
JIT time: 0 ms, Generation time: 1 ms, Assembly+Link time: 0 ms.
[24/35] if cmp -s hello.exe.bc.tmp hello.exe.bc ; then : ; else cp hello.exe.bc.tmp hello.exe.bc ; fi
[25/35] cp linker-out/mscorlib.dll ilstrip-out/mscorlib.dll; mono /Users/Workspace/mono-linker/sdks/out/wasm-bcl/wasm_tools/mono-cil-strip.exe ilstrip-out/mscorlib.dll
Mono CIL Stripper

Assembly ilstrip-out/mscorlib.dll stripped
[26/35] if cmp -s ilstrip-out/mscorlib.dll /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/managed/mscorlib.dll ; then : ; else cp ilstrip-out/mscorlib.dll /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/managed/mscorlib.dll ; fi
[27/35] bash -c 'source ./emsdk_env.sh && emcc -Oz -g -s USE_ZLIB=1 -s DISABLE_EXCEPTION_CATCHING=0 -s ASSERTIONS=1 -s WASM=1 -s ALLOW_MEMORY_GROWTH=1 -s BINARYEN=1 -s TOTAL_MEMORY=134217728 -s ALIASING_FUNCTION_POINTERS=0 -s NO_EXIT_RUNTIME=1 -s ERROR_ON_UNDEFINED_SYMBOLS=1 -s "EXTRA_EXPORTED_RUNTIME_METHODS=['ccall', 'cwrap', 'setValue', 'getValue', 'UTF8ToString']" -s "EXPORTED_FUNCTIONS=['___cxa_is_pointer_type', '___cxa_can_catch']" -s "DEFAULT_LIBRARY_FUNCS_TO_INCLUDE=['setThrew', 'memset']"  -c -o hello.exe.o hello.exe.bc'
[28/35] bash -c 'source ./emsdk_env.sh && emcc -Oz -g -s USE_ZLIB=1 -s DISABLE_EXCEPTION_CATCHING=0 -s ASSERTIONS=1 -s WASM=1 -s ALLOW_MEMORY_GROWTH=1 -s BINARYEN=1 -s TOTAL_MEMORY=134217728 -s ALIASING_FUNCTION_POINTERS=0 -s NO_EXIT_RUNTIME=1 -s ERROR_ON_UNDEFINED_SYMBOLS=1 -s "EXTRA_EXPORTED_RUNTIME_METHODS=['ccall', 'cwrap', 'setValue', 'getValue', 'UTF8ToString']" -s "EXPORTED_FUNCTIONS=['___cxa_is_pointer_type', '___cxa_can_catch']" -s "DEFAULT_LIBRARY_FUNCS_TO_INCLUDE=['setThrew', 'memset']" -DENABLE_AOT=1 -DGEN_PINVOKE  -DDRIVER_GEN=1 -I/Users/Workspace/mono-linker/sdks/out/wasm-runtime-release/include/mono-2.0 -c -o driver.o driver.c'
[29/35] MONO_PATH=./aot-in:./linker-out /Users/Workspace/mono-linker/sdks/out/wasm-cross-release/bin/wasm32-unknown-none-mono-sgen --debug  --aot=dedup-skip,llvmonly,asmonly,no-opt,static,direct-icalls,deterministic,llvm-path=/Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/upstream/bin,,depfile=./linker-out/mscorlib.dll.depfile,llvm-outfile=./mscorlib.dll.bc.tmp ./aot-in/mscorlib.dll
Mono Ahead of Time compiler - compiling assembly /Users/Workspace/mono-linker/sdks/wasm/obj/aot-sample/aot-in/mscorlib.dll
Compiled: 5176/5177
Output file: '/var/folders/1h/csdp37293890y0fw8x72tsch0000gq/T/mono_aot_nNdMRA'.
Linking symbol: 'mono_aot_module_mscorlib_info'.
LLVM output file: './mscorlib.dll.bc.tmp'.
JIT time: 659 ms, Generation time: 381 ms, Assembly+Link time: 0 ms.
[30/35] if cmp -s mscorlib.dll.bc.tmp mscorlib.dll.bc ; then : ; else cp mscorlib.dll.bc.tmp mscorlib.dll.bc ; fi
[31/35] MONO_PATH=./aot-in:./linker-out /Users/Workspace/mono-linker/sdks/out/wasm-cross-release/bin/wasm32-unknown-none-mono-sgen --debug  --aot=llvmonly,asmonly,no-opt,static,direct-icalls,deterministic,llvm-path=/Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/upstream/bin,,llvm-outfile=./aot-dummy.dll.bc.tmp,dedup-include=aot-dummy.dll ./linker-out/hello.exe ./aot-in/mscorlib.dll ./linker-out/aot-dummy.dll
Mono Ahead of Time compiler - compiling assembly /Users/Workspace/mono-linker/sdks/wasm/obj/aot-sample/linker-out/aot-dummy.dll
Compiled: 1871/1871
Output file: '/var/folders/1h/csdp37293890y0fw8x72tsch0000gq/T/mono_aot_SAAisN'.
Linking symbol: 'mono_aot_module_aot_dummy_info'.
LLVM output file: './aot-dummy.dll.bc.tmp'.
JIT time: 237 ms, Generation time: 110 ms, Assembly+Link time: 0 ms.
[32/35] if cmp -s aot-dummy.dll.bc.tmp aot-dummy.dll.bc ; then : ; else cp aot-dummy.dll.bc.tmp aot-dummy.dll.bc ; fi
[33/35] bash -c 'source ./emsdk_env.sh && emcc -Oz -g -s USE_ZLIB=1 -s DISABLE_EXCEPTION_CATCHING=0 -s ASSERTIONS=1 -s WASM=1 -s ALLOW_MEMORY_GROWTH=1 -s BINARYEN=1 -s TOTAL_MEMORY=134217728 -s ALIASING_FUNCTION_POINTERS=0 -s NO_EXIT_RUNTIME=1 -s ERROR_ON_UNDEFINED_SYMBOLS=1 -s "EXTRA_EXPORTED_RUNTIME_METHODS=['ccall', 'cwrap', 'setValue', 'getValue', 'UTF8ToString']" -s "EXPORTED_FUNCTIONS=['___cxa_is_pointer_type', '___cxa_can_catch']" -s "DEFAULT_LIBRARY_FUNCS_TO_INCLUDE=['setThrew', 'memset']"  -c -o aot-dummy.dll.o aot-dummy.dll.bc'
[34/35] bash -c 'source ./emsdk_env.sh && emcc -Oz -g -s USE_ZLIB=1 -s DISABLE_EXCEPTION_CATCHING=0 -s ASSERTIONS=1 -s WASM=1 -s ALLOW_MEMORY_GROWTH=1 -s BINARYEN=1 -s TOTAL_MEMORY=134217728 -s ALIASING_FUNCTION_POINTERS=0 -s NO_EXIT_RUNTIME=1 -s ERROR_ON_UNDEFINED_SYMBOLS=1 -s "EXTRA_EXPORTED_RUNTIME_METHODS=['ccall', 'cwrap', 'setValue', 'getValue', 'UTF8ToString']" -s "EXPORTED_FUNCTIONS=['___cxa_is_pointer_type', '___cxa_can_catch']" -s "DEFAULT_LIBRARY_FUNCS_TO_INCLUDE=['setThrew', 'memset']"  -c -o mscorlib.dll.o mscorlib.dll.bc'
[35/35] bash -c 'source ./emsdk_env.sh && emcc -Oz -g -s USE_ZLIB=1 -s DISABLE_EXCEPTION_CATCHING=0 -s ASSERTIONS=1 -s WASM=1 -s ALLOW_MEMORY_GROWTH=1 -s BINARYEN=1 -s TOTAL_MEMORY=134217728 -s ALIASING_FUNCTION_POINTERS=0 -s NO_EXIT_RUNTIME=1 -s ERROR_ON_UNDEFINED_SYMBOLS=1 -s "EXTRA_EXPORTED_RUNTIME_METHODS=['ccall', 'cwrap', 'setValue', 'getValue', 'UTF8ToString']" -s "EXPORTED_FUNCTIONS=['___cxa_is_pointer_type', '___cxa_can_catch']" -s "DEFAULT_LIBRARY_FUNCS_TO_INCLUDE=['setThrew', 'memset']"  -o /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/mono.js --js-library /Users/Workspace/mono-linker/sdks/wasm/src/library_mono.js --js-library /Users/Workspace/mono-linker/sdks/wasm/src/dotnet_support.js  driver.o zlib-helper.o hello.exe.o mscorlib.dll.o aot-dummy.dll.o /Users/Workspace/mono-linker/sdks/out/wasm-runtime-release/lib/libmonosgen-2.0.a /Users/Workspace/mono-linker/sdks/out/wasm-runtime-release/lib/libmono-native.a'  && /Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/upstream/bin/wasm-strip /Users/Workspace/mono-linker/sdks/wasm/bin/aot-sample/mono.wasm
cache:INFO: generating system library: libc++.a... (this will be cached in "/Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/.emscripten_cache/wasm-obj/libc++.a" for subsequent builds)
cache:INFO:  - ok
cache:INFO: generating system library: libc++abi.a... (this will be cached in "/Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/.emscripten_cache/wasm-obj/libc++abi.a" for subsequent builds)
cache:INFO:  - ok
make: Leaving directory `/Users/Workspace/mono-linker/sdks/wasm'
```

Just looking at the AOT invocation:

```
MONO_PATH=./aot-in:./linker-out /Users/Workspace/mono-linker/sdks/out/wasm-cross-release/bin/wasm32-unknown-none-mono-sgen --debug  --aot=dedup-skip,llvmonly,asmonly,no-opt,static,direct-icalls,deterministic,llvm-path=/Users/Workspace/mono-linker/sdks/builds/toolchains/emsdk/upstream/bin,,depfile=./linker-out/hello.exe.depfile,llvm-outfile=./hello.exe.bc.tmp ./linker-out/hello.exe
```

