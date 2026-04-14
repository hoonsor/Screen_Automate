# AutoWizard Desktop

> 打造全球最直觀、且具備生產力工具等級穩定度的 Windows 自動化解決方案，讓辦公室人員能如同操作樂高積木般，將繁瑣的重複性工作轉化為高可靠性的自動化腳本。

## 📖 專案簡介 (Overview)
AutoWizard Desktop 是一款專為 Windows 打造的次世代視覺化自動化工具。結合了複雜的「視覺化巢狀邏輯」與先進的「AI 視覺辨識（包含 OCR 文字辨識與 OpenCV 電腦視覺圖像比對）」，完美解決傳統自動化工具門檻過高、腳本難以維護，或是錄製後在不同螢幕解析度下失效的痛點。

這個程式透過直觀的拖放式介面，讓使用者可以輕鬆建立條件判斷 (If-Else)、迴圈 (Loop)、防錯處理 (Try-Catch) 等複雜邏輯。更重要的是，它具備智慧預處理引擎，在畫面複雜或是 DPI 縮放變更的環境下，仍能穩定的辨識視窗畫面並精確執行模擬點擊、文字輸入等操作，最後能將所有流程與資源檔封裝至單一 `.aws` 檔案中，達成極致的攜帶與分享便利性。

目標使用者涵蓋需要處理大量 Excel 和 ERP 系統表單的行政/財務人員、開發自動化流程的工程師，甚至是希望防偵測掛機的重度遊戲玩家。透過 AutoWizard，任何看似繁雜、重複性高且容易出錯的電腦操作，都可以交由系統穩定代勞。

## ✨ 核心功能 (Key Features)
- **高階視覺化開發環境**：支援積木式的指令拖曳與多層巢狀結構，自動對齊與縮排，並提供動態屬性面板與局部/全域變數設計。
- **強大視覺與 OCR 辨識引擎**：內建 OpenCV 影像匹配與 OCR 預處理（包含二值化、去雜訊等濾鏡），解決畫面縮小與顏色差異，並支援全自動 DPI 換算適應。
- **真實滑鼠模擬技術**：提供線性或貝氏曲線 (Bezier Curve) 的軌跡移動，模擬真實人類操作以防止反作弊機制偵測，並支援直接呼叫 API 快速輸入。
- **自動化除錯防禦系統**：內建失敗自動截圖機制、詳細的執行緒日誌查詢，以及完善的例外捕捉策略，確保自動化中斷時能夠清楚發現故障原因與修復方向。
- **單檔封裝執行**：.aws 被設計為壓縮封裝檔，同時支援匯出為單一 EXE 綠色執行檔，不需另外複製圖庫或依賴環境即可執行。

## 🛠 技術棧 (Tech Stack)
- **核心架構**：C# 12, .NET 8, WPF (Windows Presentation Foundation)
- **UI 與模式**：Prism (MVVM 架構)
- **核心處理引擎**：自定義直譯引擎、Windows API 封裝 (SendInput, UAC)
- **視覺與截圖擷取**：DirectX Desktop Duplication API, OpenCV (EmguCV / OpenCvSharp), Windows.Media.Ocr
- **資料格式與存儲**：SQLite, 自定義 JSON 結構化定義格式

## 🚀 快速開始 (Quick Start)

### 環境要求
- 作業系統：Windows 10 (1903+) 或 Windows 11
- 環境相容：需已安裝 .NET 8 Desktop Runtime，並具備 DirectX 加速。

### 編譯與運行
```bash
# 1. 複製專案
git clone https://github.com/hoonsor/Screen_Automate.git

# 2. 開啟方案
使用 Visual Studio 2022 開啟 `AutoWizard.sln`

# 3. 建置並測試
設定 AutoWizard.UI 為啟動專案，按下 F5 開始執行
```

## 📁 專案結構 (Project Structure)
```text
AutoWizard/
 ├── AutoWizard.UI/       # WPF 表現層與使用者介面設計
 ├── AutoWizard.Core/     # 核心商業邏輯、CV / OCR 引擎、變數與指令解析器
 ├── AutoWizard.Runner/   # 無頭 (Headless) 執行端與 EXE 封裝處理
 ├── AutoWizard.CV/       # 電腦視覺及圖像匹配相關包裝
 └── AutoWizard.Tests/    # 單元測試與整合驗證模組
```

## 🔄 最新更新 (Recent Updates)
- `v0.1.0` - remove spaces between Chinese characters for better matching accuracy
- `v0.1.0` - Docs: Add session memory for March 15 and initialize Issues Log with RCA.
- `v0.1.0` - Major Refactor: Replace Tesseract with Windows.Media.Ocr & UI Enhancements suite. Fixed UI deadlock, added variable/history panels, and upgraded project target frameworks.

---
*Generated and maintained by Google Antigravity Architect*
