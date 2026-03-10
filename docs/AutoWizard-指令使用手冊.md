# AutoWizard 指令使用手冊

> **版本**：1.0 | **最後更新**：2026-02-15

---

## 目錄

1. [概述](#1-概述)
2. [.aws 檔案格式](#2-aws-檔案格式)
3. [共用屬性（BaseAction）](#3-共用屬性baseaction)
4. [輸入指令](#4-輸入指令)
   - [Click — 滑鼠點擊](#41-click--滑鼠點擊)
   - [Type — 文字輸入](#42-type--文字輸入)
   - [Keyboard — 鍵盤按鍵](#43-keyboard--鍵盤按鍵)
   - [Wait — 等待延遲](#44-wait--等待延遲)
   - [Screenshot — 螢幕截圖](#45-screenshot--螢幕截圖)
5. [控制流程指令](#5-控制流程指令)
   - [SetVariable — 設定變數](#51-setvariable--設定變數)
   - [Loop — 迴圈](#52-loop--迴圈)
   - [If — 條件判斷](#53-if--條件判斷)
6. [視覺辨識指令](#6-視覺辨識指令)
   - [FindImage — 尋找影像](#61-findimage--尋找影像)
   - [OCR — 文字辨識](#62-ocr--文字辨識)
7. [變數與表達式系統](#7-變數與表達式系統)
8. [錯誤處理策略](#8-錯誤處理策略)
9. [完整範例腳本](#9-完整範例腳本)

---

## 1. 概述

AutoWizard 是一套桌面自動化工具，透過預先定義的 **指令（Action）** 序列，模擬滑鼠點擊、鍵盤輸入、條件判斷等操作來自動完成重複性工作。

腳本以 `.aws`（AutoWizard Script）格式儲存，可在 AutoWizard 編輯器中載入、編輯及執行。

---

## 2. .aws 檔案格式

`.aws` 檔案本質上是一個 **ZIP 壓縮包**，包含以下檔案：

| 檔案 | 說明 |
|---|---|
| `script.json` | 指令序列的 JSON 陣列（核心內容） |
| `metadata.json` | 中繼資料（腳本名稱、建立時間、版本等） |
| `variables.json` | 變數定義清單 |
| `images/` | 影像資源目錄（存放 FindImage 等指令用到的圖片） |

### metadata.json 範例

```json
{
  "Name": "我的自動化腳本",
  "CreatedAt": "2026-02-15T13:00:00",
  "Version": "1.0",
  "ActionCount": 15,
  "ImageCount": 1
}
```

### variables.json 範例

```json
[
  {
    "Name": "counter",
    "Type": "Integer",
    "DefaultValue": "0",
    "Description": "迴圈計數器"
  },
  {
    "Name": "username",
    "Type": "String",
    "DefaultValue": "admin",
    "Description": "登入帳號"
  }
]
```

**變數類型（VariableType）**：

| 值 | 說明 |
|---|---|
| `String` | 文字字串 |
| `Integer` | 整數 |
| `Double` | 浮點數 |
| `Boolean` | 布林值（true / false） |

---

## 3. 共用屬性（BaseAction）

**每一個指令**都繼承自 `BaseAction`，具備以下共用屬性：

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `$type` | string | *(必填)* | 指令類型辨識碼，決定此物件是哪種指令 |
| `Id` | string | 自動產生 GUID | 指令的唯一識別碼 |
| `Name` | string | `""` | 指令的顯示名稱（在編輯器中呈現） |
| `Description` | string | `""` | 指令的說明文字 |
| `IsEnabled` | bool | `true` | 是否啟用此指令（設為 `false` 則執行時跳過） |
| `DelayBeforeMs` | int | `0` | 執行此指令**前**的延遲毫秒數 |
| `DelayAfterMs` | int | `0` | 執行此指令**後**的延遲毫秒數 |
| `ErrorPolicy` | object | 見下方 | 錯誤處理策略 |

### $type 值對照表

| `$type` 值 | 對應指令 |
|---|---|
| `Click` | 滑鼠點擊 |
| `Type` | 文字輸入 |
| `Keyboard` | 鍵盤按鍵 |
| `Wait` | 等待延遲 |
| `Screenshot` | 螢幕截圖 |
| `SetVariable` | 設定變數 |
| `Loop` | 迴圈 |
| `If` | 條件判斷 |
| `FindImage` | 尋找影像 |
| `OCR` | 文字辨識 |

---

## 4. 輸入指令

### 4.1 Click — 滑鼠點擊

模擬滑鼠移動到指定座標並進行點擊。

**`$type`: `"Click"`**

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `X` | int | `0` | 螢幕 X 座標（像素） |
| `Y` | int | `0` | 螢幕 Y 座標（像素） |
| `XExpression` | string | `""` | X 座標的變數表達式（優先於 `X`），例如 `"{savedX}"` |
| `YExpression` | string | `""` | Y 座標的變數表達式（優先於 `Y`），例如 `"{savedY}"` |
| `Button` | enum | `"Left"` | 滑鼠按鈕 |
| `ClickType` | enum | `"Single"` | 點擊類型 |

**Button 可選值**：

| 值 | 說明 |
|---|---|
| `Left` | 左鍵 |
| `Right` | 右鍵 |
| `Middle` | 中鍵（滾輪鍵） |

**ClickType 可選值**：

| 值 | 說明 |
|---|---|
| `Single` | 單擊 |
| `Double` | 雙擊 |
| `Down` | 按住（不放開，用於拖曳起始） |
| `Up` | 放開（用於拖曳結束） |

#### 範例

```json
{
  "$type": "Click",
  "Name": "點擊開始按鈕",
  "Description": "點擊畫面上的開始按鈕",
  "X": 500,
  "Y": 300,
  "Button": "Left",
  "ClickType": "Single"
}
```

#### 使用變數表達式的範例

```json
{
  "$type": "Click",
  "Name": "點擊找到的影像位置",
  "X": 0,
  "Y": 0,
  "XExpression": "{foundImg_X}",
  "YExpression": "{foundImg_Y}",
  "Button": "Left",
  "ClickType": "Single"
}
```

> **提示**：當 `XExpression` 或 `YExpression` 有值時，會優先使用表達式解析結果；若解析失敗，則退回使用 `X` / `Y` 的固定值。

#### 拖曳操作範例

```json
[
  {
    "$type": "Click",
    "Name": "拖曳起點 - 按住",
    "X": 100, "Y": 200,
    "Button": "Left",
    "ClickType": "Down"
  },
  {
    "$type": "Wait",
    "Name": "拖曳移動時間",
    "DurationMs": 500
  },
  {
    "$type": "Click",
    "Name": "拖曳終點 - 放開",
    "X": 400, "Y": 500,
    "Button": "Left",
    "ClickType": "Up"
  }
]
```

---

### 4.2 Type — 文字輸入

模擬鍵盤逐字輸入文字，支援 Unicode（可輸入中文等多語言字元）。

**`$type`: `"Type"`**

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `Text` | string | `""` | 要輸入的文字內容（支援 `{變數名}` 表達式） |
| `Mode` | enum | `"Simulate"` | 輸入模式 |
| `IntervalMinMs` | int | `50` | 每個字元之間的**最小**間隔（毫秒） |
| `IntervalMaxMs` | int | `150` | 每個字元之間的**最大**間隔（毫秒） |

**Mode 可選值**：

| 值 | 說明 |
|---|---|
| `Simulate` | 模擬按鍵（透過 SendInput 逐字傳送 Unicode） |
| `Direct` | 直接設置（保留，目前行為同 Simulate） |

> **注意**：輸入間隔是在 `IntervalMinMs` 和 `IntervalMaxMs` 之間隨機取值，模擬人類自然輸入節奏。若想快速輸入，可將兩者都設為較小值（如 10）。

#### 範例

```json
{
  "$type": "Type",
  "Name": "輸入帳號",
  "Text": "{username}",
  "IntervalMinMs": 30,
  "IntervalMaxMs": 80
}
```

---

### 4.3 Keyboard — 鍵盤按鍵

模擬按下特定的鍵盤按鍵，支援組合鍵（Ctrl、Shift、Alt、Win）。

**`$type`: `"Keyboard"`**

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `Key` | string | `""` | 按鍵名稱（見下方按鍵對照表） |
| `Modifiers` | enum/int | `"None"` | 修飾鍵（可組合使用） |
| `HoldDurationMs` | int | `0` | 按住按鍵的持續時間（毫秒），`0` 表示正常按下即放開 |

**Modifiers 可選值**（可以用 `,` 組合）：

| 值 | 說明 |
|---|---|
| `None` | 無修飾鍵 |
| `Alt` | Alt 鍵 |
| `Ctrl` | Ctrl 鍵 |
| `Shift` | Shift 鍵 |
| `Win` | Windows 鍵 |

> **組合用法**：在 JSON 中，可使用 `"Ctrl, Shift"` 表示同時按住 Ctrl 和 Shift。

**按鍵名稱對照表**：

| 分類 | 按鍵名稱 |
|---|---|
| 字母 | `A` ~ `Z` |
| 數字 | `0` ~ `9` |
| 功能鍵 | `F1` ~ `F12` |
| 數字鍵盤 | `NUMPAD0` ~ `NUMPAD9`, `NUMPAD*`, `NUMPAD+`, `NUMPADENTER`, `NUMPAD-`, `NUMPAD.`, `NUMPAD/` |
| 鎖定鍵 | `NUMLOCK`, `SCROLLLOCK`, `CAPSLOCK` |
| 控制鍵 | `ENTER`, `TAB`, `BACKSPACE`, `ESC`, `SPACE`, `DELETE`, `INSERT` |
| 導航鍵 | `UP`, `DOWN`, `LEFT`, `RIGHT`, `HOME`, `END`, `PAGEUP`, `PAGEDOWN` |
| 系統鍵 | `PRINTSCREEN`, `PAUSE` |
| 修飾鍵 | `SHIFT`, `CTRL`, `ALT` |

#### 範例：單一按鍵

```json
{
  "$type": "Keyboard",
  "Name": "按下 Enter",
  "Key": "ENTER",
  "Modifiers": "None"
}
```

#### 範例：組合鍵（Ctrl+C 複製）

```json
{
  "$type": "Keyboard",
  "Name": "複製",
  "Description": "Ctrl+C 複製到剪貼簿",
  "Key": "C",
  "Modifiers": "Ctrl"
}
```

#### 範例：三鍵組合（Ctrl+Shift+S 另存新檔）

```json
{
  "$type": "Keyboard",
  "Name": "另存新檔",
  "Key": "S",
  "Modifiers": "Ctrl, Shift"
}
```

#### 範例：長按按鍵

```json
{
  "$type": "Keyboard",
  "Name": "長按空白鍵",
  "Key": "SPACE",
  "HoldDurationMs": 2000
}
```

---

### 4.4 Wait — 等待延遲

暫停執行指定的時間，可設定固定或隨機延遲。

**`$type`: `"Wait"`**

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `WaitType` | enum | `"Fixed"` | 等待模式 |
| `DurationMs` | int | `1000` | 固定等待時間（毫秒），僅 `Fixed` 模式使用 |
| `DurationExpression` | string | `""` | 等待時間的變數表達式（優先於 `DurationMs`），僅 `Fixed` 模式 |
| `RandomMinMs` | int | `500` | 隨機等待的最小值（毫秒），僅 `Random` 模式使用 |
| `RandomMaxMs` | int | `2000` | 隨機等待的最大值（毫秒），僅 `Random` 模式使用 |

**WaitType 可選值**：

| 值 | 說明 |
|---|---|
| `Fixed` | 固定時間等待 |
| `Random` | 在指定範圍內隨機等待 |

#### 範例：固定 2 秒等待

```json
{
  "$type": "Wait",
  "Name": "等待頁面載入",
  "WaitType": "Fixed",
  "DurationMs": 2000
}
```

#### 範例：隨機等待（模擬人類行為）

```json
{
  "$type": "Wait",
  "Name": "隨機延遲",
  "WaitType": "Random",
  "RandomMinMs": 800,
  "RandomMaxMs": 3000
}
```

#### 範例：使用變數表達式

```json
{
  "$type": "Wait",
  "Name": "動態等待",
  "WaitType": "Fixed",
  "DurationExpression": "{waitTime}",
  "DurationMs": 1000
}
```

> **說明**：若 `DurationExpression` 有值且解析成功，優先使用其結果；否則退回使用 `DurationMs`。

---

### 4.5 Screenshot — 螢幕截圖

擷取螢幕畫面，可儲存為檔案或存入變數（Base64 格式）。

**`$type`: `"Screenshot"`**

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `CaptureFull` | bool | `true` | `true` 擷取全螢幕，`false` 擷取指定區域 |
| `SavePath` | string | `""` | 截圖儲存路徑（支援變數表達式），留空則不儲存檔案 |
| `SaveToVariable` | string | `""` | 儲存截圖的 Base64 資料到指定變數名，留空則不存變數 |
| `RegionX` | int | `0` | 擷取區域的 X 起始座標（`CaptureFull` 為 `false` 時才有作用） |
| `RegionY` | int | `0` | 擷取區域的 Y 起始座標 |
| `RegionWidth` | int | `0` | 擷取區域的寬度（像素） |
| `RegionHeight` | int | `0` | 擷取區域的高度（像素） |

#### 範例：擷取全螢幕並存檔

```json
{
  "$type": "Screenshot",
  "Name": "全螢幕截圖",
  "CaptureFull": true,
  "SavePath": "C:\\Screenshots\\capture_{_loopIndex}.png"
}
```

#### 範例：擷取特定區域並存入變數

```json
{
  "$type": "Screenshot",
  "Name": "擷取按鈕區域",
  "CaptureFull": false,
  "RegionX": 100,
  "RegionY": 200,
  "RegionWidth": 300,
  "RegionHeight": 50,
  "SaveToVariable": "buttonImage"
}
```

---

## 5. 控制流程指令

### 5.1 SetVariable — 設定變數

在執行時設定或修改變數的值，支援算術運算。

**`$type`: `"SetVariable"`**

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `VariableName` | string | `""` | 要設定的變數名稱（必填） |
| `ValueExpression` | string | `""` | 變數的值（支援表達式運算） |

**ValueExpression 支援的格式**：

| 範例 | 說明 | 結果類型 |
|---|---|---|
| `"Hello"` | 純文字 | String |
| `"123"` | 整數 | Integer |
| `"3.14"` | 浮點數 | Double |
| `"true"` / `"false"` | 布林值 | Boolean |
| `"{count} + 1"` | 算術運算（需先替換變數） | Integer/Double |
| `"{name}_suffix"` | 字串拼接（變數 + 文字） | String |

#### 範例：設定固定值

```json
{
  "$type": "SetVariable",
  "Name": "設定計數器",
  "VariableName": "counter",
  "ValueExpression": "0"
}
```

#### 範例：遞增計數

```json
{
  "$type": "SetVariable",
  "Name": "計數器+1",
  "VariableName": "counter",
  "ValueExpression": "{counter} + 1"
}
```

#### 範例：字串組合

```json
{
  "$type": "SetVariable",
  "Name": "設定檔名",
  "VariableName": "fileName",
  "ValueExpression": "report_{counter}.png"
}
```

---

### 5.2 Loop — 迴圈

重複執行一組子指令，支援計次、條件、列表疊代三種模式。

**`$type`: `"Loop"`**

**迴圈是一個容器指令**，其子指令放在 `Children` 陣列中。

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `LoopType` | enum | `"Count"` | 迴圈類型 |
| `Count` | int | `1` | 固定迭代次數（僅 `Count` 模式） |
| `WhileCondition` | string | `""` | 條件表達式（僅 `While` 模式），條件為 true 時持續迴圈 |
| `ForeachVariable` | string | `""` | 當前項目存入的變數名（僅 `Foreach` 模式） |
| `ForeachList` | string[] | `[]` | 要疊代的清單（僅 `Foreach` 模式） |
| `Children` | Action[] | `[]` | 每次迭代要執行的子指令清單 |

**LoopType 可選值**：

| 值 | 說明 |
|---|---|
| `Count` | 固定次數迴圈 |
| `While` | 條件迴圈（當 `WhileCondition` 為 true 時持續執行，上限 10000 次） |
| `Foreach` | 列表疊代（依序取出 `ForeachList` 中的每個元素） |

> **內建變數**：迴圈執行時會自動設定 `_loopIndex` 變數（從 0 開始），可在子指令中使用。

#### 範例：固定 5 次迴圈

```json
{
  "$type": "Loop",
  "Name": "重複點擊 5 次",
  "LoopType": "Count",
  "Count": 5,
  "Children": [
    {
      "$type": "Click",
      "Name": "點擊按鈕",
      "X": 500, "Y": 300,
      "Button": "Left",
      "ClickType": "Single"
    },
    {
      "$type": "Wait",
      "Name": "間隔",
      "DurationMs": 1000
    }
  ]
}
```

#### 範例：條件迴圈

```json
{
  "$type": "Loop",
  "Name": "計數到 10",
  "LoopType": "While",
  "WhileCondition": "{counter} < 10",
  "Children": [
    {
      "$type": "SetVariable",
      "Name": "計數器+1",
      "VariableName": "counter",
      "ValueExpression": "{counter} + 1"
    }
  ]
}
```

#### 範例：列表疊代

```json
{
  "$type": "Loop",
  "Name": "依序處理檔案",
  "LoopType": "Foreach",
  "ForeachVariable": "currentFile",
  "ForeachList": ["report.xlsx", "data.csv", "summary.docx"],
  "Children": [
    {
      "$type": "Type",
      "Name": "輸入檔名",
      "Text": "{currentFile}"
    },
    {
      "$type": "Keyboard",
      "Name": "按 Enter 確認",
      "Key": "ENTER"
    }
  ]
}
```

---

### 5.3 If — 條件判斷

根據條件決定執行不同的指令分支（支援 If-Then-Else）。

**`$type`: `"If"`**

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `ConditionType` | enum | — | 條件判斷類型 |
| `LeftOperand` | string | `""` | 左運算元（傳統模式用），支援 `{變數}` |
| `RightOperand` | string | `""` | 右運算元（傳統模式用），支援 `{變數}` |
| `ConditionExpression` | string | `""` | 自由格式條件表達式（僅 `Expression` 類型使用） |
| `ThenActions` | Action[] | `[]` | 條件為 true 時執行的指令清單 |
| `ElseActions` | Action[] | `[]` | 條件為 false 時執行的指令清單（可為空） |
| `Children` | Action[] | `[]` | 繼承自 ContainerAction，此指令中不直接使用 |

**ConditionType 可選值**：

| 值 | 使用方式 | 說明 |
|---|---|---|
| `VariableEquals` | `LeftOperand == RightOperand` | 值相等 |
| `VariableNotEquals` | `LeftOperand != RightOperand` | 值不相等 |
| `VariableGreaterThan` | `LeftOperand > RightOperand` | 數值大於 |
| `VariableLessThan` | `LeftOperand < RightOperand` | 數值小於 |
| `VariableContains` | `LeftOperand contains RightOperand` | 文字包含 |
| `FileExists` | `LeftOperand` 的檔案路徑存在 | 檢查檔案是否存在 |
| `ImageExists` | *(保留)* | 檢查影像是否存在 |
| `Expression` | 使用 `ConditionExpression` 自由格式 | 使用表達式解析器直接計算 |

#### 範例：變數比較（傳統模式）

```json
{
  "$type": "If",
  "Name": "檢查計數器",
  "ConditionType": "VariableGreaterThan",
  "LeftOperand": "{counter}",
  "RightOperand": "5",
  "ThenActions": [
    {
      "$type": "Type",
      "Name": "輸出提示",
      "Text": "計數已超過 5"
    }
  ],
  "ElseActions": [
    {
      "$type": "Type",
      "Name": "輸出提示",
      "Text": "計數未超過 5"
    }
  ]
}
```

#### 範例：自由格式表達式（推薦）

```json
{
  "$type": "If",
  "Name": "檢查條件",
  "ConditionType": "Expression",
  "ConditionExpression": "{counter} >= 10",
  "ThenActions": [
    {
      "$type": "Keyboard",
      "Name": "按 ESC 退出",
      "Key": "ESC"
    }
  ],
  "ElseActions": []
}
```

#### 範例：檢查檔案是否存在

```json
{
  "$type": "If",
  "Name": "檢查檔案",
  "ConditionType": "FileExists",
  "LeftOperand": "C:\\Data\\output.csv",
  "ThenActions": [
    {
      "$type": "Type",
      "Name": "提示檔案已存在",
      "Text": "檔案已找到！"
    }
  ],
  "ElseActions": []
}
```

---

## 6. 視覺辨識指令

### 6.1 FindImage — 尋找影像

在螢幕上搜尋指定的範本影像，找到後可自動點擊或儲存座標。

**`$type`: `"FindImage"`**

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `TemplateImagePath` | string | `""` | 範本影像的檔案路徑（必填） |
| `Threshold` | double | `0.8` | 匹配門檻值（0.0 ~ 1.0），越高越嚴格 |
| `TimeoutMs` | int | `30000` | 搜尋逾時時間（毫秒） |
| `IntervalMs` | int | `500` | 每次搜尋間隔（毫秒） |
| `ClickWhenFound` | bool | `false` | 找到後是否自動點擊該位置 |
| `SaveToVariable` | string | `""` | 將找到的座標存入變數前綴名 |

> **變數儲存**：當 `SaveToVariable` 設為 `"btnPos"` 時，系統會自動建立：
> - `btnPos_X` — 影像的 X 座標
> - `btnPos_Y` — 影像的 Y 座標  
> - `btnPos_Confidence` — 匹配信心度

#### 範例

```json
{
  "$type": "FindImage",
  "Name": "尋找確認按鈕",
  "TemplateImagePath": "images/confirm_button.png",
  "Threshold": 0.85,
  "TimeoutMs": 10000,
  "IntervalMs": 300,
  "ClickWhenFound": true,
  "SaveToVariable": "confirmBtn"
}
```

> **提示**：在 `.aws` 封裝中，圖片會儲存在 `images/` 目錄內。`TemplateImagePath` 可使用相對路徑如 `"images/xxx.png"`。

---

### 6.2 OCR — 文字辨識

擷取螢幕畫面並進行光學字元辨識（OCR），可辨識全螢幕或指定區域的文字。

**`$type`: `"OCR"`**

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `RegionX` | int? | `null` | 辨識區域的 X 起始座標（留空 = 全螢幕） |
| `RegionY` | int? | `null` | 辨識區域的 Y 起始座標 |
| `RegionWidth` | int? | `null` | 辨識區域的寬度 |
| `RegionHeight` | int? | `null` | 辨識區域的高度 |
| `SearchText` | string? | `null` | 要搜尋的特定文字（留空 = 辨識所有文字） |
| `UseRegex` | bool | `false` | 搜尋時是否使用正規表達式 |
| `SaveToVariable` | string | `""` | 將辨識結果存入變數名 |
| `Language` | string | `"chi_tra"` | OCR 語言代碼 |

**常用語言代碼**：

| 代碼 | 語言 |
|---|---|
| `chi_tra` | 繁體中文 |
| `chi_sim` | 簡體中文 |
| `eng` | 英文 |
| `jpn` | 日文 |

> **變數儲存**：當 `SaveToVariable` 設為 `"ocrResult"` 時，系統會自動建立：
> - `ocrResult` — 辨識出的文字內容
> - `ocrResult_Confidence` — 辨識信心度

#### 範例：辨識特定區域

```json
{
  "$type": "OCR",
  "Name": "讀取價格",
  "RegionX": 200,
  "RegionY": 150,
  "RegionWidth": 100,
  "RegionHeight": 30,
  "SaveToVariable": "priceText",
  "Language": "eng"
}
```

#### 範例：搜尋特定文字

```json
{
  "$type": "OCR",
  "Name": "搜尋錯誤訊息",
  "SearchText": "Error",
  "SaveToVariable": "errorResult",
  "Language": "eng"
}
```

---

## 7. 變數與表達式系統

### 變數語法

在任何支援表達式的屬性中，使用 `{變數名}` 引用變數：

```
{username}     → 取得 username 變數的值
{counter}      → 取得 counter 變數的值
{_loopIndex}   → 取得迴圈索引（內建，從 0 開始）
```

### 算術運算

支援基本四則運算：

```
{count} + 1      → 加法
{total} - 5      → 減法
{price} * 2      → 乘法
{value} / 10     → 除法
{num} % 3        → 取餘數
```

### 條件表達式

用於 `If` 指令的 `ConditionExpression` 和 `Loop` 的 `WhileCondition`：

```
{counter} > 5         → 大於
{counter} < 10        → 小於
{counter} >= 5        → 大於或等於
{counter} <= 10       → 小於或等於
{status} == done      → 等於（字串比較）
{status} != error     → 不等於
{text} contains hello → 文字包含
```

### 字串拼接

變數可直接嵌入文字中：

```
Hello {name}, welcome!      → "Hello John, welcome!"
C:\Output\file_{counter}.png → "C:\Output\file_3.png"
```

---

## 8. 錯誤處理策略

每個指令都有一個 `ErrorPolicy` 物件，控制失敗時的行為：

| 屬性 | 類型 | 預設值 | 說明 |
|---|---|---|---|
| `RetryCount` | int | `0` | 失敗時的重試次數（`0` = 不重試） |
| `RetryIntervalMs` | int | `1000` | 每次重試之間的等待時間（毫秒） |
| `ContinueOnError` | bool | `false` | 失敗後是否繼續執行下一個指令 |
| `JumpToLabel` | string? | `null` | *(保留)* 失敗時跳轉的標籤 |

#### 範例：重試 3 次、失敗後繼續

```json
{
  "$type": "FindImage",
  "Name": "尋找按鈕（容錯）",
  "TemplateImagePath": "images/button.png",
  "ClickWhenFound": true,
  "ErrorPolicy": {
    "RetryCount": 3,
    "RetryIntervalMs": 2000,
    "ContinueOnError": true
  }
}
```

---

## 9. 完整範例腳本

請參考專案中的範例檔案：

- **範例 script.json**：`docs/sample-script.json`  
  — 包含所有 10 種指令類型的完整範例
- **範例 .aws 檔案**：可透過 AutoWizard 載入 `docs/sample-script.json` 後另存為 `.aws`

> **提示**：載入範例腳本後，可在編輯器中查看每個指令的屬性設定，作為撰寫自己腳本的參考。
