# AutoWizard Desktop - 簡化版自主開發指南

## 📋 概述

此簡化版自主開發系統專為 **Antigravity 環境**設計,不需要 Claude Code CLI。

## 🚀 快速開始

### 1. 檢查專案狀態

```powershell
cd "D:\01-Project\01-Screen Automate"
.\.loki\autonomous-dev.ps1 -Mode status
```

### 2. 編譯專案

```powershell
.\.loki\autonomous-dev.ps1 -Mode build
```

### 3. 執行測試

```powershell
.\.loki\autonomous-dev.ps1 -Mode test
```

### 4. 生成進度報告

```powershell
.\.loki\autonomous-dev.ps1 -Mode report
```

### 5. 執行所有檢查

```powershell
.\.loki\autonomous-dev.ps1 -Mode all
```

### 6. 啟動監控模式 (持續監控)

```powershell
.\.loki\autonomous-dev.ps1 -Mode all -Watch
```

## 🔄 開發工作流程

### 標準流程

1. **檢查狀態** - 了解當前進度
   ```powershell
   .\.loki\autonomous-dev.ps1 -Mode status
   ```

2. **在對話中請求開發** - 告訴 Antigravity 繼續開發
   ```
   請繼續開發下一個功能
   ```

3. **驗證結果** - 編譯並測試
   ```powershell
   .\.loki\autonomous-dev.ps1 -Mode all
   ```

4. **重複** - 直到專案完成

### 監控模式流程

啟動監控模式後,腳本會每 30 秒自動:
- 檢查專案狀態
- 嘗試編譯
- 執行測試
- 顯示下一步建議

```powershell
.\.loki\autonomous-dev.ps1 -Watch
```

## 📁 檔案結構

```
.loki/
├── CONTINUITY.md              # 工作記憶 (手動/自動更新)
├── autonomous-dev.ps1         # 自主開發腳本
├── README-AUTONOMOUS.md       # 本檔案
├── progress-report-*.md       # 自動生成的進度報告
├── state/
│   └── orchestrator.json      # 狀態追蹤
└── queue/
    ├── pending.json           # 待辦任務
    ├── in-progress.json       # 進行中任務
    └── completed.json         # 已完成任務
```

## 🎯 使用場景

### 場景 1: 每日開發檢查

```powershell
# 早上開始工作前
.\.loki\autonomous-dev.ps1 -Mode status

# 在對話中
"請繼續昨天的開發工作"

# 開發完成後
.\.loki\autonomous-dev.ps1 -Mode all
```

### 場景 2: 持續整合

```powershell
# 啟動監控模式
.\.loki\autonomous-dev.ps1 -Watch

# 在另一個終端進行開發
# 監控模式會自動檢測變更並編譯測試
```

### 場景 3: 生成報告

```powershell
# 生成當前進度報告
.\.loki\autonomous-dev.ps1 -Mode report

# 報告會儲存在 .loki/progress-report-*.md
```

## 🔧 自訂設定

編輯 `autonomous-dev.ps1` 中的設定:

```powershell
# 專案路徑
$ProjectRoot = "D:\01-Project\01-Screen Automate"

# .NET SDK 路徑
$DotNetPath = "C:\Program Files\dotnet\dotnet.exe"

# 監控間隔 (秒)
Start-Sleep -Seconds 30  # 修改此行
```

## 📊 輸出說明

### 狀態符號

- `[✓]` - 成功/完成
- `[i]` - 資訊
- `[!]` - 警告
- `[✗]` - 錯誤
- `[→]` - 步驟/進行中

### 進度標記 (CONTINUITY.md)

- `✅` - 已完成
- `[/]` - 進行中
- `⏳` - 待辦

## 🆚 與完整 Loki Mode 的差異

| 功能 | 完整 Loki Mode | 簡化版 |
|------|---------------|--------|
| 需要 Claude Code CLI | ✅ 是 | ❌ 否 |
| 自動派發子代理 | ✅ 是 | ❌ 否 (需手動在對話中請求) |
| Web Dashboard | ✅ 是 | ❌ 否 |
| 自動循環執行 | ✅ 是 | ⚠️ 半自動 (監控模式) |
| 狀態檢查 | ✅ 是 | ✅ 是 |
| 編譯驗證 | ✅ 是 | ✅ 是 |
| 測試執行 | ✅ 是 | ✅ 是 |
| 進度報告 | ✅ 是 | ✅ 是 |

## 💡 最佳實踐

1. **定期檢查狀態** - 每次開發前執行 `-Mode status`
2. **頻繁編譯** - 確保程式碼隨時可編譯
3. **更新 CONTINUITY.md** - 保持工作記憶最新
4. **使用監控模式** - 長時間開發時啟動 `-Watch`
5. **生成報告** - 重要里程碑時執行 `-Mode report`

## 🐛 疑難排解

### 問題: 找不到 .NET SDK

**解決方案**: 更新 `$DotNetPath` 變數

```powershell
# 查找 dotnet.exe
where.exe dotnet

# 更新腳本中的路徑
$DotNetPath = "您的路徑\dotnet.exe"
```

### 問題: 編譯失敗

**解決方案**: 檢查錯誤訊息並在對話中請求修復

```
編譯失敗,錯誤: [貼上錯誤訊息]
請協助修復
```

### 問題: 監控模式無法停止

**解決方案**: 按 `Ctrl+C`

## 📞 支援

如有問題,在對話中詢問:
```
autonomous-dev.ps1 腳本遇到問題: [描述問題]
```

---

**版本**: 1.0.0  
**最後更新**: 2026-02-11  
**適用環境**: Antigravity + PowerShell 5.1+
