# Implementation Plan - Phase 1: Critical Bug Fixes & UX Polishing

This plan addresses the immediate bug reports and high-priority UX improvements requested by the user.

## User Review Required

> [!IMPORTANT]
> **Save/Load Logic Change**: I will modify `AwsPackage` serialization settings to ensure `BaseAction` polymorphism is correctly handled. This might affect backward compatibility with manually modified JSON files if they lack `$type`.

> [!NOTE]
> **"If" Action Behavior**: I will modify the Toolbox behavior. When an `IfAction` (or any container) is selected, clicking a Toolbox button will now append the action *inside* the container, instead of at the root level.

## Proposed Changes

### 1. Fix Save/Load & Title Update

#### [MODIFY] [MainWindowViewModel.cs](file:///d:/01-Project/01-Screen%20Automate/AutoWizard.UI/ViewModels/MainWindowViewModel.cs)
- Update `OnSaveScript` to explicitly set `Title` property after saving.
- Update `OnOpenScript` to ensure `EditorViewModel.Actions` population triggers UI updates.
- Verify `EditorViewModel.ScriptName` update logic.

#### [MODIFY] [EditorViewModel.cs](file:///d:/01-Project/01-Screen%20Automate/AutoWizard.UI/ViewModels/EditorViewModel.cs)
- Ensure `FlattenedNodes` is rebuilt correctly when `Actions` are cleared and re-added during `OnOpenScript`.
- Implement `IsDirty` tracking to update the UI state (Red/Green indicators).

### 2. UI/Property Editor Improvements

#### [MODIFY] [PropertiesPanel.xaml](file:///d:/01-Project/01-Screen%20Automate/AutoWizard.UI/Views/PropertiesPanel.xaml) (and ViewModel)
- Change `IsEnabled` (bool) property editor from `TextBox` to `ComboBox`.
- Apply this pattern to other boolean properties (e.g., `CaptureFull` in Screenshot).

#### [MODIFY] [MainWindowViewModel.cs](file:///d:/01-Project/01-Screen%20Automate/AutoWizard.UI/ViewModels/MainWindowViewModel.cs)
- Modify `OnAddAction(string actionType)`:
    - Check `EditorViewModel.SelectedNode`.
    - If selected node is a Container (`If`, `Loop`), add the new action as a child of that container.
    - If not, add to root (current behavior).

### 3. Copy/Paste Validation

#### [VERIFY] [MainWindow.xaml](file:///d:/01-Project/01-Screen%20Automate/AutoWizard.UI/Views/MainWindow.xaml)
- Confirm `Ctrl+C` binding and "Copy" button visibility.
- *Note*: User claimed "Copy" button is missing or needed. I will check if the existing button is visible and functional.

## Verification Plan

### Automated Tests
- Run `AutoWizard.Tests` to ensure serialization logic is intact.

### Manual Verification
1.  **Save/Load Cycle**:
    -   Open App -> Add random actions.
    -   Save as `TestBugFix.aws`.
    -   **Check**: Window title updates to `TestBugFix - AutoWizard Desktop`.
    -   **Check**: Status bar shows "Saved".
    -   Close App -> Reopen -> Open `TestBugFix.aws`.
    -   **Check**: Actions appear in the list.
    -   **Check**: Window title is correct.

2.  **Container Logic**:
    -   Add `If` action -> Select it.
    -   Click `Type` in Toolbox.
    -   **Check**: `Type` action appears *inside* the `If` block (indentation).

3.  **Property Editor**:
    -   Select any action.
    -   **Check**: `IsEnabled` field shows a Dropdown (True/False).

4.  **Copy/Paste**:
    -   Select action -> Press `Ctrl+C`.
    -   Select another location -> Press `Ctrl+V`.
    -   **Check**: Action is duplicated.
