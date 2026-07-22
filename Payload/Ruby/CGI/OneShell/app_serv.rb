require 'json'
require 'open3'

IS_WINDOWS = Gem.win_platform?

def command_exists?(cmd)
  check_cmd = IS_WINDOWS ? "where #{cmd} 2>NUL" : "which #{cmd} 2>/dev/null"
  _stdout, _stderr, status = Open3.capture3(check_cmd)
  status.success?
end

def clean_value(v)
  return '' if v.nil?
  str = v.to_s.encode('UTF-8', invalid: :replace, undef: :replace, replace: '')
  str.gsub(/[[:cntrl:]]/, '').strip
end

def flatten_data(item)
  return {} unless item.is_a?(Hash)
  out = {}
  item.each do |k, v|
    if v.is_a?(Hash) || v.is_a?(Array)
      out[k] = JSON.generate(v, ascii_only: false)
    else
      out[k] = clean_value(v)
    end
  end
  out
end

def run_powershell(query)
  cmd = "powershell -NoProfile -ExecutionPolicy Bypass -Command \"[Console]::OutputEncoding = [Text.Encoding]::UTF8; $data = @(#{query}); $data | ConvertTo-Json -Depth 3 -Compress\""
  stdout, _stderr, status = Open3.capture3(cmd)
  return [] if !status.success? || stdout.strip.empty?

  clean_json = stdout.encode('UTF-8', invalid: :replace, undef: :replace, replace: '')
                     .delete("\x00")
                     .gsub(/^\xEF\xBB\xBF/, '')
                     .strip

  s_idx = clean_json.index('[')
  e_idx = clean_json.rindex(']')

  if s_idx.nil? && clean_json.start_with?('{')
    clean_json = "[#{clean_json}]"
    s_idx = 0
    e_idx = clean_json.rindex(']')
  end

  return [] if s_idx.nil? || e_idx.nil? || e_idx <= s_idx

  json_str = clean_json[s_idx..e_idx]
  data = JSON.parse(json_str)
  data.is_a?(Array) ? data : [data]
rescue JSON::ParserError
  []
end

def parse_wmic(wmic_cmd)
  cmd = "wmic #{wmic_cmd} get /format:list 2>NUL"
  stdout, _stderr, status = Open3.capture3(cmd)
  return [] if !status.success? || stdout.strip.empty?

  rows = []
  current = {}

  stdout.encode('UTF-8', invalid: :replace, undef: :replace, replace: '').each_line do |line|
    line = line.gsub(/^\xEF\xBB\xBF/, '').strip
    if line.empty?
      unless current.empty?
        rows << current.sort.to_h
        current = {}
      end
      next
    end

    if line.include?('=')
      k, v = line.split('=', 2)
      k_clean = clean_value(k)
      v_clean = clean_value(v)
      current[k_clean] = v_clean unless k_clean.empty?
    end
  end

  rows << current.sort.to_h unless current.empty?
  rows
rescue
  []
end

def get_windows_data(ps_query, wmic_cmd)
  if command_exists?('powershell')
    data = run_powershell(ps_query)
    return data.map { |row| flatten_data(row) } unless data.empty?
  end

  if command_exists?('wmic')
    return parse_wmic(wmic_cmd)
  end

  []
end

def get_unix_applications
  apps = []

  if command_exists?('dpkg-query')
    stdout, _, status = Open3.capture3("dpkg-query -W -f='${Package}\t${Version}\t${Maintainer}\n' 2>/dev/null")
    if status.success?
      stdout.each_line do |line|
        parts = line.strip.split("\t")
        next if parts.size < 2
        apps << {
          'name'    => clean_value(parts[0]),
          'version' => clean_value(parts[1]),
          'vendor'  => clean_value(parts[2]),
          'source'  => 'dpkg'
        }
      end
    end
  elsif command_exists?('rpm')
    stdout, _, status = Open3.capture3("rpm -qa --qf '%{NAME}\t%{VERSION}-%{RELEASE}\t%{VENDOR}\n' 2>/dev/null")
    if status.success?
      stdout.each_line do |line|
        parts = line.strip.split("\t")
        next if parts.size < 2
        apps << {
          'name'    => clean_value(parts[0]),
          'version' => clean_value(parts[1]),
          'vendor'  => clean_value(parts[2]),
          'source'  => 'rpm'
        }
      end
    end
  end

  if command_exists?('snap')
    stdout, _, status = Open3.capture3("snap list 2>/dev/null")
    if status.success?
      lines = stdout.lines
      lines.shift
      lines.each do |line|
        cols = line.strip.split(/\s+/)
        next if cols.size < 2
        apps << {
          'name'    => clean_value(cols[0]),
          'version' => clean_value(cols[1]),
          'vendor'  => clean_value(cols[4]),
          'source'  => 'snap'
        }
      end
    end
  end

  apps
end

def get_unix_services
  services = []

  if command_exists?('systemctl')
    stdout, _, status = Open3.capture3("systemctl list-units --type=service --all --no-pager --no-legend 2>/dev/null")
    if status.success?
      stdout.each_line do |line|
        cols = line.strip.split(/\s+/, 5)
        next if cols.size < 4
        services << {
          'name'         => clean_value(cols[0].sub(/\.service$/, '')),
          'display_name' => clean_value(cols[4] || cols[0]),
          'status'       => (cols[2] == 'active') ? 'running' : 'stopped',
          'source'       => 'systemd'
        }
      end
    end
  elsif command_exists?('service')
    stdout, _, status = Open3.capture3("service --status-all 2>/dev/null")
    if status.success?
      stdout.each_line do |line|
        if line =~ /\[\s*([\+\-\?])\s*\]\s+(.+)/
          flag = $1
          name = $2.strip
          services << {
            'name'         => clean_value(name),
            'display_name' => clean_value(name),
            'status'       => (flag == '+') ? 'running' : 'stopped',
            'source'       => 'sysvinit'
          }
        end
      end
    end
  elsif command_exists?('launchctl')
    stdout, _, status = Open3.capture3("launchctl list 2>/dev/null")
    if status.success?
      lines = stdout.lines
      lines.shift
      lines.each do |line|
        cols = line.strip.split(/\s+/, 3)
        next if cols.size < 3
        pid = cols[0]
        label = cols[2]
        services << {
          'name'         => clean_value(label),
          'display_name' => clean_value(label),
          'status'       => (pid != '-' && pid =~ /^\d+$/) ? 'running' : 'stopped',
          'source'       => 'launchd'
        }
      end
    end
  end

  services
end

result = {
  success: false,
  system_type: IS_WINDOWS ? 'windows' : 'unix_like',
  os_raw: RUBY_PLATFORM,
  error: '',
  data: {}
}

begin
  if IS_WINDOWS
    ps_apps = "Get-ChildItem 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\', 'HKLM:\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\' -ErrorAction SilentlyContinue | ForEach-Object { try { Get-ItemProperty $_.PSPath -ErrorAction Stop } catch {} } | Where-Object DisplayName | Select-Object @{N='name';E={$_.DisplayName}}, @{N='version';E={$_.DisplayVersion}}, @{N='vendor';E={$_.Publisher}}, @{N='installed';E={$_.InstallDate}}"
    ps_serv = "Get-Service | ForEach-Object { @{ name = $_.Name; display_name = $_.DisplayName; status = if ($_.Status -eq 'Running') { 'running' } else { 'stopped' }; start_type = $_.StartType.ToString() } }"

    result[:data] = {
      applications:  get_windows_data(ps_apps, "product"),
      services:      get_windows_data(ps_serv, "service"),
      user_accounts: get_windows_data("Get-CimInstance Win32_UserAccount", "useraccount"),
      user_profiles: get_windows_data("Get-CimInstance Win32_UserProfile", "path Win32_UserProfile"),
      groups:        get_windows_data("Get-CimInstance Win32_Group", "group")
    }
  else
    result[:data] = {
      applications:  get_unix_applications,
      services:      get_unix_services,
      user_accounts: [],
      user_profiles: [],
      groups:        []
    }
  end

  result[:success] = true
rescue => e
  result[:error] = e.message
end

puts JSON.pretty_generate(result)