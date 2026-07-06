require 'base64'
require 'fileutils'

def main
  dir_name = Base64.decode64($_POST['z0'].to_s)

  if Dir.exist?(dir_name)
    print "0|Folder already exists"
    return
  end

  begin
    FileUtils.mkdir_p(dir_name, mode: 0755)
    print "1|"
  rescue => e
    print "0|Failed to create folder."
  end
end

main