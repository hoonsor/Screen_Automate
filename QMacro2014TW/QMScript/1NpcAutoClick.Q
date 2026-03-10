[General]
SyntaxVersion=2
BeginHotkey=97
BeginHotkeyMod=2
PauseHotkey=0
PauseHotkeyMod=0
StopHotkey=96
StopHotkeyMod=2
RunOnce=1
EnableWindow=
MacroID=ca7f9da4-33d6-415b-bfa9-70bf69fe0673
Description=1多NPC智能選擇執行動作
Enable=1
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

[Script]
' 9z魔王 按鍵精靈 教學
' YouTube簡址 https://www.youtube.com/@9z魔王
' FB社團 https://www.facebook.com/groups/1262368885516025

Dim SS搜圖目錄, SSNPC互動項目數, SSNPC項目基準點X, SSNPC項目基準點Y, SSNPC項目記憶點X, SSNPC項目記憶點Y ,SS互動人數
SS搜圖目錄 = "絕世好武功"

Rem 重新開始
Delay 500
'Call 傳授武功一人(5)
'Call 讀檔刷入門禮()
'Call 快速存檔()
'Call 商船存取檔案刷銅幣(100)
'Call NPC自動互動(1450, 540, 2, 1, 5)
'Call NPC自動互動(1455, 715, 2, 4, 1)

Call 多NPC自動互動(2, 5, 1, 2, 500)
Call 快速存檔()
MessageBox "傳授 " & SS互動人數 & " 人, 自動存檔完成"

Sub 多NPC自動互動(zz多NPC互動類別, zz多NPC互動選項1, zz多NPC互動選項2, zz多NPC互動次數, zz多NPC互動延遲)
SSNPC項目記憶點Y = 0
For zz多NPC互動次數
FindColorEx 1241,499,1504,839,"EDEDED",2,1.0,intX,intY
If intX > 0 And intY > 0 Then 
	If SSNPC項目記憶點Y = 0 Then 
		SSNPC項目記憶點X = intX
		SSNPC項目記憶點Y = intY
	End If
	SS互動人數 = Round((SSNPC項目記憶點Y - 523.5) / 42.5)
	'MessageBox SSNPC項目記憶點X & " " & SSNPC項目記憶點Y & " " & SS互動人數
	For i = 1 To SS互動人數
		FindColorEx 1241, 499, 1504, 839, "EDEDED", 2, 1.0, intX, intY
		If intX > 0 And intY > 0 Then : If SSNPC項目記憶點Y <> intY Then : MessageBox "NPC人數變動 停止互動" : Exit Sub : End If : End If
		Call NPC自動互動(SSNPC項目記憶點X + 80, SSNPC項目記憶點Y - 27 - (i - 1) * 85, zz多NPC互動類別, zz多NPC互動選項1, zz多NPC互動選項2)
		Call 自動移動點擊(1, 1065, 302 , 1, 200)
		Call 自動移動點擊(1, 1766, 910, 1, 200)
		Delay zz多NPC互動延遲
	Next
Else 
MessageBox "找不到NPC對話項目 停止互動"
Exit Sub
End If
Next
End Sub


Sub NPC自動互動(zzNPC名稱位置X, zzNPC名稱位置Y, zzNPC互動類別, zzNPC互動選項1, zzNPC互動選項2)
	Call 等待圖示(3, zzNPC名稱位置X, zzNPC名稱位置Y, 1, 200, zzNPC名稱位置X - 175, zzNPC名稱位置Y - 45, zzNPC名稱位置X + 65, zzNPC名稱位置Y + 45, "F", 100)
	Select Case zzNPC互動類別
		Case 1'商船
		Case 2 , 3'NPC
			SSNPC互動項目數 = 0
			Call 等待圖示(81, 0, 0, 1, 200, 1100, 601, 1271, 916, "委託鍛造-銅幣", 3)
			Call 等待圖示(81, 0, 0, 1, 200, 986, 566, 1270, 911, "偷竊-憤怒圖示暗", 10)
	End Select
	Select Case zzNPC互動類別
		Case 2'NPC動作
			Call 自動移動點擊(1, SSNPC項目基準點X + 100, SSNPC項目基準點Y + zzNPC互動選項1 * 60, 1, 300)
			Call 自動移動點擊(1, SSNPC項目基準點X - 100, SSNPC項目基準點Y + (zzNPC互動選項1 + zzNPC互動選項2 - 1) * 60, 1, 300)
		Case 3
			Call 自動移動點擊(1, SSNPC項目基準點X + 100, SSNPC項目基準點Y + zzNPC互動選項1 * 60, 1, 300)
			Call 自動移動點擊(1, SSNPC項目基準點X - 100, SSNPC項目基準點Y + zzNPC互動選項2 * 60, 1, 300)
	End Select
End Sub


'Call 等待圖示(移動類型, 座標X, 座標Y, 點擊次數, 等待毫秒, 搜圖左上X, 左上Y, 右下X, 右下Y, 搜圖檔名, 搜圖次數)
'Call 等待圖示(2, , , 1, 200, , , , , "搜圖檔名", 100)
Sub 等待圖示(zz等待圖示類型, zz等待圖示移動X, zz等待圖示移動Y, zz等待圖示點擊次數, zz等待圖示點擊延遲, zz搜圖左上X, zz搜圖左上Y, zz搜圖右下X, zz搜圖右下Y, zz搜圖檔名,zz搜圖次數 )
	For zz搜圖次數
	Select Case zz等待圖示類型
		Case 3, 4 '移動滑鼠
		MoveTo zz等待圖示移動X, zz等待圖示移動Y
		Case 51 '滑鼠移動 + ESC 3下
		MoveTo zz等待圖示移動X, zz等待圖示移動Y
		KeyPress "Esc", 3 : Delay 500
	End Select
	Delay 100
	FindPic zz搜圖左上X, zz搜圖左上Y, zz搜圖右下X, zz搜圖右下Y, "C:\Program Files (x86)\按鍵精靈2014\PIC\" & SS搜圖目錄 & "\" & zz搜圖檔名 & ".bmp", 0.9, zz等待圖示X, zz等待圖示Y
	If zz等待圖示X > 0 And zz等待圖示Y > 0 Then 
	Select Case zz等待圖示類型
		Case 1, 3 ,51 '移動 絕對 座標
		Call 自動移動點擊(1, zz等待圖示移動X, zz等待圖示移動Y, zz等待圖示點擊次數, zz等待圖示點擊延遲)
		Case 2, 4'移動 相對 座標
		Call 自動移動點擊(1, zz等待圖示X + zz等待圖示移動X, zz等待圖示Y + zz等待圖示移動Y, zz等待圖示點擊次數, zz等待圖示點擊延遲)
		Case 81'計算
		If SSNPC互動項目數 = 0 Then : SSNPC互動項目數 = Round((zz等待圖示Y - 504)/30) : SSNPC項目基準點Y = zz等待圖示Y - SSNPC互動項目數 * 60 : End If
		SSNPC項目基準點X = zz等待圖示X
		Case 99 '重新開始
		Goto 重新開始
		MessageBox "收到禮物 " & zz搜圖檔名
	End Select
	Exit Sub
	End If
	Next
	If zz搜圖次數 > 99 Then 
		MessageBox "找不到圖示(" & zz搜圖檔名 & "),搜尋次數: " & zz搜圖次數 & " 約等待時間: " & zz搜圖次數 / 10 & " 秒"
		EndScript
	End If
End Sub

'移動類型: 1.移動絕對座標點左鍵 2...... 3.  4.
'Call 自動移動點擊(移動類型, 座標X, 座標Y, 點擊次數, 等待毫秒)
'Call 自動移動點擊(1, , , 1, 100)
Sub 自動移動點擊(zz自動移動類型, zz自動移動點擊X, zz自動移動點擊Y, zz自動移動點擊次數, zz自動點擊延遲)
	Select Case zz自動移動類型
		Case 1, 3	'移動 絕對 座標
		MoveTo zz自動移動點擊X, zz自動移動點擊Y : Delay 100
		Case 2, 4	'移動 相對 座標
		MoveR zz自動移動點擊X, zz自動移動點擊Y : Delay 100
	End Select
	For zz自動移動點擊次數
	Select Case zz自動移動類型
		Case 1, 2'左鍵
		LeftClick 1
		Delay zz自動點擊延遲
		Case 3, 4'右鍵
		RightClick 1
		Delay zz自動點擊延遲
	End Select
	Next
End Sub

Sub 快速存檔()
Call 等待圖示(51, 1511, 295, 1, 200, 1248, 256, 1342, 333, "F", 100)' 點選快速存檔
Call 等待圖示(1, 1511, 295, 0, 200, 1248, 256, 1342, 333, "F", 100)' 存檔完成
End Sub

Sub 讀取檔案(zz檔案順序,zz讀檔等待秒數,zz訊息開關)
Call 等待圖示(51, 1485, 227, 1, 200, 1252, 183, 1338, 266, "F", 100) '存取檔案
Call 等待圖示(1, 182, 36 + zz檔案順序 *155 , 1, 200, 143,19,255,81, "存檔標籤", 100) '檔案順序
Call 等待圖示(2, 27, 12, 1, 200, 619, 669, 731, 1062, "讀檔-讀取按鈕", 100) '讀取按鈕
Call 等待圖示(1, 1760, 910, 1, 200, 944, 293, 978, 326, "鈴鐺-錘心", 100)'確定讀檔
Delay zz讀檔等待秒數 * 1000 '讀檔等待時間
Call 等待圖示(2, 0, 0, 1, 200, 885, 1033, 1036, 1076, "世界載入完成", 200)'載入完成 點任意健
Delay 5000 '進入世界等待時間
Call 等待圖示(2, 0, 0, 0, 200, 0, 956, 422, 1074, "大地圖-左下任務圖示", 100)
If zz訊息開關 = 1 Then : MessageBox "讀取檔案 " & zz檔案順序 & "完成" : End If
End Sub

Sub 讀檔刷入門禮()
Call 讀取檔案(1, 50, 0)
Call 等待圖示(1, 758, 923, 1, 500, 216, 227, 344, 276, "入門送禮", 100)'點選多謝
Call 自動移動點擊(1, 693, 862, 1, 500)'點選欣然接受
Call 等待圖示(2, 0, 0, 0, 200, 0, 956, 422, 1074, "大地圖-左下任務圖示", 100)
KeyPress "B", 1 : Delay 1000
Call 等待圖示(99, 10, 10, 0, 50, 120, 150, 430, 200, "路引", 1)
Call 等待圖示(99, 10, 10, 0, 50, 120, 150, 430, 200, "心得", 1)
Call 等待圖示(99, 10, 10, 0, 50, 120, 150, 430, 200, "藥丹", 1)
Call 等待圖示(99, 10, 10, 0, 50, 120, 150, 430, 200, "名冊", 1)
MessageBox "收到額外物品"
EndScript
End Sub

Sub 傳授武功一人(zz傳授武功次數)
For zz傳授武功次數
	Call 自動移動點擊(1, 1494, 537, 1, 500)'點選F
	Call 自動移動點擊(1, 1195, 539, 1, 500)'點選幫助
	Call 自動移動點擊(1,  985, 559, 1, 500)'點選傳授
	Call 自動移動點擊(1, 1069, 306, 1, 500)'武學類別
	Call 自動移動點擊(1, 1765, 911, 1, 500)'點選確定
	Delay 8000
Next
End Sub

Sub 商船存取檔案刷銅幣(zz商船刷幣次數)
For zz商船刷幣次數
Delay 500	
FindPic 900,10,1000,100,"C:\Program Files (x86)\按鍵精靈2014\PIC\絕世好武功\交易天秤.bmp",0.9,intX,intY
If intX > 0 And intY > 0 Then 
	FindPic 1518,140,1569,189,"C:\Program Files (x86)\按鍵精靈2014\PIC\絕世好武功\交易-銅幣.bmp",0.9,intX,intY
	If intX> 0 And intY> 0 Then
		Call 等待圖示(2, 9, 9, 1, 200, 1516, 139, 1574, 201, "交易-銅幣", 100) '點選對方銅幣
		KeyPress "Num 5", 1 : Delay 200 : KeyPress "Num 0", 1 : Delay 200 : KeyPress "Num 0", 1 : Delay 200 : KeyPress "Num 0", 1 : Delay 200 : KeyPress "Num 0", 1 : Delay 200 '輸入50000
		Call 等待圖示(2, 30, 12, 1, 200, 918, 764, 998, 1046, "交易中間-交易按鈕", 100)'數量確認
		Call 等待圖示(2, 10, 10, 1, 200, 50, 139, 129, 1051, "交易-內功書", 100)'點選我方內功書
		KeyPress "Num 2", 1 : Delay 200 : KeyPress "Num 5", 1 : Delay 200'輸入25
		Call 等待圖示(2, 30, 12, 1, 200, 918, 764, 998, 1046, "交易中間-交易按鈕", 100)'數量確認
		Call 等待圖示(1, 957, 833, 1, 200, 937, 191, 986, 245, "公平交易-0", 100)'最後交易確認
		Call 快速存檔()
	Else 
		KeyPress "Esc", 1
	End If
	Call 讀取檔案(1, 50, 0)
	Call NPC自動互動(1450, 540, 1, 0, 0)
End If
Next
End Sub
