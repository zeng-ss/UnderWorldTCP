# ============================================================
# ARPGDemo 一键：生成 Proto → 复制 CS → 编译 Network.dll → 部署到 Unity
#
# 用法（在仓库根或任意位置执行均可，路径全部相对脚本自身定位）：
#   powershell -ExecutionPolicy Bypass -File Tools\deploy.ps1
#   powershell -ExecutionPolicy Bypass -File Tools\deploy.ps1 -MsBuildPath "C:\...\MSBuild.exe"
#
# ⚠️ 改了 .proto 之后必须跑这个脚本，否则服务端和客户端的协议类会不一致。
# ============================================================
param(
    [string]$MsBuildPath = $null
)

$ErrorActionPreference = "Stop"

# 目录定位：Tools/ 的上一级即仓库根
$repoRoot       = Split-Path -Parent $PSScriptRoot
$protoDir       = Join-Path $repoRoot "proto"
$protoSrc       = Join-Path $protoDir "proto"
$protoOut       = Join-Path $protoDir "out"
$protoc         = Join-Path $protoDir "bin\protoc.exe"
$networkDir     = Join-Path $repoRoot "Server\Network"
$networkCsproj  = Join-Path $networkDir "Network.csproj"
$networkProtoDir= Join-Path $networkDir "Proto"
$networkBin     = Join-Path $networkDir "bin\Debug"
$unityNetDir    = Join-Path $repoRoot "Assets\Plugins\Net"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ARPGDemo Proto生成 + Network编译 + 部署" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  仓库根: $repoRoot" -ForegroundColor DarkGray
Write-Host ""

# ====== 前置校验 ======
foreach ($p in @($protoSrc, $protoc, $networkCsproj, $unityNetDir)) {
    if (-not (Test-Path $p)) {
        Write-Error "路径不存在，请确认仓库结构完整: $p"
    }
}

# ====== Step 1: 生成 Proto C# ======
Write-Host "[1/4] 生成 Proto C# 文件..." -ForegroundColor Yellow
if (-not (Test-Path $protoOut)) {
    New-Item -ItemType Directory -Path $protoOut -Force | Out-Null
}

$protoFiles = @("ResultEntity.proto", "RequestEntity.proto")
foreach ($pf in $protoFiles) {
    $fullPath = Join-Path $protoSrc $pf
    Write-Host "  生成: $pf"
    & $protoc -I="$protoSrc" --csharp_out="$protoOut" $fullPath
    if ($LASTEXITCODE -ne 0) { Write-Error "protoc 失败: $pf" }
}
Write-Host "  Proto 生成完成。" -ForegroundColor Green

# ====== Step 2: 复制到 Server/Network/Proto ======
Write-Host "[2/4] 复制 CS 文件到 Server\Network\Proto ..." -ForegroundColor Yellow
$csFiles = @("ResultEntity.cs", "RequestEntity.cs")
foreach ($f in $csFiles) {
    $src = Join-Path $protoOut $f
    $dst = Join-Path $networkProtoDir $f
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
        Write-Host "  已复制: $f"
    } else {
        Write-Warning "  跳过: $src 不存在"
    }
}
Write-Host "  复制完成。" -ForegroundColor Green

# ====== Step 3: 编译 Network.csproj ======
Write-Host "[3/4] 编译 Network.csproj ..." -ForegroundColor Yellow

if ($MsBuildPath -and (Test-Path $MsBuildPath)) {
    $msbuild = $MsBuildPath
} else {
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
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null
        if ($vsPath) {
            $msbuild = Get-ChildItem "$vsPath\MSBuild\*\Bin\MSBuild.exe" -ErrorAction SilentlyContinue |
                       Select-Object -First 1 -ExpandProperty FullName
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

# ====== Step 4: 部署 DLL 到 Unity ======
Write-Host "[4/4] 部署 DLL 到 Unity ..." -ForegroundColor Yellow

$oldDlls = Get-ChildItem (Join-Path $unityNetDir "*.dll") -ErrorAction SilentlyContinue
foreach ($dll in $oldDlls) {
    Remove-Item $dll.FullName -Force
}

$newDlls = Get-ChildItem (Join-Path $networkBin "*.dll") -ErrorAction SilentlyContinue
foreach ($dll in $newDlls) {
    Copy-Item $dll.FullName (Join-Path $unityNetDir $dll.Name) -Force
    Write-Host "  已部署: $($dll.Name)"
}

Write-Host "  部署完成。" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  全部完成！记得把改动一起 commit" -ForegroundColor Cyan
Write-Host "  (proto/*.proto + Server/Network/Proto/*.cs + Assets/Plugins/Net/*.dll)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
