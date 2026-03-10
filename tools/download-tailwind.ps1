$version = "v4.1.3"
$os = if ($IsLinux) { "linux" } elseif ($IsMacOS) { "macos" } else { "windows" }
$arch = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq "Arm64") { "arm64" } else { "x64" }
$ext = if ($os -eq "windows") { ".exe" } else { "" }
$filename = "tailwindcss-$os-$arch$ext"
$url = "https://github.com/tailwindlabs/tailwindcss/releases/download/$version/$filename"
$outPath = Join-Path $PSScriptRoot "tailwindcss$ext"

if (Test-Path $outPath) {
    Write-Host "Tailwind CLI already exists at $outPath"
    exit 0
}

Write-Host "Downloading Tailwind CSS $version..."
Invoke-WebRequest -Uri $url -OutFile $outPath
if ($os -ne "windows") { chmod +x $outPath }
Write-Host "Downloaded to $outPath"
