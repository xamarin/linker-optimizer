thisdir = tools/linker/Martin/Tests/Regression

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir)/../..)

include $(ROOTDIR)/build/rules.make

TEST_CASES := \
	test-weak-instances-1.cs \
	test-weak-instances-2.cs \
	test-weak-instances-3.cs \
	test-weak-instances-4.cs \
	test-weak-instances-5.cs \
	test-weak-instances-6.cs \
	test-features-1.cs \
	test-features-2.cs \
	test-features-3.cs \
	test-features-4.cs \
	test-features-5.cs \
	test-features-6.cs \
	test-features-7.cs \
	test-conditionals-1.cs \
	test-conditionals-2.cs \
	test-conditionals-3.cs \
	test-conditionals-4.cs \
	test-conditionals-5.cs \
	test-conditionals-6.cs \
	test-flow-analysis-1.cs \
	test-flow-analysis-2.cs \
	test-flow-analysis-3.cs \
	test-flow-analysis-4.cs \
	test-flow-analysis-5.cs

ILTEST_CASES := \
	iltest-conditionals-1.il \
	iltest-conditionals-2.il \
	iltest-conditionals-3.il \
	iltest-conditionals-4.il \
	iltest-conditionals-5.il \
	iltest-flow-analysis-1.il


include $(ROOTDIR)/build/tests.make

$(eval $(call RunTests,weak-instances))
$(eval $(call RunTests,features))
$(eval $(call RunTests,conditionals))
$(eval $(call RunTests,flow-analysis))

$(eval $(call RunIlTests,conditionals))
$(eval $(call RunIlTests,flow-analysis))

