#!/usr/bin/perl -w
use strict;
use File::Basename;

my @CONSOLE_DIRS = qw[Samples/SimpleDebug Samples/SimpleHello Samples/SimpleHelloRelease Samples/SimpleInterpreter Samples/SimpleMixed Samples/DontLink NetCore/SimpleHello];
my @WEB_DIRS = qw[Samples/SimpleWeb Samples/SimpleWeb2 Samples/WebBindings NetCore/SimpleWebNet5];
my @TEST_DIRS = qw[TestSuite/SimpleTest TestSuite/WorkingTests TestSuite/BlazorTests TestSuite/NetCoreTests];
my @BLAZOR_DIRS = qw[Samples/SimpleBlazor];

my @WEB_TEST_SAMPLE_DIRS = qw[TestSuite/SimpleTest/WebSample TestSuite/WorkingTests/WebSample TestSuite/NetCoreTests/WebSample];
my @BLAZOR_TEST_SAMPLE_DIRS = qw[TestSuite/BlazorTests/BlazorSample];

my @ALL_PROJECTS = ();
my @ALL_TEST_PROJECTS = ();
my @ALL_TEST_SAMPLE_PROJECTS = ();

sub update($$$;$$)
{
	my ($dir, $template, $output, $testsample, $bootstrap) = @_;

	my @projects = glob "$dir/*.csproj";
	print "PROJECTS: $#projects - |$projects[0]|\n";
	die "Could not find project file in $dir" unless $#projects >= 0;
	die "More than one project file in $dir" unless $#projects == 0;

	my $projectPath = $projects[0];

	my ($project, $directories, $suffix) = fileparse($projectPath, qw[.csproj]);
	print "PROJECT PATH: |$projectPath| - |$project|$directories|$suffix|\n";

	push (@ALL_PROJECTS, $projectPath) if $bootstrap;

	$directories =~ s,/$,,;

	$dir = $directories if ($suffix eq '.csproj');

	my @dirs = split ('/', $dir);
	my $up = $#dirs + 2;

	print "TEST: $dir - $template - $output - $#dirs - $project\n";

	my $packagesFolder = $testsample ? ".test-packages" : ".packages";

	my $root = "\${workspaceFolder}" . "/.." x $up;
	my $packagesDir = "\${workspaceFolder}" . "/.." x ($up - 1) . "/${packagesFolder}";
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

	return $projectPath;
}

sub createBootstrap($$$)
{
	my ($dir, $template, $output) = @_;

	my @dirs = split ('/', $dir);
	my $up = $#dirs + 2;

	print "TEST: $dir - $template - $output - $#dirs\n";

	my $packagesFolder = ".packages";

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

	my %buildLabels = ();

	my $allProjects = "";
	my $allDependencies = "";
	for my $project (@ALL_PROJECTS) {
		my $name = $project;
		$name =~ s,^(?:TestSuite/)(.*?)\.csproj$,\1,;
		$name =~ s,[/\.],_,g;
		my $label = "z-build-$name";
		$buildLabels{$project} = $label;
		$allProjects .= qq[
        {
            "label": "$label",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "/nologo",
                "$root/Tests/$project",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:Summary"
            ],
            "problemMatcher": "\$msCompile",
            "dependsOrder": "sequence"
        },
		];
		$allDependencies .= "                \"$label\",\n";
	}

	my $buildTestsDependsOn = "";
	my $buildTestSamplesDependsOn = "";
	for my $project (@ALL_TEST_SAMPLE_PROJECTS) {
		$buildTestSamplesDependsOn .= "                \"$buildLabels{$project}\",\n";
	}
	for my $project (@ALL_TEST_PROJECTS) {
		$buildTestsDependsOn .= "                \"$buildLabels{$project}\",\n";
	}
	$allDependencies =~ s/,\n$//s;
	$buildTestSamplesDependsOn =~ s/,\s*$//s;
	$buildTestsDependsOn =~ s/,\s*$//s;

	open (my $FILE, $template) or die "Could not open template file '$template' $!";
	while (my $row = <$FILE>) {
		$row =~ s/\@ROOT\@/$root/g;
		$row =~ s/\@PACKAGES_DIR\@/$packagesDir/g;
		$row =~ s/\@ALL_PROJECTS\@/$allProjects/g;
		$row =~ s/\@ALL_DEPENDENCIES\@/$allDependencies/g;
		$row =~ s/\@BUILD_TESTS_DEPENDS_ON\@/$buildTestsDependsOn/g;
		$row =~ s/\@BUILD_TEST_SAMPLES_DEPENDS_ON\@/$buildTestSamplesDependsOn/g;

		print $OUTPUT $row;
	}
	close $FILE;
	close $OUTPUT;
}

for my $dir (@WEB_TEST_SAMPLE_DIRS)
{
	my $project = update($dir, "tasks-web.json", "tasks.json", 1, 1);
	update($dir, "launch-web.json", "launch.json", 1);
	push (@ALL_TEST_SAMPLE_PROJECTS, $project);

}

for my $dir (@BLAZOR_TEST_SAMPLE_DIRS)
{
	my $project = update($dir, "tasks-web.json", "tasks.json", 1, 1);
	update($dir, "launch-blazor.json", "launch.json", 1);
	push (@ALL_TEST_SAMPLE_PROJECTS, $project);
}

for my $dir (@TEST_DIRS)
{
	my $project = update($dir, "tasks-test.json", "tasks.json", 1, 1);
	update($dir, "launch-test.json", "launch.json");
	push (@ALL_TEST_PROJECTS, $project);
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

for my $csproj (@ALL_PROJECTS) {
	print "ALL PROJECTS: $csproj\n";
}

createBootstrap ("Bootstrap", "tasks-bootstrap.json", "tasks.json");
