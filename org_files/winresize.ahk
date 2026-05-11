/* #SingleInstance, Force
;#KeyHistory, 0
SetBatchLines, -1
;ListLines, Off
SendMode Input ; Forces Send and SendRaw to use SendInput buffering for speed.
SetTitleMatchMode, 3 ; A window's title must exactly match WinTitle to be a match.
SetWorkingDir, %A_ScriptDir%
SplitPath, A_ScriptName, , , , thisscriptname
#MaxThreadsPerHotkey, 1 ; no re-entrant hotkey handling
SetCapsLockState, AlwaysOff
; DetectHiddenWindows, On
; SetKeyDelay, -1, -1 ; Remove short delay done automatically after every keystroke sent by Send or ControlSend
; SetMouseDelay, -1 ; Remove short delay done automatically after Click and MouseMove/Click/Drag
*/
SetWinDelay, -1 ; Remove short delay done automatically after every windowing command except IfWinActive and IfWinExist

win_enlarge:
    hwnd := "ahk_id " . getCursorWindow() 
    WinGet, ismax, MinMax, %hwnd%
    if (ismax = 1)
        Return 
    moveStep := resizeStep / 2
    OutputDebug, ismax %ismax%, hwnd %hwnd%`nmovestep %moveStep% resizestep %resizeStep%
    WinGetPos, x,y,w,h,%hwnd%
    nw:=w+resizeStep
    nh:=h+resizeStep
    nx:=x-moveStep
    ny:=y-moveStep
    WinMove,%hwnd%,,%nx%,%ny%,%nw%,%nh%
return

win_shrink:
    hwnd := "ahk_id " . getCursorWindow() 
    WinGet, ismax, MinMax, %hwnd%
    if (ismax = 1)
        Return 
    moveStep := resizeStep / 2
    WinGetPos, x,y,w,h,%hwnd%
    nw:=w-resizeStep
    nh:=h-resizeStep
    nx:=x+moveStep
    ny:=y+moveStep
    WinMove,%hwnd%,,%nx%,%ny%,%nw%,%nh% 
return

;WinMove, WinTitle, WinText, X, Y [, Width, Height, ExcludeTitle, ExcludeText]
CapsLock & z::
    ResizeActiveWindow(0.6,0.6)
Return 

/*
CapsLock & up::GetKeyState("Shi"+"ft") ? ResizeActiveWindow(0.4,0.3,"",0) : ResizeActiveWindow(1,0.4,0,0)

Return 
*/

GetMonitorAt(x, y, default=1) 
{ 
    SysGet,m, MonitorCount 
    ; Iterate through all monitors. 
    Loop, %m% 
    { ; Check if the window is on this monitor. 
        SysGet, Mon, Monitor, %A_Index% 
        if (x >= MonLeft && x <= MonRight && y >= MonTop && y <= MonBottom) 
            return A_Index 
    } 
    
return default
}

ResizeActiveWindow( scalex, scaley, ux:="",uy:="",uw:="",uh:="")
{ 
    WinGet, ismax, MinMax, A
    WinGetPos, curX, curY, curW, curH, A
    if (ismax=1)
        WinRestore, A 
    
    m:=WinMonitorInfo()
    OutputDebug, %ux%,%uy%, %uw%, %uh%
    OutputDebug, % StrLen(ux)
    
    ;if (RegExMatch(uw, "^(?:\+|\-)\d+$", out)) 
    ;; TODO: dokonczyc + - `
    
    
    ww:=StrLen(uw) = 0 ? m["w"]*scalex : uw
    wh:=StrLen(uh) = 0 ? m["h"]*scaley : uh
    ; if (SubStr(ww, StartingPos [, Length]))
    wx:=StrLen(ux) = 0 ? m["left"] + ((m["w"] - ww)/2) : ux
    wy:=StrLen(uy) = 0 ? m["top"] + ((m["h"] - wh)/2) : uy
    OutputDebug, if %wx%=%ux% and %wy%=%uy% and (%ww%=uw or %ww%=m["w"]* %scalex%) and wh<=m["h"]*%scaley%
        
    OutputDebug, WinMove, A,, %wx%, %wy%, %ww%, %wh% 
    WinMove, A,, %wx%, %wy%, %ww%, %wh%
    
    
Return
}

WinMonitorInfo( wintitle:="A" ) 
{ 
    WinGetPos, x,y,w,h,%wintitle%
    mi:=GetMonitorAt(x+w/2,y+h/2)
    SysGet, m, Monitor, %mi%
    mw:=(mRight-mLeft)
    mh:=(mBottom-mTop) 
    res := {"w": mw, "h": mh, "top": mTop, "left": mLeft, "right": mRight, "bottom": mBottom } 
Return res

}

MouseMonitorInfo( ) 
{ 
    MouseGetPos, mx,my
    
    mi:=GetMonitorAt(mx,my)
    SysGet, m, Monitor, %mi%
    mw:=(mRight-mLeft)
    mh:=(mBottom-mTop) 
    res := {"w": mw, "h": mh, "top": mTop, "left": mLeft, "right": mRight, "bottom": mBottom } 
Return res

}
