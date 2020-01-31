#!/usr/bin/perl -wT
use strict;

my @CONSOLE_DIRS = qw[SimpleDebug SimpleHello SimpleInterpreter SimpleMixed];
my @WEB_DIRS = qw[SimpleWeb SimpleBlazor];

sub update($$$)
{
	my ($dir, $template, $output) = @_;
	print "TEST: $dir - $template - $output\n";

	open (my $OUTPUT, ">", "$dir/.vscode/$output") or die "Could not open output file: $!";
	print $OUTPUT "//\n";
	print $OUTPUT "// AUTO-GENERATED FILE - DO NOT MODIFY!\n";
	print $OUTPUT "// This file has been auto-generated from $template.\n";
	print $OUTPUT "//\n";

	open (my $FILE, $template) or die "Could not open template file '$template' $!";
	while (my $row = <$FILE>) {
		$row =~ s/\@PROJECT\@/$dir/;
		print $OUTPUT $row;
	}
	close $FILE;
	close $OUTPUT;
}

for my $dir (@CONSOLE_DIRS)
{
	update($dir, "tasks-console.json", "tasks.json");
	update($dir, "launch-console.json", "launch.json");
}

for my $dir (@WEB_DIRS)
{
	update($dir,"tasks-web.json", "tasks.json");
	update($dir,"launch-web.json", "launch.json");
}
