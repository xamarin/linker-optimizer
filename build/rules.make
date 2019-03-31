.DEFAULT: all
default: standalone-all

.PHONY: standalone-all standalone-build standalone-build-release standalone-clean build
standalone-all:: standalone-build

standalone-build::

ifneq "$(MONO_ROOT)" ""
STANDALONE_MAKE = 1
else
ifneq "$(wildcard $(ROOTDIR)/../net_4_x-linked-size.csv)" ""
INTEGRATED_MAKE = 1
else
STANDALONE_MAKE = 1
endif
endif

ifdef INTEGRATED_MAKE
include $(ROOTDIR)/build/mono.make
endif

ifdef STANDALONE_MAKE
include $(ROOTDIR)/build/standalone.make
endif

TESTS_COMPILER = $(MCS) -nologo -noconfig -unsafe -nostdlib -debug:portable -r:$(PROFILE_PATH)/mscorlib.dll
AOTTESTS_COMPILER = $(MCS) -nologo -noconfig -unsafe -nostdlib -debug:portable -r:$(AOTPROFILE_PATH)/mscorlib.dll

CLEAN_FILES += *.exe *.dll *.pdb

all: standalone-all

clean: standalone-clean

standalone-clean::
	@rm -f $(CLEAN_FILES)
	@rm -rf $(CLEAN_DIRECTORIES)

