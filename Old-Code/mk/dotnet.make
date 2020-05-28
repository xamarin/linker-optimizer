LINKER_OUTPUT := output

PROFILE = wasm
PROFILE_PATH := $(abspath $(MONO_ROOT)/mcs/class/lib/$(PROFILE))
AOTPROFILE_PATH := $(abspath $(MONO_ROOT)/mcs/class/lib/testing_aot_full)

MCS = csc
ILASM = ilasm

LINKER = dotnet run -p $(ROOTDIR)/Mono.Linker.Optimizer -f netcoreapp2.0 --

