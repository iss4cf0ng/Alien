use strict;
use warnings;
use Cwd qw(getcwd);

my $szCurrentDir = getcwd();

my $bUnixLike = ($^O ne 'MSWin32' && $^O ne 'Windows_NT') ? 1 : 0;

print $szCurrentDir;
print '|';

if ($bUnixLike) {
    print '/';
}
else {
    my @aResult = ();
    my $szOutput = `wmic logicaldisk get name 2>NUL`;

    if (defined $szOutput && $szOutput =~ /\S/) {
        while ($szOutput =~ /([A-Za-z]:)/g) {
            push @aResult, $1;
        }
    }
    else {
        my $szPSCheck = `powershell -Command "Write-Output OK" 2>NUL`;
        $szPSCheck //= '';
        $szPSCheck =~ s/^\s+|\s+$//g;

        if ($szPSCheck eq 'OK') {
            $szOutput = `powershell -NoProfile -Command "(Get-PSDrive -PSProvider FileSystem).Name" 2>NUL`;
            $szOutput //= '';

            my @asDrives = split(/\r?\n/, $szOutput);

            foreach my $drive (@asDrives) {
                $drive =~ s/^\s+|\s+$//g;
                if ($drive ne '') {
                    push @aResult, $drive . ':';
                }
            }
        }
    }

    print join(',', @aResult);
}