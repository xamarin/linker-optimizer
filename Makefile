thisdir = tools/linker/Martin

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir))

include $(ROOTDIR)/build/rules.make

standalone-all:: build

CLEAN_DIRECTORIES += output

ifdef STANDALONE_MAKE

standalone-build::
	msbuild /nologo /verbosity:quiet $(ROOTDIR)/Mono.Linker.Optimizer.sln

standalone-build-release::
	msbuild /nologo /verbosity:quiet /p:Configuration=Release $(ROOTDIR)/Mono.Linker.Optimizer.sln

endif

ifdef INTEGRATED_MAKE

PROGRAM = monolinker-optimizer.exe

LOCAL_MCS_FLAGS = /main:Mono.Linker.Optimizer.Program /r:$(the_libdir)monolinker.exe
LIB_REFS = System System.Core System.Xml Mono.Cecil

include $(ROOTDIR)/../../../build/executable.make

standalone-build:: $(the_lib)

standalone-build-release:: $(the_lib)

endif
