<#
.SYNOPSIS
    AutoWizard Desktop - 簡化版自主開發腳本
    適用於 Antigravity 環境 (不需要 Claude Code CLI)

.DESCRIPTION
    此腳本提供半自動化開發流程:
    1. 讀取 CONTINUITY.md 了解當前進度
    2. 檢查編譯狀態
    3. 執行測試
    4. 生成進度報告
    5. 提示下一步開發方向

.PARAMETER Mode
    執行模式: 'status' (檢查狀態), 'build' (編譯), 'test' (測試), 'report' (生成報告), 'all' (全部)

.EXAMPLE
    .\autonomous-dev.ps1 -Mode status
    .\autonomous-dev.ps1 -Mode all
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('status', 'build', 'test', 'report', 'all')]
    [string]$Mode = 'all',
    
    [Parameter(Mandatory=$false)]
    [switch]$Watch = $false
)

# 設定
$ProjectRoot = "D:\01-Project\01-Screen Automate"
$LokiDir = Join-Path $ProjectRoot ".loki"
$ContinuityFile = Join-Path $LokiDir "CONTINUITY.md"
$SolutionFile = Join-Path $ProjectRoot "AutoWizard.sln"
$DotNetPath = "C:\Program Files\dotnet\dotnet.exe"

# 顏色輸出
function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║ $Text" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Text)
    Write-Host "[✓] $Text" -ForegroundColor Green
}

function Write-Info {
    param([string]$Text)
    Write-Host "[i] $Text" -ForegroundColor Cyan
}

function Write-Warning {
    param([string]$Text)
    Write-Host "[!] $Text" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Text)
    Write-Host "[✗] $Text" -ForegroundColor Red
}

function Write-Step {
    param([string]$Text)
    Write-Host "[→] $Text" -ForegroundColor Magenta
}

# 檢查狀態
function Get-ProjectStatus {
    Write-Header "專案狀態檢查"
    
    # 讀取 CONTINUITY.md
    if (Test-Path $ContinuityFile) {
        Write-Success "找到 CONTINUITY.md"
        $content = Get-Content $ContinuityFile -Raw
        
        # 提取當前階段
        if ($content -match "Phase\*\*:\s*(.+)") {
            Write-Info "當前階段: $($matches[1])"
        }
        
        # 提取當前任務
        if ($content -match "Task\*\*:\s*(.+)") {
            Write-Info "當前任務: $($matches[1])"
        }
        
        # 提取進度檢查點
        Write-Step "進度檢查點:"
        $checkpoints = $content -split "`n" | Where-Object { $_ -match "^\s*-\s*" }
        foreach ($checkpoint in $checkpoints) {
            if ($checkpoint -match "✅") {
                Write-Success $checkpoint.Trim()
            } elseif ($checkpoint -match "\[/\]") {
                Write-Warning $checkpoint.Trim()
            } else {
                Write-Info $checkpoint.Trim()
            }
        }
    } else {
        Write-Error "找不到 CONTINUITY.md"
    }
    
    # 檢查專案結構
    Write-Step "檢查專案結構..."
    $projects = @(
        "AutoWizard.Core",
        "AutoWizard.CV",
        "AutoWizard.UI",
        "AutoWizard.Tests"
    )
    
    foreach ($proj in $projects) {
        $projPath = Join-Path $ProjectRoot "$proj\$proj.csproj"
        if (Test-Path $projPath) {
            Write-Success "$proj 專案存在"
        } else {
            Write-Error "$proj 專案不存在"
        }
    }
}

# 編譯專案
function Build-Project {
    Write-Header "編譯專案"
    
    if (-not (Test-Path $DotNetPath)) {
        Write-Error ".NET SDK 未找到: $DotNetPath"
        return $false
    }
    
    Write-Step "執行編譯..."
    $buildOutput = & $DotNetPath build $SolutionFile 2>&1
    
    # 分析編譯結果
    $buildSuccess = $LASTEXITCODE -eq 0
    
    if ($buildSuccess) {
        Write-Success "編譯成功!"
        
        # 統計警告
        $warnings = ($buildOutput | Select-String "warning").Count
        if ($warnings -gt 0) {
            Write-Warning "發現 $warnings 個警告"
        }
    } else {
        Write-Error "編譯失敗!"
        
        # 顯示錯誤
        $errors = $buildOutput | Select-String "error"
        foreach ($error in $errors) {
            Write-Error $error.Line
        }
    }
    
    return $buildSuccess
}

# 執行測試
function Run-Tests {
    Write-Header "執行測試"
    
    $testProject = Join-Path $ProjectRoot "AutoWizard.Tests\AutoWizard.Tests.csproj"
    
    if (-not (Test-Path $testProject)) {
        Write-Warning "測試專案不存在,跳過測試"
        return $true
    }
    
    Write-Step "執行單元測試..."
    $testOutput = & $DotNetPath test $testProject --no-build 2>&1
    
    $testSuccess = $LASTEXITCODE -eq 0
    
    if ($testSuccess) {
        Write-Success "測試通過!"
        
        # 提取測試統計
        $testOutput | Select-String "Passed!" | ForEach-Object {
            Write-Info $_.Line
        }
    } else {
        Write-Error "測試失敗!"
        
        # 顯示失敗的測試
        $testOutput | Select-String "Failed" | ForEach-Object {
            Write-Error $_.Line
        }
    }
    
    return $testSuccess
}

# 生成進度報告
function New-ProgressReport {
    Write-Header "生成進度報告"
    
    $reportFile = Join-Path $LokiDir "progress-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').md"
    
    $report = @"
# AutoWizard Desktop - 自動進度報告

**生成時間**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## 編譯狀態
$(if (Build-Project) { "✅ 編譯成功" } else { "❌ 編譯失敗" })

## 測試狀態
$(if (Run-Tests) { "✅ 測試通過" } else { "❌ 測試失敗" })

## 專案結構
"@

    $projects = @(
        "AutoWizard.Core",
        "AutoWizard.CV",
        "AutoWizard.UI",
        "AutoWizard.Tests"
    )
    
    foreach ($proj in $projects) {
        $projPath = Join-Path $ProjectRoot "$proj\$proj.csproj"
        if (Test-Path $projPath) {
            $report += "`n- ✅ $proj"
        } else {
            $report += "`n- ❌ $proj"
        }
    }
    
    $report += "`n`n## 下一步建議`n"
    
    # 讀取 CONTINUITY.md 提取待辦事項
    if (Test-Path $ContinuityFile) {
        $content = Get-Content $ContinuityFile -Raw
        $pending = $content -split "`n" | Where-Object { $_ -match "^\s*-\s*" -and $_ -match "⏳" }
        
        if ($pending.Count -gt 0) {
            $report += "`n### 待完成任務:`n"
            foreach ($item in $pending) {
                $report += "$item`n"
            }
        }
    }
    
    $report | Out-File -FilePath $reportFile -Encoding UTF8
    Write-Success "報告已生成: $reportFile"
    
    # 顯示報告
    Write-Host "`n" -NoNewline
    Get-Content $reportFile | ForEach-Object {
        if ($_ -match "✅") {
            Write-Host $_ -ForegroundColor Green
        } elseif ($_ -match "❌") {
            Write-Host $_ -ForegroundColor Red
        } elseif ($_ -match "^#") {
            Write-Host $_ -ForegroundColor Cyan
        } else {
            Write-Host $_
        }
    }
}

# 提示下一步
function Show-NextSteps {
    Write-Header "下一步開發建議"
    
    if (Test-Path $ContinuityFile) {
        $content = Get-Content $ContinuityFile -Raw
        
        # 提取待辦事項
        $pending = $content -split "`n" | Where-Object { $_ -match "^\s*-\s*" -and $_ -match "⏳" }
        
        if ($pending.Count -gt 0) {
            Write-Step "待完成任務:"
            foreach ($item in $pending | Select-Object -First 3) {
                Write-Info $item.Trim()
            }
        }
        
        # 提取進行中的任務
        $inProgress = $content -split "`n" | Where-Object { $_ -match "^\s*-\s*\[/\]" }
        
        if ($inProgress.Count -gt 0) {
            Write-Step "進行中的任務:"
            foreach ($item in $inProgress) {
                Write-Warning $item.Trim()
            }
        }
    }
    
    Write-Host ""
    Write-Info "建議操作:"
    Write-Host "  1. 在對話中告訴 Antigravity: '請繼續開發下一個功能'" -ForegroundColor White
    Write-Host "  2. 或指定具體任務: '請實作拖放系統'" -ForegroundColor White
    Write-Host "  3. 或執行: .\autonomous-dev.ps1 -Mode all -Watch (持續監控)" -ForegroundColor White
}

# 監控模式
function Start-WatchMode {
    Write-Header "啟動監控模式"
    Write-Info "每 30 秒檢查一次專案狀態 (按 Ctrl+C 停止)"
    
    while ($true) {
        Clear-Host
        Get-ProjectStatus
        
        if (Build-Project) {
            Run-Tests | Out-Null
        }
        
        Show-NextSteps
        
        Write-Host "`n下次檢查: $(Get-Date -Date (Get-Date).AddSeconds(30) -Format 'HH:mm:ss')" -ForegroundColor Gray
        Start-Sleep -Seconds 30
    }
}

# 主程式
function Main {
    Write-Host @"
╔════════════════════════════════════════════════════════════════╗
║           AutoWizard Desktop - 自主開發助手                    ║
║                  Antigravity 簡化版                            ║
╚════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

    if ($Watch) {
        Start-WatchMode
        return
    }

    switch ($Mode) {
        'status' {
            Get-ProjectStatus
            Show-NextSteps
        }
        'build' {
            Build-Project
        }
        'test' {
            Run-Tests
        }
        'report' {
            New-ProgressReport
        }
        'all' {
            Get-ProjectStatus
            if (Build-Project) {
                Run-Tests
            }
            Show-NextSteps
        }
    }
    
    Write-Host ""
}

# 執行
Main
