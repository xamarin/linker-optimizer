thisdir = tools/linker/Martin

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir))

include rules.make

ifdef STANDALONE_MAKE

build:
	msbuild /nologo /verbosity:minimal $(ROOTDIR)/Mono.Linker.Optimizer.sln

build-release:
	msbuild /nologo /verbosity:minimal /p:Configuration=Release $(ROOTDIR)Mono.Linker.Optimizer.sln

endif

ifdef INTEGRATED_MAKE

PROGRAM = monolinker-optimizer.exe

LOCAL_MCS_FLAGS = /main:Mono.Linker.Optimizer.Program
LIB_REFS = System System.Core System.Xml Mono.Cecil

include $(ROOTDIR)/../../../build/executable.make

build: $(the_lib)

build-release: $(the_lib)

endif
