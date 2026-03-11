<#
.SYNOPSIS
    Builds a self-contained, single-file DropPost.exe for Windows x64.
    Output: tray\DropPost\publish\DropPost.exe
#>

$project = Join-Path $PSScriptRoot "DropPost.csproj"

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -o "$PSScriptRoot\publish"

$exe = Join-Path $PSScriptRoot "publish\DropPost.exe"
if (Test-Path $exe) {
    $size = [math]::Round((Get-Item $exe).Length / 1MB, 1)
    Write-Host "`nBuild successful: $exe ($size MB)" -ForegroundColor Green
} else {
    Write-Error "Build failed — DropPost.exe not found."
}
