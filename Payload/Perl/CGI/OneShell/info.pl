use strict;
use warnings;
use CGI;
use Config;

my $q = CGI->new;

my $is_windows = ($^O eq 'MSWin32');

# Safely load Win32::OLE only if available
my $ole_available = 0;
if ($is_windows) {
    eval {
        require Win32::OLE;
        Win32::OLE->import();
        $ole_available = 1;
    };
}

sub test_com_obj {
    my ($prog_id) = @_;

    return "NOT APPLICABLE (NON-WINDOWS)" if !$is_windows;
    return "Win32::OLE MISSING" if !$ole_available;

    local $Win32::OLE::Warn = 0;

    my $obj;
    eval {
        $obj = Win32::OLE->new($prog_id);
    };

    if ($obj) {
        return "AVAILABLE";
    } else {
        return "NOT INSTALLED";
    }
}

sub get_architecture {
    my $arch = $ENV{PROCESSOR_ARCHITECTURE} || $ENV{HOSTTYPE} || $Config{archname} || "UNKNOWN";

    if ($is_windows && -d "C:\\Program Files (x86)") {
        $arch .= " (64-bit Windows Environment)";
    }

    return $arch;
}

print <<'HTML';
<table border='1' cellpadding='5' cellspacing='0' style='font-family: Arial; border-collapse: collapse;'>
HTML

print "<tr><th colspan='2'>SYSTEM & PERL INFO</th></tr>\n";
print "<tr><td>Operating System</td><td>$^O</td></tr>\n";
print "<tr><td>Perl Version</td><td>$]</td></tr>\n";
print "<tr><td>Architecture</td><td>" . get_architecture() . "</td></tr>\n";

print "<tr><th colspan='2'>CORE COM COMPONENTS</th></tr>\n";

my @com_components = (
    "Scripting.FileSystemObject",
    "Scripting.Dictionary",
    "ADODB.Connection",
    "ADODB.Recordset",
    "ADODB.Stream",
    "MSXML2.DOMDocument.6.0",
    "MSXML2.DOMDocument.3.0",
    "MSXML2.ServerXMLHTTP.6.0",
    "Microsoft.XMLHTTP",
    "WScript.Shell",
    "Shell.Application",
    "CDO.Message"
);

for my $comp (@com_components) {
    my $status = test_com_obj($comp);
    print "<tr><td>$comp</td><td>$status</td></tr>\n";
}

print "<tr><th colspan='2'>ENVIRONMENT VARIABLES</th></tr>\n";

for my $key (sort keys %ENV) {
    next if !defined $ENV{$key} || $ENV{$key} eq '';

    my $safe_key = $q->escapeHTML($key);
    my $safe_val = $q->escapeHTML($ENV{$key});

    print "<tr><td>$safe_key</td><td>$safe_val</td></tr>\n";
}

print "</table>\n";