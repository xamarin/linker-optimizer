thisdir = tools/linker/Martin/Tests/Corlib

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir)/../..)

include $(ROOTDIR)/build/rules.make

LOCAL_LINKER_ARGS := --martin-options -main-debug --martin-report $(LINKER_OUTPUT)/martin-report.xml

TEST_CASES := \
	test-globalization-1.cs \
	test-globalization-2.cs \
	test-globalization-3.cs \
	test-globalization-4.cs \
	test-globalization-5.cs \
	test-crypto-1.cs \
	test-security-1.cs \
	test-remoting-1.cs \
	test-remoting-2.cs \
	test-size-check-1.cs \
	test-size-check-2.cs \
	test-size-check-3.cs \
	test-size-check-4.cs \
	test-size-check-5.cs

AOTTEST_CASES := \
	aottest-crypto-1.cs \
	aottest-remoting-1.cs

include $(ROOTDIR)/build/tests.make

$(eval $(call RunTests,globalization))
$(eval $(call RunTests,crypto))
$(eval $(call RunTests,security))
$(eval $(call RunTests,remoting))
$(eval $(call RunTests,size-check))
