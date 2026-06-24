# $_POST: global variable used by the Ruby loader

require 'base64'

print Base64.decode64($_POST['z0']);