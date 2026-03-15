# 🛡️ ISSUES_LOG.md (2026-03-15)

## 1. UI Thread Deadlock during Windows OCR Initialization

- **Issue ID**: BUG-OCR-001
- **Severity**: Critical (Application Hang)
- **Status**: Resolved
- **Date**: 2026-03-15

### 📝 Root Cause
Windows 10 OCR API (`recognizedAsync`) 屬於 WinRT 非同步機制 (`IAsyncOperation`)。在 WPF 的 UI 執行緒中呼叫 `.GetAwaiter().GetResult()` 同步阻塞時，背景完成的回呼 (Callback) 嘗試回到主執行緒進行排程，但主執行緒正在阻塞等待，導致經典的 **UI Thread Deadlock**。

### ✅ Solution
將 `OCREngine` 的辨識邏輯封裝在 `Task.Run(async () => ...)` 中執行。
- 使用 `ConfigureAwait(false)` 確保非同步延續發生在 ThreadPool 而非 UI 執行緒。
- 主 UI 僅在 `Task.Run` 返回後獲取結果，不再阻塞 WinRT 的系統回呼。

### 🛡️ Prevention Plan
- 未來所有對 WinRT 或非同步系統 API 的呼叫，必須強制使用 `Task.Run` 打散執行緒。
- 嚴禁在 UI Event Handler 中使用 `.Result` 或 `.Wait()`。

---

## 2. MSBuild DLL Locking during Compilation

- **Issue ID**: BUG-ENV-001
- **Severity**: Medium (Build Blocker)
- **Status**: Resolved
- **Date**: 2026-03-15

### 📝 Root Cause
由於遺留的 `AutoWizard.UI` 進程因前述死結而駐留在內存中，且保持著對 `AutoWizard.Core.dll` 與 `AutoWizard.CV.dll` 的文件鎖。導致重啟編譯時出現 `MSB3021: Unable to copy file... process cannot access the file` 錯誤。

### ✅ Solution
- 使用 PowerShell 的 `Stop-Process -Name AutoWizard.UI -Force` 指令強制終止殘留進程。
- 清除臨時日誌後重新執行 Full Build。

### 🛡️ Prevention Plan
- 在開發環境的部署腳本中，增加「編譯前自動終止舊程序」的檢查步驟。
- 專案 TargetFramework 切換後，建議先執行 `dotnet clean`。
