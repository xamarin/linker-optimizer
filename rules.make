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
include $(ROOTDIR)/mono.make
endif

ifdef STANDALONE_MAKE
include $(ROOTDIR)/standalone.make
endif
