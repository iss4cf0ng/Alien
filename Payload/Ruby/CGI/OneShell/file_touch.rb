require 'base64'

def main
  filename = Base64.decode64($_POST['z0'].to_s)
  timestamp = Base64.decode64($_POST['z1'].to_s).to_i

  unless File.exist?(filename)
    print "0|File does not exist."
    return
  end

  begin
    target_time = Time.at(timestamp)

    File.utime(target_time, target_time, filename)
    
    print "1|"
  rescue => e
    print "0|Failed to modify the timestamps"
  end
end

main