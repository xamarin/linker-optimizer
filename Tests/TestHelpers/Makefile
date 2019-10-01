thisdir = tools/linker/Martin/Tests/TestHelpers

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir)/../..)

include $(ROOTDIR)/mk/rules.make

CORLIB_SOURCES := \
	../../Mono.Linker.Optimizer/System.Runtime.CompilerServices/MonoLinkerFeature.cs \
	../../Mono.Linker.Optimizer/System.Runtime.CompilerServices/MonoLinkerSupport.cs

TEST_HELPER_SOURCES := \
	$(CORLIB_SOURCES) \
	AssertionException.cs \
	TestHelpers.cs

TEST_HELPERS_LIBRARY := TestHelpers.dll

standalone-all:: $(TEST_HELPERS_LIBRARY)

$(TEST_HELPERS_LIBRARY): $(TEST_HELPER_SOURCES)
	$(TESTS_COMPILER) /t:library /out:$@ $^

