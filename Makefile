thisdir = tools/linker/Martin
SUBDIRS =
include ../../../build/rules.make

PROGRAM = monolinker.exe

LIB_REFS = System System.Core System.Xml Mono.Cecil

include ../../../build/executable.make
