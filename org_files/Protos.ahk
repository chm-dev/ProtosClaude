#SingleInstance Force
#Persistent 
#InstallKeybdHook
#InstallMouseHook
#MenuMaskKey LShift

SoundPlay, %A_ScriptDir%\sounds\protos_active.mp3
Sleep 1500

DetectHiddenWindows, On 
SetTitleMatchMode, RegEx
SendMode, Event
SetWorkingDir %A_ScriptDir%
SetBatchLines -1
SetKeyDelay, 0
SetCapsLockState, AlwaysOff
SetNumLockState, Off
BlockInput, Send
CoordMode, Mouse
Menu, Tray, NoStandard
Menu, Tray, Icon, %A_ScriptDir%\protos.png, 1


Menu, Tray, Add
Menu, Tray, Standard
dhw := A_DetectHiddenWindows
DetectHiddenWindows On

Run "%ComSpec%" /k,, Hide, pid
while !(hConsole := WinExist("ahk_pid" pid))
    Sleep 10
DllCall("AttachConsole", "UInt", pid)
DetectHiddenWindows %dhw%

workHostname=AGIL
ClipSaved= ; this is not used ???
cmderMode=0
launcherMode=1
resizeMode=0

resizeStep=80 ;winresize

playerExe:="Spotify\.exe"
playerHWND= ; this is not used ??
playerPath=%A_AppData%\Spotify\%playerExe%

playerWinTitle=ahk_exe %playerExe% 
GroupAdd, SpotifyGrp, ahk_exe %playerExe%,,,(?:^.?$)|(?:^devtools) ;global window group holding ... only main window thanks to regex .. it is totally stupid and should be changed ;)





global thm
;#include %A_ScriptDir%\Lib\TapHoldManager.ahk
; TapTime / Prefix can now be set here
;thm := new TapHoldManager(,,,"~")
;thm.Add("LAlt", Func("openLauncher"))
;rthm.Add("CapsLock", Func("sendMegaModifier"))

; it has to be after first capslock definitions

; these share same mouse mods+wheel actions
; #Include %A_ScriptDir%\toggler.ahk
#Include %A_ScriptDir%\volControl.ahk 
#Include %A_ScriptDir%\winresize.ahk

^!WheelUp::
    Gosub, vol_MasterUp 
Return
^!WheelDown::
    Gosub, vol_MasterDown
Return

^+WheelUp::
    Gosub, win_enlarge 
Return
^+WheelDown::
    Gosub, win_shrink 
Return

!+WheelUp::Gosub vol_WaveUp ; Shift+Win+UpArrow
!+WheelDown::Gosub vol_WaveDown
;so we have to deal with it here

if (InStr(A_ComputerName, workHostname, false))
    #Include %A_ScriptDir%\temp.ahk




releaseAllModifiers() 
{ 
    list = Control|Alt|Shift 
    Loop Parse, list, | 
    { 
        if (GetKeyState(A_LoopField)) 
            send {Blind}{%A_LoopField% up} ; {Blind} is added.
    } 
} 

isWindowVisible(WTitle){
    if not DllCall("IsWindowVisible", "UInt", WinExist(WTitle)){
        Return False
    }Else{
        Return True
    }
}
;~CapsLock:: 
;Send {LCtrl Down}{LShift Down}{LAlt Down}
;keyWait, CapsLock
;Send {LCtrl Up}{LShift Up}{LAlt Up}
;Return

getCursorWindow(){
    MouseGetPos,,,WinUMID
    WinGet, pName, ProcessName, ahk_id %WinUMID%
    If (InStr(pName, "explorer") or pName = "")
        Return -1
    Else
        Return WinUMID 
}

activateCursorWindow(){
    OutputDebug, % WinGetTitle, `% "ahk_id " . getCursorWindow()
    WinActivate, % "ahk_id " . getCursorWindow()
}
;=== BEGIN === 
; == Debug ==
CapsLock & r:: 
    Reload
Return

; == Start of script ==

CapsLock & LButton::
    WinUMID := getCursorWindow()
    if (WinUMID = -1) 
        Return
    WinSet AlwaysOnTop, Toggle, ahk_id %WinUMID%
    WinGet, exs, ExStyle, ahk_id %WinUMID%
    if (exs & 0x8) { ;Always on top on
        WinSet, Transparent, 250, ahk_id %WinUMID%
    }else { ; AOT Off
        WinSet, Transparent, Off, ahk_id %WinUMID%
    }
Return

~Home & PgUp:: 
    Suspend, Permit
    if (A_IsSuspended = 1) 
    {
        SoundPlay, %A_ScriptDir%\sounds\protos_active.mp3
        Sleep 1200
        SetCapsLockState, AlwaysOff
        Suspend, Off
    }
Return

~Home & PgDn:: 
    SoundPlay, %A_ScriptDir%\sounds\protos_suspended.mp3
    Sleep 1200
    SetCapsLockState, Off
    Suspend, On
Return

^!+End:: 
    SoundPlay, %A_ScriptDir%\sounds\protos_exiting.mp3
    Sleep 1800 
ExitApp
Return


CapsLock & WheelDown:: 
    WinUMID := getCursorWindow()
    if WinUMID = -1
        Return 
    WinMinimize ahk_id %WinUMID%
Return

CapsLock & WheelUp:: 
    WinUMID := getCursorWindow()
    if WinUMID = -1
        Return 
    WinMaximize ahk_id %WinUMID%
Return

CapsLock & MButton:: 
    WinUMID := getCursorWindow()
    if WinUMID = -1
        Return 
    WinRestore ahk_id %WinUMID%
Return

CapsLock & XButton1::
    if (!GetKeyState("Shift"))
        activateCursorWindow()

    Send #{Left}
Return

CapsLock & XButton2:: 
    if (!GetKeyState("Shift"))
        activateCursorWindow()

    Send #{Right}

Return

LCtrl & XButton1::Send ^#{Left}
LCtrl & XButton2::Send ^#{Right}

^!v::
    SendEvent {Raw}%Clipboard%
Return 

CapsLock & v:: 
    if (InStr(Clipboard, "document.query") = 0){

        SendStr := RegExReplace(Clipboard, "^""(.+)""$", "$1")
        SendStr := StrReplace(SendStr, """""", """",,-1)
        ;MsgBox, %Clipboard%, %SendStr%
        SendEvent {Raw} document.querySelector('%SendStr%')
    } Else {
        SendStr := StrReplace(Clipboard, """", """""")
        SendStr := RegExReplace(SendStr, "document\.querySelector(?:All)?\('?([^\(\)]*)'\)", "$1")

        SendEvent "%SendStr%"
    }
Return



XButton1 & MButton:: 
^!MButton:: 
    Send {Volume_Mute}
Return

XButton1:: 
+XButton1:: 
    if (!GetKeyState("Shift")){
        MouseGetPos,,, WinUMID
        WinActivate, ahk_id %WinUMID%
    }
    SendEvent #{Left}
Return
XButton2::
+XButton2:: 
    if (!GetKeyState("Shift")){
        MouseGetPos,,, WinUMID
        WinActivate, ahk_id %WinUMID%
    }
    SendEvent #{Right}
Return

^XButton1:: SendEvent #+{Left}

^XButton2::SendEvent #+{Right}
Return
!XButton1::
!+XButton1::
    if (!GetKeyState("Shift"))
        activateCursorWindow() 
    SendEvent #+{Left}
Return
!XButton2:: 
+!XButton2::
    if (!GetKeyState("Shift"))
        activateCursorWindow()
    SendEvent #+{Right}
Return
+!MButton:: 
NumpadIns:: 
    Send {Media_Play_Pause}
Return 
NumpadDel:: Send {Media_Stop}
NumpadEnd:: Send {Media_Prev}
NumpadDown:: Send {Media_Next}
NumpadAdd:: Send {Volume_Up 4}
NumpadSub:: Send {Volume_Down 4}

#v::SendEvent +!v
#+v::SendEvent #v


CapsLock & LWin:: 
    if (launcherMode = 0) {
        launcherMode := 1
        SoundPlay, %A_ScriptDir%\sounds\launcher_mode_on.wav
    }else {
        launcherMode := 0
        SoundPlay, %A_ScriptDir%\sounds\launcher_mode_off.wav
    }
    Send {CapsLock Up}
Return

#If launcherMode = 1 and GetKeyState("ScrollLock", "T") = 0

;to understand what is going on with LWin here check #MenuMaskKey in ahk help
~LWin:: 
    Send {Blind}{LShift}
    KeyWait, LWin 
    If (InStr(A_PriorKey,"LWin"))
        Send !{backspace}
Return 
#If


CapsLock & s::
    ; WinGet, num, Count, ahk_group SpotifyGrp
    ; OutputDebug, %A_TitleMatchMode%, HiddenWIndows %A_DetectHiddenWindows%
    Process, Exist, Spotify.exe

    ;OutputDebug, %ErrorLevel% 

    if (ErrorLevel){ 
        sWinTitle := "(?:Spotify Premium)|(?:[^\s]+\s-\s[^\s]+).* ahk_exe Spotify.exe"

        if (isWindowVisible(sWinTitle)){
            OutputDebug, is visible
            If (WinActive(sWinTitle)){
                OutputDebug, is active, WinHide
                WinHide, %sWinTitle% 
            } Else {
                OutputDebug, not active so restoring, just in case and activating
                WinRestore, %sWinTitle%
                WinActivate, %sWinTitle% 
            }
        }else {
            OutputDebug, is not visible - showing
            WinShow, %sWinTitle%
            WinActivate, %sWinTitle% 
        }
    }else{
        OutputDebug not running, start
        Run, Spotify.exe
    } 
Return

~F4 & ~RButton::
    WinGet,pName, ProcessName, A
forcekill:=GetKeyState("Shift") ? "/F" : "F"
    Run taskkill /IM %pName%,,Hide
Return

#If WinActive("ahk_group SpotifyGrp") or WinActive("ahk_id " sptHWND)
    ~Esc::WinHide, A
#If


::@gm:: 
    SendInput chmielciu@gmail.com
Return

::@we:: 
    SendInput tomasz.chmielewski@weareams.com 
Return
::@num:: 
    SendInput 579948647
Return

;---------- EXPERIMENTS BELOW ---------------------------

;in place of the 1, can use
;const int MONITOR_ON = -1;
;const int MONITOR_OFF = 2;
;const int MONITOR_STANBY = 1;

CapsLock & Numpad5::
    Sleep 1000 ; if you use this with a hotkey, not sleeping will make it so your keyboard input wakes up the monitor immediately
    SendMessage 0x112, 0xF170, 2,,Program Manager ; send the monitor into off mode
    ;wait for a key to be pressed
    Input, SingleKey, L1, {LControl}{RControl}{LAlt}{RAlt}{LShift}{RShift}{LWin}{RWin}{AppsKey}{F1}{F2}{F3}{F4}{F5}{F6}{F7}{F8}{F9}{F10}{F11}{F12}{Left}{Right}{Up}{Down}{Home}{End}{PgUp}{PgDn}{Del}{Ins}{BS}{Capslock}{Numlock}{PrintScreen}{Pause} ;wait for a key to be pressed
    SendMessage 0x112, 0xF170, -1,,Program Manager ; send the monitor into on mode
return





~RAlt & LButton:: 
    CoordMode, Mouse ; Switch to screen/absolute coordinates.
    MouseGetPos, EWD_MouseStartX, EWD_MouseStartY, EWD_MouseWin
    WinGetPos, EWD_OriginalPosX, EWD_OriginalPosY,,, ahk_id %EWD_MouseWin%
    WinGet, EWD_WinState, MinMax, ahk_id %EWD_MouseWin%
    if EWD_WinState = 0 ; Only if the window isn't maximized
        SetTimer, EWD_WatchMouse, 10 ; Track the mouse as the user drags it.
return

#If


EWD_WatchMouse: 
    GetKeyState, EWD_LButtonState, LButton, P
    if EWD_LButtonState = U ; Button has been relseased, so drag is complete.
    {
        SetTimer, EWD_WatchMouse, off
        return
    }

    GetKeyState, EWD_EscapeState, Escape, P
    if EWD_EscapeState = D ; Escape has been pressed, so drag is cancelled.
    {
        SetTimer, EWD_WatchMouse, off
        WinMove, ahk_id %EWD_MouseWin%,, %EWD_OriginalPosX%, %EWD_OriginalPosY%
        return
    }

    ; Otherwise, reposition the window to match the change in mouse coordinates
    ; caused by the user having dragged the mouse: 
    CoordMode, Mouse
    MouseGetPos, EWD_MouseX, EWD_MouseY
    WinGetPos, EWD_WinX, EWD_WinY,,, ahk_id %EWD_MouseWin%
    SetWinDelay, -1 ; Makes the below move faster/smoother.
    WinMove, ahk_id %EWD_MouseWin%,, EWD_WinX + EWD_MouseX - EWD_MouseStartX, EWD_WinY + EWD_MouseY - EWD_MouseStartY
    EWD_MouseStartX := EWD_MouseX ; Update for the next timer-call to this subroutine.
    EWD_MouseStartY := EWD_MouseY
return

SptPositionSet:
    mm:=MouseMonitorInfo()
    w := Floor(mm["w"] * sptScale)
    h := Floor(w/2) ; Floor(A_ScreenHeight * 0.75)
    x := Floor((A_ScreenWidth - w) / 2 )+ Floor(mm["left"])
    y := Floor((A_ScreenHeight - h) / 2 )+ Floor(mm["top"])
    WinMove, ahk_id %sptHWND%,, %x%, %y%, %w%, %h%
    OutputDebug WinMove, ahk_id %sptHWND%,, %x%, %y%, %w%, %h% 
Return





