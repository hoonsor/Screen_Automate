---
description: Build and test AutoWizard Desktop solution
---

# Build & Test AutoWizard Desktop

> **重要**: 由於系統 PATH 中 x86 dotnet.exe 優先被解析，但 x86 版本沒有安裝 SDK，
> 必須使用 x64 完整路徑來執行 dotnet 指令。

## 1. Build the solution
// turbo
```
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; & "C:\Program Files\dotnet\dotnet.exe" build AutoWizard.sln 2>&1 | Out-String
```
Working directory: `d:\01-Project\01-Screen Automate`

## 2. Run all tests
// turbo
```
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; & "C:\Program Files\dotnet\dotnet.exe" test AutoWizard.Tests/AutoWizard.Tests.csproj --logger "console;verbosity=detailed" 2>&1 | Select-Object -Last 20
```
Working directory: `d:\01-Project\01-Screen Automate`

## 3. Check warnings/errors only
// turbo
```
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; & "C:\Program Files\dotnet\dotnet.exe" build AutoWizard.sln 2>&1 | Select-String -Pattern "error|warning|Error|Warning" | Out-String
```
Working directory: `d:\01-Project\01-Screen Automate`
