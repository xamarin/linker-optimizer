#!/usr/bin/perl -w
use strict;
use File::Basename;

my @CONSOLE_DIRS = qw[Samples/SimpleDebug Samples/SimpleHello Samples/SimpleInterpreter Samples/SimpleMixed Samples/DontLink];
my @NETCORE_DIRS = qw[Samples/SimpleNetCore];
my @WEB_DIRS = qw[Samples/SimpleWeb Samples/SimpleWeb2 Samples/WebBindings];
my @TEST_DIRS = qw[Tests/SimpleTest Tests/WorkingTests Tests/BlazorTests];
my @BLAZOR_DIRS = qw[Samples/SimpleBlazor];

my @WEB_TEST_DIRS = qw[Tests/SimpleTest/WebSample Tests/WorkingTests/WebSample];
my @BLAZOR_TEST_DIRS = qw[Tests/BlazorTests/BlazorSample];


sub update($$$;$)
{
	my ($dir, $template, $output, $testsample) = @_;

	my @projects = glob "$dir/*.csproj";
	print "PROJECTS: $#projects - |$projects[0]|\n";
	die "Could not find project file in $dir" unless $#projects >= 0;
	die "More than one project file in $dir" unless $#projects == 0;

	my $projectPath = $projects[0];

	my ($project, $directories, $suffix) = fileparse($projectPath, qw[.csproj]);
	print "PROJECT PATH: |$projectPath| - |$project|$directories|$suffix|\n";

	$directories =~ s,/$,,;

	$dir = $directories if ($suffix eq '.csproj');

	my @dirs = split ('/', $dir);
	my $up = $#dirs + 3;

	print "TEST: $dir - $template - $output - $#dirs - $project\n";

	my $packagesFolder = $testsample ? ".test-packages" : ".packages";

	my $root = "\${workspaceFolder}" . "/.." x $up;
	my $packagesDir = "\${workspaceFolder}" . "/.." x ($up - 2) . "/${packagesFolder}";
	print "ROOT: |$root|$packagesDir|\n";

	my $vscodeDir = "$dir/.vscode";
	print "VSCODE DIR: |$dir|$vscodeDir|$output|\n\n";

	mkdir ($vscodeDir, 0777) unless -d $vscodeDir;

	open (my $OUTPUT, ">", "$vscodeDir/$output") or die "Could not open output file $vscodeDir/$output: $!";
	print $OUTPUT "//\n";
	print $OUTPUT "// AUTO-GENERATED FILE - DO NOT MODIFY!\n";
	print $OUTPUT "// This file has been auto-generated from $template.\n";
	print $OUTPUT "//\n";

	open (my $FILE, $template) or die "Could not open template file '$template' $!";
	while (my $row = <$FILE>) {
		$row =~ s/\@PROJECT\@/$project/g;
		$row =~ s/\@ROOT\@/$root/g;
		$row =~ s/\@PACKAGES_DIR\@/$packagesDir/g;
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
	update($dir, "tasks-web.json", "tasks.json");
	update($dir, "launch-web.json", "launch.json");
}

for my $dir (@BLAZOR_DIRS)
{
	update($dir, "tasks-web.json", "tasks.json");
	update($dir, "launch-blazor.json", "launch.json");
}

for my $dir (@TEST_DIRS)
{
	update($dir, "tasks-test.json", "tasks.json");
	update($dir, "launch-test.json", "launch.json");
}

for my $dir (@WEB_TEST_DIRS)
{
	update($dir, "tasks-web.json", "tasks.json", 1);
	update($dir, "launch-web.json", "launch.json", 1);
}

for my $dir (@BLAZOR_TEST_DIRS)
{
	update($dir, "tasks-web.json", "tasks.json", 1);
	update($dir, "launch-blazor.json", "launch.json", 1);
}

for my $dir (@NETCORE_DIRS)
{
	update($dir, "tasks-netcore.json", "tasks.json");
	update($dir, "launch-netcore.json", "launch.json");
}
