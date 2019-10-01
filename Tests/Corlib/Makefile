thisdir = tools/linker/Martin/Tests/Corlib

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir)/../..)

include $(ROOTDIR)/mk/rules.make

TEST_LINKER_ARGS := --optimizer-report $(LINKER_OUTPUT)/martin-report.xml --optimizer-options report-profile=$(PROFILE)

BROKEN_TEST_CASES := \
	test-encoding-1.cs \
	test-globalization-4.cs

AOTTEST_CASES := \
	aottest-crypto-1.cs \
	aottest-remoting-1.cs

TEST_CASES := \
	test-crypto-1.cs \
	test-globalization-1.cs \
	test-globalization-2.cs \
	test-globalization-3.cs \
	test-globalization-5.cs \
	test-globalization-5.cs \
	test-remoting-1.cs \
	test-remoting-2.cs \
	test-remoting-3.cs \
	test-remoting-4.cs \
	test-security-1.cs \
	test-size-check-full.cs \
	test-size-check-collator.cs \
	test-size-check-globalization.cs \
	test-size-check-reflection-emit.cs \
	test-size-check-security.cs \
	test-size-check-remoting.cs \
	test-size-check-all.cs \

include $(ROOTDIR)/mk/tests.make

$(eval $(call RunTests,globalization))
$(eval $(call RunTests,crypto))
$(eval $(call RunTests,security))
$(eval $(call RunTests,remoting))
$(eval $(call RunTests,size-check))

all-size-checks::
	$(MAKE) LOCAL_LINKER_ARGS='--optimizer-options size-check-tolerance=0' test-size-check
	$(MAKE) LOCAL_LINKER_ARGS='--optimizer-options size-check-tolerance=0' aot-test-size-check
	$(MAKE) LOCAL_LINKER_ARGS='--optimizer-options size-check-tolerance=0' wasm-test-size-check
