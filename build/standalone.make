COMPILER_OUTPUT := $(ROOTDIR)/output
LINKER_OUTPUT := output

PROFILE = net_4_x
PROFILE_PATH := $(abspath $(MONO_ROOT)/mcs/class/lib/$(PROFILE))
AOTPROFILE_PATH := $(abspath $(MONO_ROOT)/mcs/class/lib/testing_aot_full)

MCS = csc
ILASM = ilasm
RUNTIME = $(MONO_ROOT)/runtime/mono-wrapper
RUNTIME_FLAGS =

PROFILER_FLAGS := --profile=log:calls,calldepth=100

# XUNIT_PATH := $(abspath $(topdir)/..)/external/xunit-binaries

LINKER_EXE = $(COMPILER_OUTPUT)/bin/Debug/Mono.Linker.Optimizer.exe
LINKER_RELEASE_EXE = $(COMPILER_OUTPUT)/bin/Release/Mono.Linker.Optimizer.exe
LINKER = MONO_PATH=$(MONO_ROOT)/mcs/class/lib/build $(RUNTIME) $(RUNTIME_FLAGS) --debug $(LINKER_EXE)

LINKER_ARGS = -out $(LINKER_OUTPUT) -b true -d $(PROFILE_PATH) -d $(ROOTDIR)/Tests/TestHelpers
LINKER_ARGS_DEFAULT = $(LINKER_ARGS) -c link -l none
LINKER_ARGS_CORLIB_TEST = $(LINKER_ARGS) -c copy -p link mscorlib -l none --exclude-feature sre
LINKER_ARGS_AOT = -out $(LINKER_OUTPUT) -b true -d $(AOTPROFILE_PATH) -c link -l none

TEST_EXEC = MONO_PATH=$(LINKER_OUTPUT) $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot
NUNIT_ARGS := -exclude=NotOnMac,MacNotWorking,ReflectionEmit,NotWorking,CAS,LinkerNotWorking,$(EXTRA_NUNIT_EXCLUDES)
XUNIT_ARGS := -noappdomain -noshadow -parallel none -notrait category=failing -notrait category=nonmonotests -notrait Benchmark=true -notrait category=outerloop -notrait category=nonosxtests
