param($workDir)
if (-not $workDir) {
    $workDir = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$queueDir = "$workDir\.queue"
$outFile = "$workDir\.output.txt"
$pidFile = "$workDir\.pid.txt"

# initialize state tokens cleanly without BOM
[System.IO.File]::WriteAllText($pidFile, "running")
[System.IO.File]::WriteAllText($outFile, "")

# configure a fully asynchronous headless terminal process
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "cmd.exe"
$psi.Arguments = "/q /k @echo off"
$psi.WorkingDirectory = $workDir
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$proc = New-Object System.Diagnostics.Process
$proc.StartInfo = $psi
[void]$proc.Start()

# synchronize initial path inside the shell
$proc.StandardInput.WriteLine("cd /d `"$workDir`"")

# setup Async .NET Stream Buffer, bypasses flaky Register-ObjectEvent
$outBuffer = New-Object char[] 4096
$errBuffer = New-Object char[] 4096

# arm the asynchronous read tasks immediately
$outTask = $proc.StandardOutput.ReadAsync($outBuffer, 0, $outBuffer.Length)
$errTask = $proc.StandardError.ReadAsync($errBuffer, 0, $errBuffer.Length)

# main loop
while ($proc.HasExited -eq $false) {
    if (-not (Test-Path $pidFile) -or (Get-Content $pidFile -ErrorAction SilentlyContinue) -eq "stopped") {
        break
    }

    # check stdout asynchronously
    if ($outTask.IsCompleted) {
        $count = $outTask.Result
        if ($count -gt 0) {
            $text = New-Object string($outBuffer, 0, $count)
            [System.IO.File]::AppendAllText($outFile, $text, [System.Text.Encoding]::UTF8)
        }

        # re-arm the background task for the next chunk of data
        $outTask = $proc.StandardOutput.ReadAsync($outBuffer, 0, $outBuffer.Length)
    }

    if ($errTask.IsCompleted) {
        $count = $errTask.Result
        if ($count -gt 0) {
            $text = New-Object string($errBuffer, 0, $count)
            [System.IO.File]::AppendAllText($outFile, $text, [System.Text.Encoding]::UTF8)
        }
        # Re-arm the background task for errors
        $errTask = $proc.StandardError.ReadAsync($errBuffer, 0, $errBuffer.Length)
    }

    if (Test-Path $queueDir) {
        $files = Get-ChildItem -Path "$queueDir\*.txt" -ErrorAction SilentlyContinue | Sort-Object Name
        foreach ($file in $files) {
            $cmd = ""
            try {
                $stream = New-Object System.IO.FileStream($file.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
                $reader = New-Object System.IO.StreamReader($stream)
                $cmd = $reader.ReadToEnd()
                $reader.Close()
                $stream.Close()
                
                [System.IO.File]::Delete($file.FullName)
            } catch {
                continue
            }
            
            if ($cmd) {
                $proc.StandardInput.WriteLine($cmd)
            }
        }
    }

    Start-Sleep -Milliseconds 20
}

# Clean teardown
try { $proc.Kill() } catch {}
if (Test-Path $pidFile) {
    [System.IO.File]::Delete($pidFile)
}