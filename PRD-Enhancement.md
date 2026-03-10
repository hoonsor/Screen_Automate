# PRD: AutoWizard 功能增修計畫 (基於 QMacro 2014 分析與使用者回饋)

## 1. 專案背景與目標
本文件基於對經典自動化軟體 **QMacro 2014** 的分析以及 **使用者回饋 (2026/02/15)**，提出 **AutoWizard** 的功能增修計畫。目標是補足功能缺口，提升錄製與回放的精確度，並優化使用者體驗。

## 2. 核心架構原則
1.  **維持 BaseAction 多型設計**：新功能需相容於現有執行引擎。
2.  **低階輸入整合**：錄製引擎需支援 KeyDown/KeyUp 與 MouseDown/MouseUp 以處理拖曳與組合鍵。
3.  **現代化 UI/UX**：提供深色模式、程式碼檢視、智慧截圖等現代化功能。

---

## 3. 功能落差與需求分析 (Gap Analysis)

| 功能領域 | QMacro/使用者需求 | AutoWizard (現狀) | 缺口嚴重度 | 建議優先順序 |
| :--- | :--- | :--- | :--- | :--- |
| **輸入錄製** | 支援 **拖曳 (Drag)**、**組合鍵 (Ctrl+C)** | 僅支援 Click/Type，無法記錄按住不放 | 🔴 極高 | **P0 (Bug Fix)** |
| **視窗控制** | 針對特定視窗操作 (Handle) | 僅能全域座標點擊 (桌面模式易失敗) | 🔴 極高 | **P0** |
| **截圖/找圖** | **智慧截圖 (F11)**：變暗+框選+自動生成指令 | 僅能手動截圖後再設定路徑 | 🟠 高 | **P1** |
| **顏色辨識** | **取色工具**：放大鏡+十字準心，自動帶入 If 條件 | 無 | 🟠 高 | **P1** |
| **邏輯控制** | If 指令可加入任意子指令 | UI 限制僅能加 Click | 🔴 極高 | **P0 (Bug Fix)** |
| **人機模擬** | **模擬人類**：自動加入隨機延遲 | 無 (固定延遲) | 🟡 中 | **P2** |
| **IDE 功能** | 程式碼模式 (Code View)、全域熱鍵 | 僅圖形化介面 | 🟡 中 | **P2** |

---

## 4. 增修功能詳細規格 (Implementation Plan)

### 階段一：核心 Bug 修復與錄製優化 (Core Fixes) - P0

#### 4.1 低階輸入錄製 (Low-Level Input)
**問題**：無法錄製 Ctrl+C、拖曳動作。
**解法**：
- 修改 `Recorder.cs`，不再只偵測 `Click` 與 `KeyPress`。
- 新增 `InputEvent` 類型：`MouseDown`, `MouseUp`, `KeyDown`, `KeyUp`。
- 錄製後處理 (Post-processing)：
    - 將短時間內的 Down+Up 合併為 Click/Press。
    - 將長時間的 Down...Move...Up 識別為 **Drag** 指令 (或保持原始 Down/Move/Up 序列)。

#### 4.2 檔案存取與 UI 修正
**問題**：儲存後標題未更新、重新開啟無法載入腳本、桌面全螢幕回放失敗。
**解法**：
- 修正 `MainWindowViewModel` 的 `Title` 綁定與更新邏輯。
- 確保 `AwsPackage` 序列化包含正確的 Type Discriminator。
- 修正 `EditorViewModel` 在載入時的 UI 更新通知 (`FlattenedNodes`)。
- 增加 `Desktop` 模式下的視窗激活檢查。

#### 4.3 UI 屬性優化
- **布林值下拉選單**：PropertyGrid 中的 `bool` 屬性改用 ComboBox (True/False) 顯示。
- **If 指令容器化**：修正 UI 邏輯，允許將 *任何* 指令拖曳或新增至 `IfAction` 的 `Then/Else`區塊。
- **複製功能**：新增 `Ctrl+C` 快捷鍵與「複製」按鈕。

---

### 階段二：智慧工具與互動增強 (Smart Tools) - P1

#### 4.4 智慧截圖 (Smart Capture - F11)
**流程**：
1.  錄製時按下 `F11`。
2.  螢幕變暗 (Overlay Window)，滑鼠游標變為十字。
3.  使用者框選區域 (區域內變亮)。
4.  放開滑鼠 -> 自動裁切並儲存圖片至 `Images` 資料夾。
5.  自動生成 `FindImageAction` + `ClickAction` (點擊該圖中心)。
6.  恢復錄製。

#### 4.5 取色工具 (Color Picker)
**位置**：`IfAction` 的屬性面板中。
**功能**：
- 點擊「取色」按鈕 -> 開啟放大鏡 Overlay。
- 顯示滑鼠周邊像素放大圖 + 當前 RGB/Hex 值。
- 點擊 -> 自動填入 `TargetColor` 欄位。

#### 4.6 應用程式熱鍵 (App Hotkeys)
- **錄製/停止**：`Ctrl+F9`
- **播放/停止**：`Ctrl+F10`
- **檔案操作**：`Ctrl+S` (存檔), `Ctrl+Shift+S` (另存), `Ctrl+O` (開啟)

---

### 階段三：進階模擬與編輯 (Advanced Simulation) - P2

#### 4.7 擬人化模擬 (Human Simulation)
**屬性**：全域設定或單一指令屬性 `SimulateHuman` (bool)。
**行為**：
- 在指令間自動插入 `Random(50ms, 200ms)` 的延遲。
- 滑鼠移動軌跡加入些微隨機偏移 (Bezier 曲線)。

#### 4.8 程式碼檢視 (Code View)
**功能**：
- 新增 `Code` 分頁，顯示腳本的 JSON 或類似 VBS 的虛擬碼。
- 允許直接編輯文字 -> 解析回物件模型。

#### 4.9 視覺化狀態指示
- 腳本名稱旁顯示狀態燈號：
    - 🟢 綠燈：已儲存 (Saved)
    - 🔴 紅燈：有修改 (Dirty)

---

## 5. 執行路徑建議 (Roadmap)

1.  **Week 1**: 修復 Save/Load Bug、If 容器限制、UI 布林選單。
2.  **Week 2**: 重構 Recorder 支援 Drag/Combo Keys (Low-Level Events)。
3.  **Week 3**: 實作智慧截圖 (F11) 與取色工具。
4.  **Week 4**: 實作擬人化模擬與程式碼檢視。
