# Builds CharManJur for testing. Run from this folder:
#   powershell -ExecutionPolicy Bypass -File .\Build.ps1 android                       builds an APK
#   powershell -ExecutionPolicy Bypass -File .\Build.ps1 android -Install              builds it and installs it on a USB-connected phone
#   powershell -ExecutionPolicy Bypass -File .\Build.ps1 android -InstallDependencies  first time only: downloads the Android SDK, then builds
#   powershell -ExecutionPolicy Bypass -File .\Build.ps1 windows                       your Ready-To-Run build
#   powershell -ExecutionPolicy Bypass -File .\Build.ps1 both
# If the script can't find your Android SDK, point it there with -SdkDir "C:\path\to\android-sdk".

param(
    [ValidateSet('android', 'windows', 'both')]
    [string]$Target = 'android',
    [switch]$Install,                      # install the APK with adb after building
    [switch]$InstallDependencies,          # download the Android SDK (and what the build needs) into -SdkDir
    [string]$SdkDir = '',                  # the Android SDK folder (found automatically when empty)
    [string]$JdkDir = '',                  # the Java JDK folder (found automatically when empty)
    [string]$Abi = 'android-arm64'         # most phones and tablets; android-x64 for an Intel emulator image
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'CharManJur.csproj'
$builds  = Join-Path $PSScriptRoot '..\Builds'

function Build-Windows {
    Write-Host "`n=== Windows (Ready To Run) ===" -ForegroundColor Cyan
    dotnet publish $project -f net10.0-windows10.0.19041.0 -c Release `
        -p:RuntimeIdentifierOverride=win10-x64 -p:UseMonoRuntime=false `
        -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -p:SelfContained=true
    if ($LASTEXITCODE -ne 0) { throw 'The Windows build failed. Send me the first error lines above.' }

    $folder = Get-ChildItem (Join-Path $PSScriptRoot 'bin\Release\net10.0-windows10.0.19041.0') -Directory -Recurse -Filter publish |
              Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host "Done: $($folder.FullName)" -ForegroundColor Green
}

# A folder counts as an Android SDK when it holds platform-tools, build-tools or platforms.
function Test-Sdk([string]$path) {
    if (-not $path -or -not (Test-Path $path)) { return $false }
    foreach ($sub in 'platform-tools', 'build-tools', 'platforms') {
        if (Test-Path (Join-Path $path $sub)) { return $true }
    }
    return $false
}

function Find-Sdk {
    $candidates = @($SdkDir, $env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, $env:ANDROID_SDK,
                    "${env:ProgramFiles(x86)}\Android\android-sdk",
                    "$env:ProgramFiles\Android\android-sdk",
                    "$env:LOCALAPPDATA\Android\Sdk",
                    'C:\Android\sdk')
    foreach ($c in $candidates) { if (Test-Sdk $c) { return $c } }
    return $null
}

function Find-Jdk {
    $candidates = @($JdkDir, $env:JAVA_HOME)
    foreach ($pattern in "$env:ProgramFiles\Microsoft\jdk-*", "$env:ProgramFiles\Eclipse Adoptium\jdk-*",
                         "$env:ProgramFiles\Android\jdk\*", "$env:ProgramFiles\Java\jdk-*") {
        $candidates += (Get-ChildItem $pattern -Directory -ErrorAction SilentlyContinue |
                        Sort-Object Name -Descending | ForEach-Object FullName)
    }
    foreach ($c in $candidates) {
        if ($c -and (Test-Path (Join-Path $c 'bin\java.exe'))) { return $c }
    }
    return $null
}

function Find-Adb([string]$sdk) {
    if ($sdk) {
        $adb = Join-Path $sdk 'platform-tools\adb.exe'
        if (Test-Path $adb) { return $adb }
    }
    $onPath = Get-Command adb -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }
    return $null
}

function Build-Android {
    Write-Host "`n=== Android APK ($Abi) ===" -ForegroundColor Cyan

    $sdk = Find-Sdk
    $jdk = Find-Jdk
    $props = @()

    if ($InstallDependencies) {
        # First-time setup: downloads the SDK pieces the build needs. Needs a JDK (Visual Studio installs one).
        $target = if ($SdkDir) { $SdkDir } elseif ($sdk) { $sdk } else { "$env:LOCALAPPDATA\Android\Sdk" }
        Write-Host "Installing the Android SDK into: $target (this downloads a few hundred MB)"
        $setup = @('build', $project, '-t:InstallAndroidDependencies', '-f', 'net10.0-android',
                   "-p:AndroidSdkDirectory=$target", '-p:AcceptAndroidSDKLicenses=True')
        if ($jdk) { $setup += "-p:JavaSdkDirectory=$jdk" }
        & dotnet @setup
        if ($LASTEXITCODE -ne 0) { throw 'The Android SDK install failed. Send me the first error lines above.' }
        $sdk = $target
    }

    if (-not $sdk) {
        throw ("No Android SDK was found.`n" +
               "  - If Visual Studio has one: Tools > Android > Android SDK Manager shows its folder. Run again with -SdkDir `"<that folder>`".`n" +
               "  - If not: run again with -InstallDependencies to download one.")
    }

    Write-Host "Android SDK: $sdk"
    $props += "-p:AndroidSdkDirectory=$sdk"
    if ($jdk) { Write-Host "Java JDK:    $jdk"; $props += "-p:JavaSdkDirectory=$jdk" }
    else { Write-Host 'Java JDK:    (not found; using whatever the build finds on its own)' }

    # Trimming and AOT are off so the app behaves like the Windows build while you test.
    dotnet publish $project -f net10.0-android -c Release -r $Abi `
        -p:AndroidPackageFormat=apk -p:PublishTrimmed=false -p:RunAOTCompilation=false @props
    if ($LASTEXITCODE -ne 0) { throw 'The Android build failed. Send me the first error lines above.' }

    $found = Get-ChildItem (Join-Path $PSScriptRoot 'bin\Release\net10.0-android') -Filter *.apk -Recurse |
             Sort-Object LastWriteTime -Descending
    $apk = $found | Where-Object Name -like '*Signed*' | Select-Object -First 1
    if (-not $apk) { $apk = $found | Select-Object -First 1 }
    if (-not $apk) { throw 'The build finished, but no .apk was found under bin\Release\net10.0-android.' }

    $folder = Join-Path $builds 'Android'
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $dest = Join-Path $folder 'CharManJur.apk'
    Copy-Item $apk.FullName $dest -Force
    Write-Host "Done: $dest ($([math]::Round((Get-Item $dest).Length / 1MB, 1)) MB)" -ForegroundColor Green

    if (-not $Install) {
        Write-Host 'Copy that file to the phone and open it, or run again with -Install while the phone is plugged in.'
        return
    }

    $adb = Find-Adb $sdk
    if (-not $adb) { Write-Host 'adb was not found. Install the Android SDK platform-tools, or copy the APK to the phone by hand.' -ForegroundColor Yellow; return }

    & $adb devices
    & $adb install -r $dest
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'The install failed. If it says INSTALL_FAILED_UPDATE_INCOMPATIBLE, uninstall the old copy from the phone first.' -ForegroundColor Yellow
    }
}

switch ($Target) {
    'windows' { Build-Windows }
    'android' { Build-Android }
    'both'    { Build-Windows; Build-Android }
}