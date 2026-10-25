# 01-Screen Automate

> **版本號：** `v0.1.0`
> **最後更新：** 2026-04-14
> **GitHub：** [https://github.com/hoonsor/Screen_Automate.git](https://github.com/hoonsor/Screen_Automate.git)

---

## 📖 專案程式功能概述

First, run the development server:

---

## 📋 版本歷程及功能改變紀錄

| 版本 | 日期 | 類型 | 變更說明 |
|------|------|------|----------|
| v0.1.1 | 2026-10-25 | fix | 測試不再送出真實鍵盤滑鼠輸入（IInputSimulator＋測試預設只記錄＋Interactive 預設排除）；起因：CallModuleActionTests 把 OverriddenText 打進使用者輸入框。程式主線已移到 51-GestureWizard，本專案僅做此安全修正；另有 4 個 SetVariable 型別測試失敗未修（51 號 v0.1.0 已修） |
| v0.1.0 | 2026-03-16 | feat | remove spaces between Chinese characters for better matching accuracy |
| v0.1.0 | 2026-03-15 | chore | Docs: Add session memory for March 15 and initialize Issues Log with RCA. |
| v0.1.0 | 2026-03-15 | chore | Major Refactor: Replace Tesseract with Windows.Media.Ocr & UI Enhancements suite. Fixed UI deadlock, added variable/history panels, and upgraded project target frameworks. |
| v0.1.0 | 2026-03-14 | chore | Enhance Export: Single-file EXE support, embedded AWS package, fixed image path resolution, added Runner log export, and fixed magnifier crosshair offset |
| v0.1.0 | 2026-03-13 | feat | implement multi-condition IfAction with boolean expression support |

---

## 🎯 目前專案進度進展狀態

### 主要任務

- [ ] 待定義主要任務

### 次要任務

- [ ] 待定義次要任務

---

## 📝 歷次修訂紀錄

### 當前方向

請在此描述目前的專案發展方向。

### 歷史修訂

<!-- 
當專案方向改變時，使用以下格式記錄：

> [!NOTE]
> **已棄用方向 (YYYY-MM-DD)**
> ~~原先計畫的方向或任務描述~~
> **棄用原因：** 簡述為何改變方向
-->

_尚無歷史修訂紀錄_
