require 'cgi'

is_windows = Gem.win_platform?

ole_available = false
if is_windows
  begin
    require 'win32ole'
    ole_available = true
  rescue LoadError
    ole_available = false
  end
end

def test_com_obj(prog_id, is_windows, ole_available)
  return "NOT APPLICABLE (NON-WINDOWS)" unless is_windows
  return "win32ole MISSING" unless ole_available

  begin
    obj = WIN32OLE.new(prog_id)
    if obj
      obj = nil 
      return "AVAILABLE"
    else
      return "NOT INSTALLED"
    end
  rescue => e
    return "NOT INSTALLED"
  end
end

def get_architecture(is_windows)
  arch = ENV['PROCESSOR_ARCHITECTURE'] || ENV['HOSTTYPE'] || RUBY_PLATFORM || "UNKNOWN"
  if is_windows && Dir.exist?("C:\\Program Files (x86)")
    arch += " (64-bit Windows Environment)"
  end

  arch
end

q = CGI.new

print "<table border='1' cellpadding='5' cellspacing='0' style='font-family: Arial; border-collapse: collapse;'>\n"

print "<tr><th colspan='2'>SYSTEM & RUBY INFO</th></tr>\n"
print "<tr><td>Operating System</td><td>#{RUBY_PLATFORM}</td></tr>\n"
print "<tr><td>Ruby Version</td><td>#{RUBY_VERSION}</td></tr>\n"
print "<tr><td>Architecture</td><td>#{get_architecture(is_windows)}</td></tr>\n"

print "<tr><th colspan='2'>CORE COM COMPONENTS</th></tr>\n"

com_components = [
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
]

com_components.each do |comp|
  status = test_com_obj(comp, is_windows, ole_available)
  print "<tr><td>#{CGI.escapeHTML(comp)}</td><td>#{status}</td></tr>\n"
end

print "<tr><th colspan='2'>ENVIRONMENT VARIABLES</th></tr>\n"

ENV.keys.sort.each do |key|
  next if ENV[key].nil? || ENV[key].empty?

  safe_key = CGI.escapeHTML(key)
  safe_val = CGI.escapeHTML(ENV[key])

  print "<tr><td>#{safe_key}</td><td>#{safe_val}</td></tr>\n"
end

print "</table>\n"