[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $Root=([System.IO.Path]::GetFullPath("$PSScriptRoot\..\"))
)


Write-HostFormatted "Building Xpand.XAF.ModelEditor.WinDesktop" -Section
Push-Location "$Root\Tools\Xpand.XAF.ModelEditor\"
dotnet publish -p:PublishProfile="Folderprofile.pubxml" ".\Xpand.XAF.ModelEditor.WinDesktop.csproj" 

Set-Location ".\bin\Release\net5.0-windows7.0\publish"
$zip=[System.IO.Path]::GetFullPath("$(Get-Location)\..\Xpand.XAF.ModelEditor.WinDesktop.zip")
Compress-Files -zipfileName $zip -Force
if (!(Test-Path "$root\bin\zip")){
    New-Item "$root\bin\zip" -ItemType Directory -Verbose
}

Copy-Item $zip "$root\bin\zip\Xpand.XAF.ModelEditor.WinDesktop.zip" -Verbose

if (!(Test-AzDevops)){
    Write-HostFormatted "Building Xpand.XAF.ModelEditor.Win" -Section
    Set-Location "$Root\tools\Xpand.XAF.ModelEditor\IDE\ModelEditor.Win\Xpand.XAF.ModelEditor.Win"
    dotnet publish -p:PublishProfile="Folderprofile.pubxml" ".\Xpand.XAF.ModelEditor.Win.csproj"
    Set-Location "$(Get-Location)\bin\Release\net5.0-windows\publish"
    Get-ChildItem|Copy-Item -Destination "$env:APPDATA\Xpand.XAF.ModelEditor.Win\Xpand.XAF.ModelEditor.Win" -Force -Recurse
    $zip="$(Get-Location)\..\Xpand.XAF.ModelEditor.Win.zip"
    Compress-Files -zipfileName $zip -Force 
    $version=[System.Diagnostics.FileVersionInfo]::GetVersionInfo("$(Get-Location)\Xpand.XAF.ModelEditor.Win.exe").FileVersion
    Move-Item $zip "$([System.IO.Path]::GetDirectoryName($zip))\$([System.IO.Path]::GetFileNameWithoutExtension($zip)).$version.zip" -Force

    $proj=Get-Content "$root\tools\Xpand.XAF.ModelEditor\IDE\XVSIX\XVSIX.csproj" -Raw
    $regex = [regex] 'Xpand\.XAF\.ModelEditor\.Win\..*\.zip'
    $allmatches = $regex.Matches($proj);
    $currentValue=$allmatches[0].Value
    $newValue="Xpand.XAF.ModelEditor.Win.$version.zip"
    Set-Content "$root\tools\Xpand.XAF.ModelEditor\IDE\XVSIX\XVSIX.csproj" $proj.Replace($currentValue,$newValue)
    
    $proj=Get-Content "$root\tools\Xpand.XAF.ModelEditor\IDE\Rider\src\dotnet\ReSharperPlugin.Xpand\ReSharperPlugin.Xpand.Rider.csproj" -Raw
    Set-Content       "$root\tools\Xpand.XAF.ModelEditor\IDE\Rider\src\dotnet\ReSharperPlugin.Xpand\ReSharperPlugin.Xpand.Rider.csproj" $proj.Replace($currentValue,$newValue)

    $proj=Get-Content "$root\tools\Xpand.XAF.ModelEditor\IDE\Rider\src\dotnet\ReSharperPlugin.Xpand\ReSharperPlugin.Xpand.csproj" -Raw
    Set-Content       "$root\tools\Xpand.XAF.ModelEditor\IDE\Rider\src\dotnet\ReSharperPlugin.Xpand\ReSharperPlugin.Xpand.csproj" $proj.Replace($currentValue,$newValue)

    Write-HostFormatted "Building Rider" -Section
    Set-Location "$Root\tools\Xpand.XAF.ModelEditor\IDE\Rider"
    Start-Build
    Write-HostFormatted "Building XVSIX" -Section
    Set-Location "$Root\tools\Xpand.XAF.ModelEditor\IDE\XVSIX"
    Start-Build
}

Pop-Location