thisdir = .

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir))

include $(ROOTDIR)/mk/rules.make

standalone-all:: build

CLEAN_DIRECTORIES += output

standalone-build::
	dotnet build $(ROOTDIR)/Mono.Linker.Optimizer

standalone-build-release:: standalone-build
