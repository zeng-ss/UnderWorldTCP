# ============================================================
# ARPGDemo 一键：生成Proto → 复制CS → 编译Network.dll → 部署到Unity
# ============================================================
param(
    [string]$MsBuildPath = $null
)

$ErrorActionPreference = "Stop"
$protoDir   = "D:\BaiduNetdiskDownload\课程资料\服务端\proto"
$protoOut   = "$protoDir\out"
$protoSrc   = "$protoDir\proto"
$protoc     = "$protoDir\bin\protoc.exe"
$networkDir = "D:\BaiduNetdiskDownload\课程资料\ARPGDemo_Server\Network"
$networkCsproj = "$networkDir\Network.csproj"
$networkProtoDir = "$networkDir\Proto"
$networkBin = "$networkDir\bin\Debug"
$unityNetDir = "D:\Unity\UnderWordYoo\Assets\Plugins\Net"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ARPGDemo Proto生成 + Network编译 + 部署" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ====== Step 1: 生成Proto C# ======
Write-Host "[1/4] 生成 Proto C# 文件..." -ForegroundColor Yellow
if (-not (Test-Path $protoc)) {
    Write-Error "protoc.exe 未找到: $protoc"
}
if (-not (Test-Path $protoOut)) {
    New-Item -ItemType Directory -Path $protoOut -Force | Out-Null
}

$protoFiles = @("ResultEntity.proto", "RequestEntity.proto")
foreach ($pf in $protoFiles) {
    $fullPath = "$protoSrc\$pf"
    Write-Host "  生成: $pf"
    & $protoc -I="$protoSrc" --csharp_out="$protoOut" $fullPath
    if ($LASTEXITCODE -ne 0) { Write-Error "protoc 失败: $pf" }
}
Write-Host "  Proto 生成完成。" -ForegroundColor Green

# ====== Step 2: 复制到 Network/Proto ======
Write-Host "[2/4] 复制 CS 文件到 Network\Proto ..." -ForegroundColor Yellow
$csFiles = @("ResultEntity.cs", "RequestEntity.cs")
foreach ($f in $csFiles) {
    $src = "$protoOut\$f"
    $dst = "$networkProtoDir\$f"
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
        Write-Host "  已复制: $f → $dst"
    } else {
        Write-Warning "  跳过: $src 不存在"
    }
}
Write-Host "  复制完成。" -ForegroundColor Green

# ====== Step 3: 编译 Network.csproj ======
Write-Host "[3/4] 编译 Network.csproj ..." -ForegroundColor Yellow

# 查找 MSBuild
if ($MsBuildPath -and (Test-Path $MsBuildPath)) {
    $msbuild = $MsBuildPath
} else {
    # 自动查找 VS2022 / VS2019 的 MSBuild
    $candidates = @(
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    )
    $msbuild = $null
    foreach ($c in $candidates) {
        if (Test-Path $c) { $msbuild = $c; break }
    }
}

if (-not $msbuild) {
    # 最后尝试 vswhere
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null
        if ($vsPath) {
            $msbuild = Get-ChildItem "$vsPath\MSBuild\*\Bin\MSBuild.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
        }
    }
}

if (-not $msbuild) {
    Write-Error "未找到 MSBuild.exe，请用 -MsBuildPath 参数指定路径"
}

Write-Host "  使用 MSBuild: $msbuild"
$buildResult = & $msbuild $networkCsproj /t:Build /p:Configuration=Debug /v:minimal 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host $buildResult
    Write-Error "Network 编译失败，请检查错误信息"
}
Write-Host "  编译成功。" -ForegroundColor Green

# ====== Step 4: 清理并部署 DLL 到 Unity ======
Write-Host "[4/4] 部署 DLL 到 Unity ..." -ForegroundColor Yellow

if (-not (Test-Path $unityNetDir)) {
    New-Item -ItemType Directory -Path $unityNetDir -Force | Out-Null
}

# 删除旧 DLL
$oldDlls = Get-ChildItem "$unityNetDir\*.dll" -ErrorAction SilentlyContinue
foreach ($dll in $oldDlls) {
    Write-Host "  删除旧: $($dll.Name)"
    Remove-Item $dll.FullName -Force
}

# 复制新 DLL
$newDlls = Get-ChildItem "$networkBin\*.dll" -ErrorAction SilentlyContinue
foreach ($dll in $newDlls) {
    Copy-Item $dll.FullName "$unityNetDir\$($dll.Name)" -Force
    Write-Host "  已部署: $($dll.Name)"
}

Write-Host "  部署完成。" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  全部完成！已部署到 Unity Plugins/Net" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
