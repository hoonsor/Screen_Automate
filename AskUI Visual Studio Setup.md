# Chat Conversation

Note: _This is purely the output of the chat conversation and does not contain any raw data, codebase snippets, etc. used to generate the output._

### User Input

# 產品需求文件 (PRD): AskUI Visual Studio (在地化積木編輯器)

## 1. 專案願景

打造一個隱私優先（Privacy-First）且完全在地化運行的桌面端自動化工具。結合 AskUI 的 AI 視覺能力與 Blockly 的直覺編輯體驗，讓使用者能安全地錄製「座標精確」或「AI 韌性」兩種模式的自動化腳本，並自動優化滑鼠路徑。

## 2. 目標用戶

- **RPA 專員**：偏好使用可視化邏輯（積木）建構流程。
- **對隱私敏感的企業**：要求所有數據與截圖不得離開本機環境。
- **初級測試工程師**：需要簡單的介面來快速產出穩定的自動化腳本。
    
## 3. 核心功能與錄製模式

### 3.1. 雙軌錄製模式 (Dual Recording Modes)

- **座標錄製模式 (Coordinate Mode)**：
    - 紀錄精確的畫素座標 (X, Y)。
    - 適用於介面絕對不會變動的固定環境，反應速度最快。
        
- **人眼辨識模式 (Inference Mode)**：
    - 使用 AskUI AI 模型辨識 UI 元素（文字、圖示、按鈕）。
    - 自動標註元素邊框 (Inference Overlays)，讓使用者確認 AI 抓取位置。
    - 具備高度韌性，即使按鈕位置微調也能正確執行。
        

### 3.2. 可視化積木編輯器 (Blockly Workspace)

- **自動生成積木**：錄製完成後，動作會自動轉換為對應的 Blockly 積木塊。
- **邏輯控制**：支持「迴圈」、「條件判斷」與「變數」，可在積木中直接編輯。
- **API 映射**：每一個積木對應一個 `askui` 指令（例如 `aui.click().text('登入').exec()`）。
    

### 3.3. 軌跡優化與平滑處理

- **關鍵幀 (Keyframe) 提取**：從原始滑鼠路徑中提取轉折點，去除冗餘數據。
- **平滑演算法**：使用貝茲曲線（Bezier Curve）優化移動軌跡，模擬更自然的人類操作，減少被反自動化偵測的風險。

## 4. 使用者旅程 (User Journeys)

1. **選擇模式**：使用者在介面上切換「座標」或「AI 辨識」模式。
2. **啟動錄製**：透明覆蓋層啟動，即時顯示 AI 辨識到的綠色框線 (Inference Overlays)。
3. **執行動作**：使用者在受控視窗點擊，系統捕捉畫面與動作。
4. **積木生成**：停止錄製後，主介面自動填充對應的動作積木塊。
5. **本地驗證**：使用者點擊「播放」，系統啟動本地執行引擎，完全不連網。
    

## 5. 功能分解 (Functional Requirements)

| 功能模組                   | 描述                            | 優先級 |
| ---------------------- | ----------------------------- | --- |
| **Tauri 桌面外殼**         | 提供系統權限存取與高效率截圖。               | P0  |
| **雙模式切換器**             | 支持在錄製過程中即時切換座標或 AI 模式。        | P0  |
| **Blockly 集成**         | 定義專屬 `askui` 積木庫，支持從代碼反向生成積木。 | P0  |
| **Inference Overlays** | 錄製時在螢幕上即時繪製辨識框（畫框回饋）。         | P0  |
| **軌跡優化引擎**             | 在錄製結束後自動進行平滑化與關鍵幀處理。          | P1  |
| **在地化處理引擎**            | 確保截圖與辨識邏輯皆在本地 CPU/GPU 運行。     | P0  |
| **本地存儲管理**             | 腳本以專案資料夾形式儲存於本地 Disk，支援匯出。    | P1  |

## 6. 技術規格

- **Frontend**: Next.js + Tailwind CSS + **Blockly**.
- **Desktop Runtime**: **Tauri (Rust)** - 負責調用系統 API (Screenshots, Mouse events)。
- **AI Inference**: **Local AskUI Node** - 在本地環境啟動 AskUI 服務。
- **Trajectory Logic**: Rust 實現的數值平滑處理演算法（確保效能）。
- **Storage**: 本地 JSON / TypeScript 檔案。
    

## 7. 非功能性需求與安全性

- **完全離線**：禁止任何自動上傳截圖的行為。
- **效能控制**：Inference Overlays 繪製延遲需控制在 300ms 內，確保錄製感官同步。
- **高精確度**：座標錄製需兼容不同 DPI 縮放設定。
    

## 8. 邊界情況

- **遮擋處理**：當 GUI 覆蓋層啟動時，需確保不阻擋底層視窗的事件穿透（Click-through）。
- **座標偏置**：在多螢幕、不同縮放率下的座標自動校準。

*Listed directory [01-Screen Automate](file:///d:/01-Project/01-Screen%20Automate) *

*Edited relevant file*

*Edited relevant file*

### User Input

批准

*Edited relevant file*

*User accepted the command `npx create-tauri-app@latest . --manager npm --template next-ts -y`*

*User accepted the command `npm -v`*