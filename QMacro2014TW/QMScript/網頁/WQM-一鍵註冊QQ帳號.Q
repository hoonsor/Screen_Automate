[General]
SyntaxVersion=2
BeginHotkey=121
BeginHotkeyMod=0
PauseHotkey=0
PauseHotkeyMod=0
StopHotkey=123
StopHotkeyMod=0
RunOnce=1
EnableWindow=
MacroID=30832d0b-dce2-4f09-9c4d-a223522d49b8
Description=WQM-一鍵註冊QQ帳號
Enable=0
AutoRun=0
[Repeat]
Type=0
Number=1
[SetupUI]
Type=2
QUI=
[Relative]
SetupOCXFile=
[Comment]
【功能說明】
一鍵註冊QQ帳號
【使用說明】
驗證碼輸入需要手動輸入,再按回車鍵後即可完成註冊
【作者介紹】
兄弟工程師01
【製作日期】
2009.6.24

[Script]
Delay 1000
Call Plugin.Web.Bind("wqm.exe")
//<>===================================================打開網頁免費申請
Call Plugin.Web.Tips("打開網頁免費申請網頁")
Call Plugin.Web.Go("http://zc.qq.com/chs/index.html")
Call Plugin.Web.SetSize(913,850)
Delay 1000
//<>===================================================輸入名字
Call Plugin.Web.LeftClick(450,160)
Delay 500
Call Plugin.Web.HtmlInput("name", "id:nick")
//<>===================================================輸入密碼
Call Plugin.Web.LeftClick(450,215)
Delay 500
Call Plugin.Web.Tips("輸入密碼，默認密碼：a111111")
Call Plugin.Web.KeyPress(65)
For 6
    Call Plugin.Web.KeyPress(49)
Next 
Delay 500
//<>===================================================確認密碼
Call Plugin.Web.LeftClick(450,265)
Delay 500
Call Plugin.Web.KeyPress(65)
For 6
    Call Plugin.Web.KeyPress(49)
Next 
Delay 500
//<>===================================================輸入生日-年
Call Plugin.Web.Tips("輸入生日")
Delay 500
Call Plugin.Web.LeftClick(515,350)
Delay 500
For 3
    Call Plugin.Web.KeyPress(40)
Next 
Delay 500
//<>===================================================輸入生日-月
Call Plugin.Web.LeftClick(610,350)
Delay 500
For 3
    Call Plugin.Web.KeyPress(40)
Next 
Delay 500
//<>===================================================輸入生日-日
Call Plugin.Web.LeftClick(670,350)
Delay 500
For 3
    Call Plugin.Web.KeyPress(40)
Next 
Delay 500
//<>==================================================輸入驗證碼
Call Plugin.Web.LeftClick(480,445)
Call Plugin.Web.Tips("等待輸入驗證碼,結束按回車鍵")
Delay 500
Rem 等待
key=WaitKey()
//<>===================================================按回車鍵即可提交申請
If key=13 Then
    Goto 離開
End If 
Goto 等待
Rem 離開
//<>===================================================確定並同意以下條款
Call Plugin.Web.Tips("註冊提交")
Call Plugin.web.HtmlClick("id:submit")
Delay 500
EndScript 
Sub OnScriptExit()
    Call Plugin.Web.Tips("腳本停止運行")
End Sub 
