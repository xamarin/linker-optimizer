MONO_ROOT = $(abspath $(ROOTDIR)/../../../..)
include $(MONO_ROOT)/mcs/build/rules.make

LINKER_OUTPUT := output
PROFILE_PATH := $(abspath $(MONO_ROOT)/mcs/class/lib/$(PROFILE_DIRECTORY))
AOTPROFILE_PATH := $(abspath $(MONO_ROOT)/mcs/class/lib/testing_aot_full)
XUNIT_PATH := $(MONO_ROOT)/external/xunit-binaries

PROFILER_FLAGS := --profile=log:calls,calldepth=100

LINKER_EXE = $(PROFILE_PATH)/monolinker-optimizer.exe
LINKER_RELEASE_EXE = $(PROFILE_PATH)/monolinker-optimizer.exe
LINKER = MONO_PATH=$(MONO_ROOT)/mcs/class/lib/build $(RUNTIME) $(RUNTIME_FLAGS) --debug $(LINKER_EXE)

LINKER_ARGS = -out $(LINKER_OUTPUT) -b true -d $(PROFILE_PATH)
LINKER_ARGS_DEFAULT = $(LINKER_ARGS) -c link -l none --exclude-feature sre --exclude-feature security --exclude-feature globalization
LINKER_ARGS_CORLIB_TEST = $(LINKER_ARGS) -c copy -p link mscorlib -l none --exclude-feature sre
LINKER_ARGS_AOT = -out $(LINKER_OUTPUT) -b true -d $(AOTPROFILE_PATH) -c link -l none --exclude-feature sre --exclude-feature security --exclude-feature globalization

TEST_EXEC = MONO_PATH=$(LINKER_OUTPUT) $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot
NUNIT_ARGS := -exclude=NotOnMac,MacNotWorking,ReflectionEmit,NotWorking,CAS,LinkerNotWorking,$(EXTRA_NUNIT_EXCLUDES)
XUNIT_ARGS := -noappdomain -noshadow -parallel none -notrait category=failing -notrait category=nonmonotests -notrait Benchmark=true -notrait category=outerloop -notrait category=nonosxtests

