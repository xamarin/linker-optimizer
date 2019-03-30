topdir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
abs_topdir := $(abspath $(topdir))

LINKER_OUTPUT := output

PROFILE = net_4_x
PROFILE_PATH := $(abspath $(MONO_ROOT)/mcs/class/lib/$(PROFILE))
AOTPROFILE_PATH := $(abspath $(MONO_ROOT)/mcs/class/lib/testing_aot_full)

MCS = csc
ILASM = ilasm
RUNTIME = $(MONO_ROOT)/runtime/mono-wrapper
RUNTIME_FLAGS =

# XUNIT_PATH := $(abspath $(topdir)/..)/external/xunit-binaries

LINKER = MONO_PATH=$(MONO_ROOT)/mcs/class/lib/build $(RUNTIME) $(RUNTIME_FLAGS) --debug ../bin/Debug/Mono.Linker.Optimizer.exe

LINKER_ARGS = -out $(LINKER_OUTPUT) -b true -d $(PROFILE_PATH)
LINKER_ARGS_DEFAULT = $(LINKER_ARGS) -c link -l none --exclude-feature sre --exclude-feature security --exclude-feature globalization
LINKER_ARGS_CORLIB_TEST = $(LINKER_ARGS) -c copy -p link mscorlib -l none --exclude-feature sre
LINKER_ARGS_AOT = --out $(LINKER_OUTPUT) -b true -d $(AOTPROFILE_PATH) -c link -l none --exclude-feature sre --exclude-feature security --exclude-feature globalization

TEST_EXEC = MONO_PATH=$(LINKER_OUTPUT) $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot
NUNIT_ARGS := -exclude=NotOnMac,MacNotWorking,ReflectionEmit,NotWorking,CAS,LinkerNotWorking,$(EXTRA_NUNIT_EXCLUDES)
XUNIT_ARGS := -noappdomain -noshadow -parallel none -notrait category=failing -notrait category=nonmonotests -notrait Benchmark=true -notrait category=outerloop -notrait category=nonosxtests

ifdef TESTNAME
TESTNAME_ARG = -test=$(TESTNAME)
endif

TESTS_COMPILER = $(MCS) -nologo -noconfig -unsafe -nostdlib -debug:portable -r:$(PROFILE_PATH)/mscorlib.dll
AOTTESTS_COMPILER = $(MCS) -nologo -noconfig -unsafe -nostdlib -debug:portable -r:$(AOTPROFILE_PATH)/mscorlib.dll

CLEAN_FILES += *.exe *.dll *.pdb

CORLIB_SOURCES := \
	../System.Runtime.CompilerServices/MonoLinkerFeature.cs \
	../System.Runtime.CompilerServices/MonoLinkerSupport.cs

TEST_HELPER_SOURCES := \
	$(CORLIB_SOURCES) \
	AssertionException.cs \
	TestHelpers.cs

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
	test-flow-analysis-5.cs \
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

BROKEN_TESTS :=

ILTEST_CASES := \
	iltest-conditionals-1.il \
	iltest-conditionals-2.il \
	iltest-conditionals-3.il \
	iltest-conditionals-4.il \
	iltest-conditionals-5.il \
	iltest-flow-analysis-1.il

AOTTEST_CASES := \
	aottest-crypto-1.cs \
	aottest-remoting-1.cs

TEST_HELPERS_LIBRARY := TestHelpers.dll

all: build compile-tests

$(TEST_HELPERS_LIBRARY): $(TEST_HELPER_SOURCES)
	$(TESTS_COMPILER) /t:library /out:$@ $^

compile-tests: $(TEST_CASES:.cs=.exe) $(ILTEST_CASES:.il=.exe) $(AOTTEST_CASES:.cs=.exe) $(BROKEN_TESTS:.cs=.exe) $(TEST_HELPERS_LIBRARY)

run: $(TEST_CASES:.cs=) $(AOTTEST_CASES:.cs=) $(ILTEST_CASES:.il=) build

test-%.exe: test-%.cs $(TEST_HELPERS_LIBRARY)
	$(TESTS_COMPILER) /optimize /out:$@ /r:$(TEST_HELPERS_LIBRARY) $(filter test-%.cs,$^)

aottest-%.exe: aottest-%.cs $(TEST_HELPERS_LIBRARY)
	$(AOTTESTS_COMPILER) /optimize /out:$@  $(filter aottest-%.cs,$^)

iltest-%.exe: iltest-%.il $(TEST_HELPERS_LIBRARY)
	$(ILASM) /out:$@ $(filter iltest-%.il,$^)

define RunTests
test-$(1): build $(patsubst %.cs,%,$(filter test-$(1)-%.cs,$(TEST_CASES)))
endef

define RunIlTests
iltest-$(1): build $(patsubst %.il,%,$(filter iltest-$(1)-%.il,$(ILTEST_CASES)))
endef

iltests: build $(ILTEST_CASES:.il=)

$(eval $(call RunTests,weak-instances))
$(eval $(call RunTests,features))
$(eval $(call RunTests,conditionals))
$(eval $(call RunTests,flow-analysis))
$(eval $(call RunTests,globalization))
$(eval $(call RunTests,size-check))

$(eval $(call RunIlTests,conditionals))
$(eval $(call RunIlTests,flow-analysis))

test-%: test-%.exe build
	@echo RUN TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(LINKER) --martin $(if $(wildcard $(@F).xml), --martin-xml $(@F).xml) $(LINKER_ARGS_DEFAULT) --dump-dependencies -a $@
	#@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/$(@F).exe > $(LINKER_OUTPUT)/$(@F).il
	MONO_PATH=$(PROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	@! grep AssertRemoved $(LINKER_OUTPUT)/$(@F).il
	@ls -lR $(LINKER_OUTPUT)
	(cd $(LINKER_OUTPUT); MONO_PATH=. $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot ./$(@F).exe)
	#@rm -rf $(LINKER_OUTPUT)

aottest-%: aottest-%.exe build
	@echo RUN TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(LINKER) --martin $(if $(wildcard $(@F).xml), --martin-xml $(@F).xml) $(LINKER_ARGS_AOT) --dump-dependencies -a $@
	#@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(AOTPROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(AOTPROFILE_PATH) monodis $(LINKER_OUTPUT)/$(@F).exe > $(LINKER_OUTPUT)/$(@F).il
	MONO_PATH=$(AOTPROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	@! grep AssertRemoved $(LINKER_OUTPUT)/$(@F).il
	@ls -lR $(LINKER_OUTPUT)
	(cd $(LINKER_OUTPUT); MONO_PATH=. $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot ./$(@F).exe)
	#@rm -rf $(LINKER_OUTPUT)

iltest-%: iltest-%.exe build
	@echo RUN TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(LINKER) --martin $(if $(wildcard $(@F).xml), --martin-xml $(@F).xml) $(LINKER_ARGS_DEFAULT) --dump-dependencies -a $@
	#@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/$(@F).exe > $(LINKER_OUTPUT)/$(@F).il
	MONO_PATH=$(PROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	@! grep AssertRemoved $(LINKER_OUTPUT)/$(@F).il
	@ls -lR $(LINKER_OUTPUT)
	(cd $(LINKER_OUTPUT); MONO_PATH=. $(RUNTIME) $(RUNTIME_FLAGS) --debug -O=-aot ./$(@F).exe)
	#@rm -rf $(LINKER_OUTPUT)

corlib-test: build
	@echo RUN CORLIB TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(LINKER) --martin --martin-xml corlib-nunit.xml $(LINKER_ARGS_CORLIB_TEST) --dump-dependencies -a $(PROFILE_PATH)/tests/net_4_x_corlib_test.dll -x $(abs_topdir)/class/corlib/LinkerDescriptor/mscorlib.xml
	#@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(PROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	(cd $(LINKER_OUTPUT); MONO_PATH=.:$(PROFILE_PATH) $(RUNTIME) $(RUNTIME_FLAGS) --debug $(PROFILE_PATH)/nunit-lite-console.exe net_4_x_corlib_test.dll $(NUNIT_ARGS) $(TESTNAME_ARG))

corlib-test-linkonly: build
	@echo RUN CORLIB TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	time ($(LINKER_CORLIB_TEST) --dump-dependencies --martin -a $(PROFILE_PATH)/tests/net_4_x_corlib_test.dll -i corlib-nunit.xml -x $(abs_topdir)/class/corlib/LinkerDescriptor/mscorlib.xml)

corlib-test-linkonly2: build
	@echo RUN CORLIB TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	time ($(LINKER_CORLIB_TEST) --dump-dependencies -a $(PROFILE_PATH)/tests/net_4_x_corlib_test.dll -i corlib-nunit.xml -x $(abs_topdir)/class/corlib/LinkerDescriptor/mscorlib.xml)

corlib-test-globalization: build
	$(MAKE) corlib-test MARTIN_LINKER_OPTIONS="globalization" EXTRA_NUNIT_EXCLUDES=",LinkerGlobalization,ManagedCollator"

corlib-test-collator: build
	$(MAKE) corlib-test MARTIN_LINKER_OPTIONS="collator" EXTRA_NUNIT_EXCLUDES=",ManagedCollator"

run-corlib-test:
	(cd $(LINKER_OUTPUT); MONO_PATH=.:$(PROFILE_PATH) $(RUNTIME) $(RUNTIME_FLAGS) --debug $(PROFILE_PATH)/nunit-lite-console.exe net_4_x_corlib_test.dll $(NUNIT_ARGS) $(TESTNAME_ARG))

corlib-xtest: build
	@echo RUN CORLIB XUNIT TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	@echo $(XUNIT_PATH)
	$(LINKER) --martin -i corlib-xunit.xml $(LINKER_ARGS_CORLIB_TEST) --dump-dependencies -a $(PROFILE_PATH)/tests/net_4_x_corlib_xunit-test.dll -d $(XUNIT_PATH) -d $(PROFILE_PATH)/tests -d $(PROFILE_PATH)/Facades -x $(abs_topdir)/class/corlib/LinkerDescriptor/mscorlib_xtest.xml
	#@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(PROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	@cp $(XUNIT_PATH)/*.dll $(LINKER_OUTPUT)
	(cd $(LINKER_OUTPUT); MONO_PATH=.:$(PROFILE_PATH):$(XUNIT_PATH) REMOTE_EXECUTOR="$(PROFILE_PATH)/tests/RemoteExecutorConsoleApp.exe" $(RUNTIME) $(RUNTIME_FLAGS) --debug $(XUNIT_PATH)/xunit.console.exe net_4_x_corlib_xunit-test.dll $(XUNIT_ARGS))

build:
	msbuild /nologo /verbosity:minimal ../Mono.Linker.Optimizer.sln

clean:
	@rm -rf $(LINKER_OUTPUT)
	@rm -rf ../bin ../obj
	@rm -f $(executable_CLEAN_FILES) $(CLEAN_FILES) $(tests_CLEAN_FILES) $(TEST_HELPERS_LIBRARY)

cross-check: all run corlib-test corlib-test-globalization


