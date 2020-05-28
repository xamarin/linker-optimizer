.DEFAULT: all
default: standalone-all

.PHONY: standalone-all standalone-build standalone-build-release standalone-clean build
standalone-all:: standalone-build

standalone-build::

include $(ROOTDIR)/mk/dotnet.make

TESTS_COMPILER = $(MCS) -nologo -noconfig -unsafe -nostdlib -debug:portable -r:$(PROFILE_PATH)/mscorlib.dll
AOTTESTS_COMPILER = $(MCS) -nologo -noconfig -unsafe -nostdlib -debug:portable -r:$(AOTPROFILE_PATH)/mscorlib.dll

CLEAN_FILES += *.exe *.dll *.pdb

all: standalone-all

clean: standalone-clean

standalone-clean::
	@rm -f $(CLEAN_FILES)
	@rm -rf $(CLEAN_DIRECTORIES)

