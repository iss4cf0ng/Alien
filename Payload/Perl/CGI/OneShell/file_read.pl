use strict;
use warnings;
use MIME::Base64 qw(decode_base64 encode_base64);

my $q = CGI->new;
my $file_path = decode_base64($q->param('z0') // '');

sub main {
    open(my $fh, '<', $file_path) or do {
        print 'ERROR://Unable to open file!';
        return;
    };

    local $/; # slurp mode
    my $content = <$fh>;

    close($fh);

    print encode_base64($content, ''); # no line breaks

    return;
}

main();