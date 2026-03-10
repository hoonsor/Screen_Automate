# AutoWizard 指令使用手冊

本文件詳細說明 AutoWizard 自動化工具支援的所有指令及其屬性設定。

## 1. 檔案格式說明 (.aws)

`.aws` 腳本檔案實際上是一個 **ZIP 壓縮檔**，包含以下內容：

- `script.json`: 主要指令列表 (JSON 陣列)。
- `metadata.json`: 腳本資訊 (名稱、版本、建立時間)。
- `variables.json`: 變數定義列表。
- `images/`: (選用) 儲存腳本中使用的圖片資源。

若要手動編輯，請解壓縮 `.aws` 檔案，編輯 JSON 後再重新壓縮 (將副檔名改回 `.aws`)。

## 2. 通用屬性

所有指令都包含以下通用屬性：

| 屬性 | 類型 | 說明 |
|------|------|------|
| `$type` | 字串 | 指令類型識別碼 (如 "Click", "Type", "Wait") |
| `Id` | 字串 | 唯一識別碼 (需為 GUID 格式) |
| `Name` | 字串 | 顯示名稱 (方便閱讀) |
| `Description` | 字串 | 備註說明 |
| `IsEnabled` | 布林 | 是否啟用 (true/false) |
| `DelayBeforeMs` | 整數 | 執行前延遲 (毫秒)，預設 0 |
| `DelayAfterMs` | 整數 | 執行後延遲 (毫秒)，預設 0 |
| `ErrorPolicy` | 物件 | 錯誤處理策略 |

**ErrorPolicy 結構:**
```json
"ErrorPolicy": {
  "RetryCount": 0,          // 重試次數
  "RetryIntervalMs": 1000,  // 重試間隔 (毫秒)
  "ContinueOnError": false  // 發生錯誤是否繼續執行
}
```

## 3. 指令詳解

### 3.1 滑鼠點擊 (Click)
模擬滑鼠移動與點擊。

**識別碼 ($type):** `Click`

| 屬性 | 類型 | 說明 |
|------|------|------|
| `X` | 整數 | 重點 X 座標 |
| `Y` | 整數 | 重點 Y 座標 |
| `Button` | 列舉 | `Left` (左鍵), `Right` (右鍵), `Middle` (中鍵) |
| `ClickType` | 列舉 | `Single` (單擊), `Double` (雙擊), `Down` (按下), `Up` (放開) |
| `XExpression` | 字串 | (選用) X 座標變數表達式，如 `{targetX}` |
| `YExpression` | 字串 | (選用) Y 座標變數表達式，如 `{targetY}` |
| `IsHumanLike` | 布林 | 是否啟用真人模擬移動 (貝茲曲線軌跡) |
| `HumanLikeDurationMs` | 整數 | 模擬移動的耗時 (毫秒)，預設 500 |

### 3.2 輸入文字 (Type)
模擬鍵盤輸入文字。

**識別碼 ($type):** `Type`

| 屬性 | 類型 | 說明 |
|------|------|------|
| `Text` | 字串 | 要輸入的文字 (支援變數，如 `Hello {name}`) |
| `Mode` | 列舉 | `Simulate` (逐字模擬), `Direct` (剪貼簿貼上/快速輸入 - 尚未完全實作) |
| `IntervalMinMs` | 整數 | 隨機打字間隔最小值 (毫秒)，預設 50 |
| `IntervalMaxMs` | 整數 | 隨機打字間隔最大值 (毫秒)，預設 150 |

### 3.3 按鍵/快捷鍵 (Keyboard)
模擬單一鍵盤按鍵或組合鍵。

**識別碼 ($type):** `Keyboard`

| 屬性 | 類型 | 說明 |
|------|------|------|
| `Key` | 字串 | 按鍵名稱 (如 `ENTER`, `ESC`, `A`, `F1`, `TAB`) |
| `Modifiers` | 列舉(旗標) | 組合鍵修飾符：`None`, `Alt`, `Ctrl`, `Shift`, `Win` (可複選，如 `Ctrl | Shift`) |
| `HoldDurationMs`| 整數 | 按住持續時間 (毫秒)，預設 0 |

**常見按鍵代碼:**
- 功能鍵: `F1` - `F12`
- 控制鍵: `BACKSPACE`, `TAB`, `ENTER`, `ESC`, `SPACE`, `PAGEUP`, `PAGEDOWN`, `END`, `HOME`, `LEFT`, `UP`, `RIGHT`, `DOWN`, `INSERT`, `DELETE`, `PRINTSCREEN`
- 數字鍵盤: `NUMPAD0`-`9`, `NUMPAD+`, `NUMPAD-`, `NUMPAD*`, `NUMPAD/`, `NUMPADENTER`, `NUMPAD.`
- 一般按鍵: `A`-`Z`, `0`-`9`

### 3.4 等待 (Wait)
暫停執行一段時間。

**識別碼 ($type):** `Wait`

| 屬性 | 類型 | 說明 |
|------|------|------|
| `WaitType` | 列舉 | `Fixed` (固定時間), `Random` (隨機時間) |
| `DurationMs` | 整數 | 固定等待時間 (毫秒) |
| `RandomMinMs` | 整數 | 隨機等待下限 (毫秒) |
| `RandomMaxMs` | 整數 | 隨機等待上限 (毫秒) |

### 3.5 設定變數 (SetVariable)
設定或計算變數值。

**識別碼 ($type):** `SetVariable`

| 屬性 | 類型 | 說明 |
|------|------|------|
| `VariableName` | 字串 | 變數名稱 (不含大括號，如 `count`) |
| `ValueExpression`| 字串 | 值或表達式 (支援算術運算，如 `1 + 2` 或 `{count} + 1`) |

### 3.6 螢幕截圖 (Screenshot)
擷取螢幕畫面並存檔或存入變數。

**識別碼 ($type):** `Screenshot`

| 屬性 | 類型 | 說明 |
|------|------|------|
| `CaptureFull` | 布林 | `true` (全螢幕), `false` (指定區域) |
| `RegionX` | 整數 | 區域 X 座標 (僅當 CaptureFull=false 時有效) |
| `RegionY` | 整數 | 區域 Y 座標 |
| `RegionWidth` | 整數 | 區域寬度 |
| `RegionHeight` | 整數 | 區域高度 |
| `SavePath` | 字串 | 存檔路徑 (支援變數，如 `C:\Temp\{time}.png`) |
| `SaveToVariable` | 字串 | (選用) 將圖片 Base64 存入變數 |

### 3.7 條件判斷 (If)
**容器指令**，根據條件執行不同子指令。

**識別碼 ($type):** `If`

| 屬性 | 類型 | 說明 |
|------|------|------|
| `ConditionType` | 列舉 | 比較類型：<br>`VariableEquals` (相等)<br>`VariableNotEquals` (不相等)<br>`VariableGreaterThan` (大於)<br>`VariableLessThan` (小於)<br>`VariableContains` (包含)<br>`FileExists` (檔案存在)<br>`Expression` (自訂表達式) |
| `LeftOperand` | 字串 | 左運算元 (支援變數) |
| `RightOperand` | 字串 | 右運算元 (常數或變數) |
| `ConditionExpression` | 字串 | 自訂表達式 (僅用於 `Expression` 類型，如 `{a} > 10 && {b} == "ok"`) |
| `ThenActions` | 陣列 | 條件成立時執行的指令列表 |
| `ElseActions` | 陣列 | 條件不成立時執行的指令列表 |

### 3.8 迴圈 (Loop)
**容器指令**，重複執行子指令。

**識別碼 ($type):** `Loop`

| 屬性 | 類型 | 說明 |
|------|------|------|
| `LoopType` | 列舉 | `Count` (固定次數), `While` (條件迴圈), `Foreach` (遍歷列表 - 尚未完整支援 UI 設定) |
| `Count` | 整數 | 迴圈次數 (僅用於 `Count` 類型) |
| `WhileCondition`| 字串 | 繼續迴圈的條件表達式 (僅用於 `While` 類型) |
| `Children` | 陣列 | 迴圈內執行的指令列表 |

### 3.9 尋找影像 (FindImage)
在螢幕上搜尋指定圖片並取得座標。可配合 **Smart Capture (F11)** 自動產生。

**識別碼 ($type):** `FindImage`

| 屬性 | 類型 | 說明 |
|------|------|------|
| `TemplateImagePath` | 字串 | 範本圖片路徑 (相對路徑通常優於 Images/ 資料夾) |
| `Threshold` | 浮點數 | 相似度門檻 (0.1 ~ 1.0)，預設 0.8 |
| `TimeoutMs` | 整數 | 等待圖片出現的超時時間 (毫秒) |
| `IntervalMs` | 整數 | 搜尋間隔 (毫秒) |
| `ClickWhenFound` | 布林 | 找到後是否自動點擊中心點 |
| `SaveToVariable` | 字串 | (選用) 將座標存入變數 (產生 `{var}_X`, `{var}_Y`, `{var}_Confidence`) |

### 3.10 OCR 文字辨識 (OCR)
辨識螢幕指定區域的文字。

**識別碼 ($type):** `OCR`

| 屬性 | 類型 | 說明 |
|------|------|------|
| `RegionX/Y/Width/Height` | 整數 | 辨識區域 (若為空則辨識全螢幕) |
| `SearchText` | 字串 | (選用) 搜尋特定文字，若為空則回傳區域內所有文字 |
| `UseRegex` | 布林 | SearchText 是否為正規表達式 |
| `Language` | 字串 | 語言代碼 (如 `chi_tra`, `eng`) |
| `SaveToVariable` | 字串 | 將結果存入變數 |

## 4. 應用程式快捷鍵

AutoWizard Desktop 提供以下全域與區域快捷鍵以加速開發：

| 功能 | 快捷鍵 | 範圍 |
|------|--------|------|
| **新增腳本** | `Ctrl + N` | 主視窗 |
| **開啟腳本** | `Ctrl + O` | 主視窗 |
| **儲存腳本** | `Ctrl + S` | 主視窗 |
| **開始/停止錄製** | `Ctrl + F9` | **全域** (視窗最小化時亦可使用) |
| **執行/停止腳本** | `Ctrl + F10` | **全域** (視窗最小化時亦可使用) |
| **智慧截圖 (Smart Capture)** | `F11` | 主視窗 |
| **復原** | `Ctrl + Z` | 編輯器 |
| **重做** | `Ctrl + Shift + Z` | 編輯器 |
| **刪除指令** | `Delete` | 編輯器 |

## 5. 腳本語言模式 (DSL Scripting)

AutoWizard 提供進階的 **DSL 腳本語言 (Scripting Language)**，讓使用者能以類似程式碼的方式編寫自動化流程，取代繁瑣的 JSON 編輯。

### 5.1 切換模式

- 在編輯器右上角點擊 **「💻 程式碼模式」** 按鈕即可切換。
- **視覺 -> 代碼**：目前的視覺化指令列表會自動轉換為 DSL 腳本原始碼。
- **代碼 -> 視覺**：切換回視覺模式時，系統會解析腳本並重建指令列表。

### 5.2 語法說明

DSL 採用類似 C# 的函數調用風格，並自動包含註解。

**範例：**
```csharp
// 點擊開始按鈕
Click(100, 200, "Left", "Single");

// 輸入使用者名稱
Type("AdminUser");

// 等待載入
Wait(2000);

// 迴圈執行 5 次
Loop(5)
{
    // 點擊確認
    Click(500, 300);
}
```

### 5.3 編輯功能

- **自動註解**：系統產生代碼時會自動在每個指令上方加上描述註解。
- **雙向同步**：在代碼模式下的修改（如參數值、順序調整）都會在切換回視覺模式時生效。
- **錯誤檢查**：若語法錯誤（如缺少括號、參數錯誤），系統會顯示錯誤訊息並阻止切換回視覺模式。

### 5.4 應用場景

- **快速複製**：直接複製一段程式碼來重複建立相似的指令。
- **批次修改**：將腳本內容複製到外部文字編輯器進行搜尋/取代。
- **參數微調**：直接修改數值（如座標或時間），比操作圖形介面更快速。

