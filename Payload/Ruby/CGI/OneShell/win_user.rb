require 'json'
require 'open3'

def has_powershell?
  _stdout, _stderr, status = Open3.capture3("powershell -Command \"Get-Host\"")
  status.success?
rescue
  false
end

def clean_value(v)
  v.to_s.gsub(/[[:cntrl:]]/, '').strip
end

def flatten_data(item)
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
  cmd = "powershell -NoProfile -Command \"#{query} | ConvertTo-Json -Depth 3 -Compress\""
  stdout, _stderr, status = Open3.capture3(cmd)
  
  return [] if !status.success? || stdout.strip.empty?

  clean_json = stdout.force_encoding('UTF-8').gsub(/^\xEF\xBB\xBF/, '')
  data = JSON.parse(clean_json)

  data.is_a?(Array) ? data : [data]
rescue JSON::ParserError
  []
end

def parse_wmic(wmic_class)
  cmd = "wmic path #{wmic_class} get /format:list"
  stdout, _stderr, status = Open3.capture3(cmd)
  
  return [] if !status.success? || stdout.strip.empty?

  rows = []
  current = {}

  stdout.force_encoding('UTF-8').each_line do |line|
    line = line.gsub(/^\xEF\xBB\xBF/, '').strip
    next if line.empty?

    if line.include?('=')
      k, v = line.split('=', 2)
      k = clean_value(k)
      v = clean_value(v)
      current[k] = v unless k.empty?
    end
  rescue
    next
  end

  rows << current.sort.to_h unless current.empty?
  rows
end

def get_data(ps_query, wmic_class)
  if has_powershell?
    data = run_powershell(ps_query)
    unless data.empty?
      return data.map { |row| flatten_data(row) }
    end
  end
  parse_wmic(wmic_class)
end

result = { success: false, error: '', data: nil }

begin
  result[:data] = {
    user_accounts: get_data("Get-CimInstance Win32_UserAccount", "Win32_UserAccount"),
    user_profiles: get_data("Get-CimInstance Win32_UserProfile", "Win32_UserProfile"),
    groups:        get_data("Get-CimInstance Win32_Group", "Win32_Group"),
    group_users:   get_data("Get-CimInstance Win32_GroupUser", "Win32_GroupUser"),
    logged_on:     get_data("Get-CimInstance Win32_LoggedOnUser", "Win32_LoggedOnUser"),
    logon_session: get_data("Get-CimInstance Win32_LogonSession", "Win32_LogonSession")
  }
  result[:success] = true
rescue => e
  result[:error] = e.message
end

puts JSON.pretty_generate(result)