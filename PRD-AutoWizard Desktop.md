---
obsidianUIMode: 
created: Wednesday, February 11th 2026, 8:41:49 pm
modified: Wednesday, February 11th 2026, 8:47:27 pm
---
# **AutoWizard Desktop 產品需求文件 (PRD)**

## 1. 專案概覽

### 1.1. 專案願景 (Vision)

打造全球最直觀、且具備生產力工具等級穩定度的 Windows 自動化解決方案，讓辦公室人員能如同操作樂高積木般，將繁瑣的重複性工作轉化為高可靠性的自動化腳本。

### 1.2. 核心價值 (Core Value)

透過「視覺化巢狀邏輯」與「AI 視覺辨識（OCR/CV）」的深度整合，解決傳統自動化工具門檻過高或腳本難以維護的痛點，並提供單一檔案 (.aws) 封裝格式，達成極致的腳本攜帶性。

### 1.3. 成功指標 (KPIs)

- **腳本執行成功率**：在正確配置 DPI 縮放的情況下，腳本回放成功率 > 98%。
    
- **開發效率**：使用者從啟動錄製到生成第一個可用腳本的時間 < 5 分鐘。
    
- **資源佔用**：閒置時記憶體佔用 < 100MB，執行中 CPU 峰值 < 15% (含 OpenCV 運算)。
    

---

## 2. 目標用戶與場景 (User Personas)

|**角色**|**描述**|**核心需求**|**痛點**|
|---|---|---|---|
|**行政財務人員**|處理大量 Excel 與舊型 ERP 系統數據錄入。|需要穩定的 OCR 識別金額與單號，並自動填寫至表單。|系統間資料不互通，手動輸入極易出錯。|
|**自動化開發者**|具備邏輯基礎，為部門開發複雜自動化流程。|支援巢狀 IF/Loop 邏輯、變數傳遞與自定義報錯截圖。|傳統腳本不易分享，依賴圖檔時常遺失。|
|**遊戲重度玩家**|進行簡單的循環掛機或定時領取獎勵。|找圖點擊、防偵測模擬滑鼠軌跡（貝氏曲線）。|容易被反作弊偵測到固定的點擊特徵。|

---

## 3. 功能需求詳述 (Functional Requirements)

### 3.1. 視覺化巢狀指令開發環境 (Advanced Visual IDE)

本模組是整個 App 的核心介面，必須支援複雜的邏輯組合與極致的流暢度。

#### 3.1.1. 畫布佈局與指令容器邏輯

- **指令塊（Action Block）結構**：每個指令塊必須包含：
    
    - **指標區**：顯示指令 ID 與執行序號。
        
    - **圖示區**：根據指令類別（如滑鼠、OCR）顯示不同顏色與圖示。
        
    - **內嵌編輯區**：常用的簡單參數（如座標、延遲毫秒）可直接在列表修改，無需開啟側邊欄。
        
    - **狀態指示燈**：顯示「正常」、「停用」或「執行錯誤」。
        
- **巢狀層級（Nesting Level）規則**：
    
    - **容器指令（Container Actions）**：包含 `If-Else`、`Loop`、`Try-Catch`、`Group`。
        
    - **縮排與連接線**：層級之間需有垂直連接線引導視覺，縮排間距固定為 24px。
        
    - **自動對齊（Auto-Snap）**：當指令被拖曳至容器邊界時，容器需自動展開並顯示插入提示線。
        
- **拖曳行為與多選機制**：
    
    - **多選過濾**：當按住 `Ctrl` 選取了父容器後，系統需自動將其下的所有子指令視為一個「原子單位」。
        
    - **批量複製快取**：複製時，系統會將該段 JSON 指令結構存入剪貼簿，支援跨腳本檔案貼上。
        

#### 3.1.2. 屬性面板 (Property Inspector)

屬性面板會根據選中的指令動態變更內容。

|**參數類別**|**規格詳述**|
|---|---|
|**基礎參數**|名稱、備註（支援 Markdown）、執行前延遲、執行後延遲。|
|**目標參數**|座標 X/Y、視窗 Title、進程名稱（Process Name）、控制項 ID（Class Name）。|
|**變數綁定**|每個參數欄位右側皆有 `{x}` 按鈕，點擊後開啟「變數選擇器」。|
|**進階設定**|執行失敗後的動作（重試次數、重試間隔、跳轉至標籤）。|

---

### 3.2. 指令集百科全書 (Detailed Command Set)

我們將指令分為六大類別，每一項均需具備完整的邏輯參數。

#### 3.2.1. 鍵盤與滑鼠模擬 (Input Simulation)

- **Click (點擊)**：
    
    - **按鈕**：左鍵、右鍵、中鍵、雙擊、按下、彈起。
        
    - **模擬方式**：Windows API (`SendInput`)、驅動級模擬（選配）。
        
    - **座標偏置**：支援相對視窗座標、相對上一個點座標。
        
- **Type (文字輸入)**：
    
    - **輸入模式**：模擬按鍵（逐字模擬）與直接設置（後台 API，速度極快）。
        
    - **間隔時間**：隨機間隔 $t \in [min, max]$。
        
    - **特殊按鍵**：支援 `{ENTER}`, `{TAB}`, `{WIN}`, `{ALT+F4}` 等組合鍵。
        
- **Move (滑鼠移動)**：
    
    - **移動軌跡**：線性移動與**貝氏曲線 (Bezier Curve)** 移動。
        
    - **速度控制**：1 (最慢) 至 10 (瞬間)。
        

#### 3.2.2. 邏輯控制與流程 (Control Flow)

- **If-Else 條件判斷**：
    
    - **判斷類型**：圖片存在、文字包含、數值比較、檔案存在、視窗是否在前台。
        
- **Loop 迴圈**：
    
    - **次數迴圈**：固定次數。
        
    - **條件迴圈 (While)**：直到某個條件不成立為止。
        
    - **列表迭代 (Foreach)**：走訪資料夾中的檔案或 CSV 中的列。
        
- **Label & Goto (標籤與跳轉)**：
    
    - 用於非線性執行流程，支援跳轉回「腳本起始點」或「錯誤自理區」。
        

---

### 3.3. 視覺辨識系統 (CV & OCR Deep Specification)

這是本產品最核心的競爭力，必須處理複雜的螢幕縮放與反鋸齒問題。

#### 3.3.1. 找圖 (Image Template Matching)

- **演算法規格**：採用 OpenCV `TM_CCOEFF_NORMED`（歸一化相關係數）。
    
- **相似度門檻 (Confidence Threshold)**：
    
    - 0.0 到 1.0 的浮點數。
        
    - **自適應門檻**：系統會根據背景複雜度自動建議門檻。
        
- **搜尋範圍 (Region of Interest, ROI)**：
    
    - 全螢幕。
        
    - 特定視窗。
        
    - 指定矩形區域（$(x1, y1)$ 到 $(x2, y2)$）。
        
- **多圖搜尋**：支援在同一個指令中放入多張圖片（如不同外觀的按鈕），只要找到其中一張即視為成功。
    

#### 3.3.2. OCR 字元辨識與預處理

- **預處理流水線 (Image Preprocessing Pipeline)**：
    
    使用者可以透過 GUI 組合多個濾鏡，順序如下：
    
    1. **灰階化 (Grayscale)**：去除顏色干擾。
        
    2. **二值化 (Thresholding)**：
        
        - **固定門檻**：由使用者手動調整。
            
        - **自適應門檻 (Otsu)**：自動計算最佳切換點，適合明亮變化的場景。
            
    3. **縮放 (Scaling)**：將小文字放大 2-3 倍以提升辨識率。
        
    4. **去雜訊 (Median Blur)**：去除螢幕顆粒感。
        
- **辨識邏輯**：
    
    - **繁體中文優化**：針對微軟正黑體、標楷體進行模型微調。
        
    - **正則表達式過濾**：例如讀取金額時，設定正則 `\d+(\.\d{2})?` 自動提取數字，過濾掉貨幣符號。
        

---

### 3.4. 智慧錄製與 DPI 感知系統 (Smart Recorder & DPI Engine)

#### 3.4.1. 跨解析度座標轉換模型

為了解決使用者在不同顯示縮放（DPI）下腳本失效的問題，系統採用以下數學模型：

$$Physical\_Coordinate = Logic\_Coordinate \times (DPI / 96)$$

- **錄製端行為**：捕捉到點擊點 $(1500, 900)$，當時 DPI 為 150%，則存入腳本的邏輯座標為 $(1000, 600)$。
    
- **播放端行為**：若執行環境 DPI 為 100%，則實點擊位置為 $(1000, 600)$；若為 200%，則自動換算為 $(2000, 1200)$。
    

#### 3.4.2. 錄製器微觀狀態機

- **掛鉤 (Hooking)**：使用 `GlobalMouseHook` 監聽。
    
- **事件合併 (Event Merging)**：
    
    - 錄製時，連續的滑鼠位移事件會合併為一個「移動軌跡」指令。
        
    - 偵測到「視窗切換」時，自動插入 `SwitchWindow` 指令，而非單純紀錄點擊座標。
        
- **螢幕遮罩 UI**：
    
    錄製時，滑鼠下方會顯示一個精小的圓圈（放大鏡），即時顯示當前像素的 RGB 色碼與座標，增加專業感。
    

---

### 3.5. 變數與數據模型 (Variable & Data Persistence)

#### 3.5.1. 數據類型架構

1. **String (字串)**：預設類型，儲存 OCR 結果或路徑。
    
2. **Number (數值)**：計算迴圈次數或金額加總。
    
3. **Boolean (布林)**：紀錄「圖片是否找到」的標誌。
    
4. **List (列表)**：儲存檔案清單或多筆訂單編號。
    

#### 3.5.2. 作用域規範

- **局部變數**：僅在當前腳本內有效。
    
- **全域變數**：可由一個腳本寫入，另一個腳本讀取（跨 `.aws` 分享）。
    

---

### 3.6. 異常處理與除錯日誌 (Error Handling & Logging)

#### 3.6.1. 自動截圖日誌系統 (Screenshot History)

- **觸發機制**：
    
    - 定時模式：每 5000ms 截圖一次。
        
    - 事件模式：僅在「執行失敗」或「If 條件不成立」時截圖。
        
- **儲存管理**：
    
    - 所有截圖均以 Base64 或壓縮檔形式暫存，腳本執行結束後自動清理。
        
    - 日誌檢視器支援「播放」功能，模擬腳本執行過程中的螢幕變化，方便找出「為何沒找到按鈕」。
        

#### 3.6.2. UAC 權限管理流程

1. **啟動檢查**：調用 `WindowsIdentity.GetCurrent()` 判斷當前權限。
    
2. **提權對話框**：若權限不足，顯示專業的說明對話框，解釋「為什麼我們需要系統管理員權限（以便操作某些受保護的應用程式視窗）」。
    
3. **強制重啟**：點擊確認後，利用 `Verb = "runas"` 重啟應用程式。
    

---

### 3.7. UI/UX 交互詳細規範表 (The Micro-Interaction Matrix)

|**元件名稱**|**操作觸發**|**視覺反饋 (States)**|**行為邏輯**|**無障礙 (A11y)**|
|---|---|---|---|---|
|**指令拖拉把手**|滑鼠懸停|游標變更為 `Grab` 手勢|拖動時顯示半透明指令塊，目標位置顯示橫向插入線|支援 `Alt + Up/Down` 進行鍵盤排序|
|**變數引用框**|輸入 `{` 字元|彈出 Autocomplete 選單|列出所有已定義變數，支援 Fuzzy Search 模糊搜尋|鍵盤 `Enter` 選取變數|
|**執行進度條**|指令執行中|指令塊邊框出現呼吸燈（流光效果）|當前執行行自動捲動至視窗中心|螢幕閱讀器朗讀「正在執行第 N 行：點擊按鈕」|
|**圖片縮圖**|滑鼠懸停|彈出懸浮大圖預覽|顯示找圖的原始範圍與中心點標記|顯示 Alt 文字：截圖來源名稱|

---

### 3.8. 非功能性需求 (Non-Functional Requirements)

- **效能基準**：
    
    - **啟動時間**：在 NVMe SSD 環境下冷啟動 < 1.5 秒。
        
    - **掃描速率**：找圖掃描頻率最高支援 60 FPS（適用於高頻率動態畫面）。
        
- **相容性**：
    
    - 支援 Windows 10 (1903 以上) 與 Windows 11。
        
    - 支援 .NET 8 執行環境。
        

---

### 3.9. 腳本升級邏輯 (Migration Engine)

為了確保 `.aws` 檔案的未來相容性：

- **Version Check**：每個 `.aws` 包含 `schema_version`。
    
- **Auto-Mapper**：當 v2.0 開啟 v1.0 檔案時，會自動調用 `CommandLegacyHandler`。
    
    - 例如：v1.0 的 `SimpleClick` 指令會被自動轉換為 v2.0 的 `EnhancedClick`，並補足缺失的預設參數（如隨機延遲）。

## 4. ## 4. 技術架構 (Technical Architecture)

### 4.1. 系統分層架構 (Layered Architecture)

我們採用「核心 - 外殼」模型，確保 UI 與底層執行引擎完全解耦。

|**層次**|**名稱**|**技術實現**|**職責描述**|
|---|---|---|---|
|**Presentation Layer**|UI 介面層|WPF (.NET 8) + Prism (MVVM)|處理指令拖放、屬性編輯、即時日誌顯示與主題切換。|
|**Logic Layer**|指令直譯層|Custom Interpreter Engine|解析 JSON 指令序列，處理變數作用域、巢狀邏輯跳轉。|
|**Service Layer**|系統服務層|Windows API Wrappers|封裝鍵盤鉤子 (Hook)、DPI 感知轉換、UAC 權限管理。|
|**Resource Layer**|資源管理層|SQLite + System.IO.Compression|負責 `.aws` 檔案的壓縮/解壓、圖片資產載入、配置存取。|
|**Hardware Abstraction**|核心驅動層|C++ DLL / P/Invoke|呼叫 `user32.dll` 與 `gdi32.dll` 進行極速螢幕截取與輸入模擬。|

---

### 4.2. 腳本執行緒模型 (Threading & Concurrency)

為了防止腳本執行時 UI 卡死，並支援「緊急停止」熱鍵，我們必須採用 **雙線程/多線程** 架構。

- **UI Thread (主執行緒)**：
    
    - 負責介面渲染。
        
    - 監聽非阻塞式的系統事件（如視窗縮放）。
        
- **Execution Worker Thread (執行緒)**：
    
    - **指令執行器 (Executor)**：每個腳本在獨立的 `Task` 中運行。
        
    - **阻塞處理**：遇到 `Wait` 或 `FindImage` 延遲時，不占用 UI 線程。
        
- **Monitor Thread (監視執行緒)**：
    
    - **全域熱鍵監聽**：獨立監聽 `F12`。一旦觸發，立即調用 `CancellationTokenSource.Cancel()` 強制終止 Worker Thread。
        
    - **自動截圖監控**：每隔 $N$ 秒獨立運行的截圖任務。
        

---

### 4.3. 指令物件模型 (Action Object Model - AOM)

所有的指令必須繼承自基底類別 `BaseAction`，以實現高度的擴充性。

C#

```
// 核心抽象類別設計範例
public abstract class BaseAction {
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsEnabled { get; set; }
    public ErrorHandlingPolicy ErrorPolicy { get; set; } // 失敗自癒策略
    
    // 核心執行邏輯
    public abstract ActionResult Execute(ExecutionContext context);
}

// 容器指令類別 (如 Loop, If)
public class ContainerAction : BaseAction {
    public List<BaseAction> Children { get; set; } // 支援巢狀結構
}
```

---

### 4.4. 圖像辨識引擎詳解 (CV Engine Specs)

#### 4.4.1. 高速截圖流 (DirectX Capture)

- **傳統方式**：`Graphics.CopyFromScreen`（速度慢，CPU 佔用高）。
    
- **優化方式**：使用 **Desktop Duplication API (DirectX)**。這允許我們以高達 60-120 FPS 的速率獲取螢幕幀，對於遊戲掛機或動態 UI 辨識至關重要。
    

#### 4.4.2. 圖像匹配流程

1. **影像對齊**：根據當前播放端的 DPI 設定，動態縮放「範本圖片」。
    
2. **色彩空間轉換**：將 RGB 轉換為灰階（Grayscale）以減少運算量，除非使用者開啟「精確顏色匹配」。
    
3. **多核加速**：使用 `Parallel.For` 處理大區域的圖像搜尋，分攤至 CPU 多核心。
    

---

### 4.5. 變數解析與運算引擎 (Expression Parser)

為了支援如 `{OrderCount} + 1` 這樣的運算，我們需要整合一個輕量級的表達式解析器。

- **運算語法**：
    
    - 數學運算：`+`, `-`, `*`, `/`, `%`
        
    - 字串操作：`Concatenate`, `Substring`, `Replace`
        
    - 邏輯比較：`==`, `!=`, `>`, `<`
        
- **解析流程**：
    
    1. 指令執行前，Regex 掃描參數字串。
        
    2. 提取 `{...}` 內容。
        
    3. 從 `VariableDictionary` 查找值。
        
    4. 計算結果並回填至指令。
        

---

### 4.6. 資料庫與持久化設計

#### 4.6.1. `.aws` 內部數據模式 (JSON Schema)

我們棄用 XML，採用 JSON 以利於版本擴充與 Web 集成。

JSON

```
{
  "Metadata": {
    "Version": "2.1",
    "Author": "User01",
    "CreatedDPI": 144,
    "Resolution": "1920x1080"
  },
  "Variables": [
    { "Key": "LoopCount", "Value": "0", "Type": "Number" }
  ],
  "Workflow": [
    {
      "Type": "Loop",
      "Repeat": "{LoopCount}",
      "Children": [
        { "Type": "OCR_Read", "ROI": [100, 200, 50, 30], "OutputTo": "TempText" }
      ]
    }
  ]
}
```

---

### 4.7. 安全性與防禦性編程 (Defensive Programming)

#### 4.7.1. 異常隔離 (Sandbox-lite)

- 每個指令的執行都包覆在 `try-catch` 區塊中。
    
- **堆棧跟蹤 (Stack Trace)**：當報錯發生時，日誌不只顯示錯誤訊息，還會標記出是在哪一個「巢狀層級」的哪一行發生。
    

#### 4.7.2. 記憶體管理 (GC Optimization)

- **大物件堆 (LOH) 監控**：由於頻繁處理 Bitmap，必須定期手動呼叫 `GC.Collect()` 或使用 `MemoryPool` 來重用圖像緩衝區，防止腳本運行數天後出現 `OutOfMemoryException`。
    

---

### 4.8. UI 渲染優化 (WPF Virtualization)

當腳本達到數千行時，WPF 的視覺樹會變得沈重。

- **虛擬化列表 (VirtualizingStackPanel)**：僅渲染目前螢幕可見的指令塊。
    
- **非同步載入圖片**：屬性面板中的截圖縮圖採用非同步加載，避免切換指令時介面卡頓。
    

---

## 5. 擴充性規劃 (Extensibility)

### 5.1. 插件系統 (Plugin SDK)

預留 `IPluginAction` 接口，允許高階使用者使用 C# 編寫自定義插件 DLL，放進 `Plugins/` 資料夾後，App 工具箱會自動出現新指令。

### 5.2. 檔案與資源管理 (The .aws Container)

- **封裝邏輯**：`.aws` 為標準 ZIP 格式。
    
- **內部管理**：
    
    - 加載時解壓至 `%APPDATA%/AutoWizard/Temp/[ScriptID]`。
        
    - 關閉時自動清理，確保不留下垃圾圖檔。
        
- **版本自動升級**：
    
    - 內部 `manifest.json` 紀錄版本號。
        
    - 啟動時若偵測為舊版格式（如 v1.0），執行 `UpgradeAdapter` 將舊指令映射至 v2.0 結構。
        

### 5.3. 模擬與安全性

- **UAC 權限管理**：
    
    - 程式於 `Main()` 進入點檢查 `IsAdministrator()`。
        
    - 若為 `false`，呼叫 `ShellExecute` 並設定 `runas` 謂詞，強制使用者同意提權後才進入主程式。
        
- **貝氏曲線滑鼠移動 (Bezier Curve)**：
    
    - 透過控制點（Control Points）生成非線性的平滑移動路徑，模擬人類肌肉細微的不規則移動。
        
    - 此功能於「設定」選單提供 Toggle 選項。
        

### 5.4. 自動截圖日誌 (Auto-Logging)

- **邏輯**：背景線程每隔 $N$ 秒（預設 5s，可自訂）執行全螢幕截圖。
    
- **循環覆蓋**：僅保留最近 100 張截圖，避免硬碟空間被撐爆。
    
- **作用**：當腳本卡住時，使用者可透過「日誌檢視器」依序查看截圖，找出邏輯斷點。
    

---

## 6. 驗證與測試準則 (Acceptance Criteria)

- [ ] **權限驗證**：在一般使用者權限下執行，必須彈出 Windows UAC 提權視窗，拒絕則無法啟動。
    
- [ ] **巢狀邏輯測試**：`Loop (5次) > If (找圖) > Click` 結構需精確執行，且多選刪除 Loop 時子指令需同步消失。
    
- [ ] **DPI 適應測試**：在 100% 縮放錄製的點擊計算器按鈕腳本，在 150% 縮放環境回放時，點擊位置需完全精確。
    
- [ ] **OCR 精準度**：經過「二值化」預處理後，對於背景混亂的浮水印文字辨識率需提升 30% 以上。
    
- [ ] **效能基準**：執行 2 小時後，記憶體洩漏量需低於 5MB。
    

---
