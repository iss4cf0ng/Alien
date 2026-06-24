
szCurrentDir = Dir.pwd
bUnixLike = szCurrentDir.start_with?('/')

print szCurrentDir
print '|'

if bUnixLike
  print '/'
else
  aResult = []
  szOutput = `wmic logicaldisk get name 2>NUL` rescue ""

  if szOutput && !szOutput.strip.empty?
    aResult = szOutput.scan(/[A-Z]:/i)
  else
    szPSCheck = `powershell -Command "Write-Output OK" 2>NUL` rescue ""

    if szPSCheck.strip == 'OK'
      szOutput = `powershell -NoProfile -Command "(Get-PSDrive -PSProvider FileSystem).Name" 2>NUL` rescue ""
      asDrives = szOutput.strip.split(/\r\n|\r|\n/)

      asDrives.each do |drive|
        drive_trimmed = drive.strip
        unless drive_trimmed.empty?
          aResult << "#{drive_trimmed}:"
        end
      end
    end
  end

  print aResult.join(',')
end
