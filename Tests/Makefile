thisdir = tools/linker/Martin/Tests

makefile_dir := $(shell dirname $(realpath $(lastword $(MAKEFILE_LIST))))
ROOTDIR := $(abspath $(makefile_dir)/..)

include $(ROOTDIR)/build/rules.make

include $(ROOTDIR)/build/tests.make

ifdef TESTNAME
TESTNAME_ARG = -test=$(TESTNAME)
endif

OPTIMIZER_NUNIT_XML_OPTIONS := \
	--optimizer-xml corlib-nunit.xml \
	--optimizer-xml Corlib/corlib-api.xml \
	--optimizer-xml Corlib/corlib-asserts.xml

corlib-test: build
	@echo RUN CORLIB TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	$(LINKER) --optimizer $(PROFILE_PATH)/tests/net_4_x_corlib_test.dll $(OPTIMIZER_NUNIT_XML_OPTIONS) $(LINKER_ARGS_CORLIB_TEST) --dump-dependencies -x $(MONO_ROOT)/mcs/class/corlib/LinkerDescriptor/mscorlib.xml
	#@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(PROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	(cd $(LINKER_OUTPUT); MONO_PATH=.:$(PROFILE_PATH) $(RUNTIME) $(RUNTIME_FLAGS) --debug $(PROFILE_PATH)/nunit-lite-console.exe net_4_x_corlib_test.dll $(NUNIT_ARGS) $(TESTNAME_ARG))

corlib-test-profiler: build-release
	@echo RUN CORLIB TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	time (mono --debug $(PROFILER_FLAGS) $(LINKER_RELEASE_EXE) --optimizer $(PROFILE_PATH)/tests/net_4_x_corlib_test.dll --optimizer-xml corlib-profile.xml $(LINKER_ARGS_CORLIB_TEST) --dump-dependencies -x $(MONO_ROOT)/mcs/class/corlib/LinkerDescriptor/mscorlib.xml)

corlib-test-profiler-disabled: build-release
	@echo RUN CORLIB TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	time (mono --debug $(PROFILER_FLAGS) $(LINKER_RELEASE_EXE) $(LINKER_ARGS_CORLIB_TEST) --dump-dependencies -a $(PROFILE_PATH)/tests/net_4_x_corlib_test.dll -x $(MONO_ROOT)/mcs/class/corlib/LinkerDescriptor/mscorlib.xml)

corlib-test-globalization: build
	$(MAKE) corlib-test LINKER_OPTIMIZER_OPTIONS="globalization" EXTRA_NUNIT_EXCLUDES=",LinkerGlobalization,ManagedCollator"

corlib-test-collator: build
	$(MAKE) corlib-test LINKER_OPTIMIZER_OPTIONS="collator" EXTRA_NUNIT_EXCLUDES=",ManagedCollator"

corlib-test-reflection-emit: build
	$(MAKE) corlib-test LINKER_OPTIMIZER_OPTIONS="reflection-emit" EXTRA_NUNIT_EXCLUDES=",ReflectionEmit"

corlib-test-remove-all: build
	$(MAKE) corlib-test LINKER_OPTIMIZER_OPTIONS="disable-all" EXTRA_NUNIT_EXCLUDES=",ReflectionEmit,LinkerGlobalization,ManagedCollator"

run-corlib-test:
	(cd $(LINKER_OUTPUT); MONO_PATH=.:$(PROFILE_PATH) $(RUNTIME) $(RUNTIME_FLAGS) --debug $(PROFILE_PATH)/nunit-lite-console.exe net_4_x_corlib_test.dll $(NUNIT_ARGS) $(TESTNAME_ARG))

corlib-xtest: build
	@echo RUN CORLIB XUNIT TEST $@
	@rm -rf $(LINKER_OUTPUT)
	@mkdir $(LINKER_OUTPUT)
	@echo $(XUNIT_PATH)
	$(LINKER) --optimizer -a $(PROFILE_PATH)/tests/net_4_x_corlib_xunit-test.dll -i corlib-xunit.xml $(LINKER_ARGS_CORLIB_TEST) --dump-dependencies -d $(XUNIT_PATH) -d $(PROFILE_PATH)/tests -d $(PROFILE_PATH)/Facades -x $(ROOTDIR)/class/corlib/LinkerDescriptor/mscorlib_xtest.xml
	#@gzip -d $(LINKER_OUTPUT)/linker-dependencies.xml.gz
	MONO_PATH=$(PROFILE_PATH) monodis $(LINKER_OUTPUT)/mscorlib.dll > $(LINKER_OUTPUT)/mscorlib.il
	MONO_PATH=$(PROFILE_PATH) monodis --typedef $(LINKER_OUTPUT)/mscorlib.dll | sed -e 's,^[0-9]*: ,,g' -e 's,(.*,,g' > $(LINKER_OUTPUT)/mscorlib.txt
	@cp $(XUNIT_PATH)/*.dll $(LINKER_OUTPUT)
	(cd $(LINKER_OUTPUT); MONO_PATH=.:$(PROFILE_PATH):$(XUNIT_PATH) REMOTE_EXECUTOR="$(PROFILE_PATH)/tests/RemoteExecutorConsoleApp.exe" $(RUNTIME) $(RUNTIME_FLAGS) --debug $(XUNIT_PATH)/xunit.console.exe net_4_x_corlib_xunit-test.dll $(XUNIT_ARGS))

cross-check: all run corlib-test corlib-test-globalization
