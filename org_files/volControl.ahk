global fsApp
; The percentage by which to raise or lower the volume each time:
vol_Step = 4

; How long to display the volume level bar graphs:
vol_DisplayTime = 2000

; Master Volume Bar color (see the help file to use more
; precise shades):
vol_CBM = 42a5f5R

; Wave Volume Bar color
vol_CBW = 1ed760

; Background color
vol_CW = black

; Bar's screen position.  Use -1 to center the bar in that dimension:
vol_PosX = -1
vol_PosY = 900
vol_Width = 320  ; width of bar
vol_Thick:=20  ; thickness of bar
vol_PosY:=(A_ScreenHeight-(4*vol_Thick))
getLevels()
; If your keyboard has multimedia buttons for Volume, you can
; try changing the below hotkeys to use them by specifying
; Volume_Up, ^Volume_Up, Volume_Down, and ^Volume_Down:

; Hotkey, ^!WheelUp, vol_MasterUp      ; Win+UpArrow
; Hotkey, ^!WheelDown, vol_MasterDown
; Hotkey, !+WheelUp, vol_WaveUp       ; Shift+Win+UpArrow
; Hotkey, !+WheelDown, vol_WaveDown

;___________________________________________ 
;_____Auto Execute Section__________________ 

; DON'T CHANGE ANYTHING HERE (unless you know what you're doing).

vol_BarOptionsMaster = 1:B ZH%vol_Thick% ZX0 ZY0 W%vol_Width% CB%vol_CBM% CW%vol_CW%
vol_BarOptionsWave   = 2:B ZH%vol_Thick% ZX0 ZY0 W%vol_Width% CB%vol_CBW% CW%vol_CW%

; If the X position has been specified, add it to the options.
; Otherwise, omit it to center the bar horizontally:
if vol_PosX >= 0
{
    vol_BarOptionsMaster = %vol_BarOptionsMaster% X%vol_PosX%
    vol_BarOptionsWave   = %vol_BarOptionsWave% X%vol_PosX%
}

; If the Y position has been specified, add it to the options.
; Otherwise, omit it to have it calculated later:
if vol_PosY >= 0
{
    vol_BarOptionsMaster = %vol_BarOptionsMaster% Y%vol_PosY%
    vol_PosY_wave = %vol_PosY%
    vol_PosY_wave += %vol_Thick%
    vol_BarOptionsWave = %vol_BarOptionsWave% Y%vol_PosY_wave%
}

;___________________________________________ 

vol_WaveUp: 
    If vol_Wave < 100
    {
        Run, %A_ScriptDir%\3rdParty\soundvolumeview.exe /ChangeVolume Spotify.exe +%vol_Step%
            vol_Wave:=vol_Wave+vol_Step
        OutputDebug %vol_Wave%     
    }
    Gosub, vol_ShowBars
return

vol_WaveDown: 
    If vol_Wave > 0
    {
        Run, %A_ScriptDir%\3rdParty\soundvolumeview.exe /ChangeVolume Spotify.exe -%vol_Step%
            vol_Wave:=vol_Wave-vol_Step
        OutputDebug %vol_Wave%       
    }
    Gosub, vol_ShowBars
return

vol_MasterUp: 
    If vol_Master < 100
    {
        vol_Master:=vol_Master+vol_Step
        Run, %A_ScriptDir%\3rdParty\soundvolumeview.exe /ChangeVolume Speakers +%vol_Step%
        OutputDebug %vol_Master%
    }
    Gosub, vol_ShowBars
return

vol_MasterDown: 
    ;SoundSet, -%vol_Step%
    If vol_Master > 0
    {
        Run, %A_ScriptDir%\3rdParty\soundvolumeview.exe /ChangeVolume Speakers -%vol_Step%
        vol_Master:=vol_Master-vol_Step
        OutputDebug %vol_Master%
    }
    Gosub, vol_ShowBars
return

vol_ShowBars:
    
    curhwnd:=WinExist("A")
    fs:=isWindowFullScreen(curhwnd)
    OutputDebug,fs: %curhwnd%`n true: %fs%
    If fs
        Return
    
    ; To prevent the "flashing" effect, only create the bar window if it
    ; doesn't already exist:
    IfWinNotExist, vol_Wave
    {
        Progress, %vol_BarOptionsWave%, , , vol_Wave
        WinSet, Transparent, 155, vol_Wave
    }
    IfWinNotExist, vol_Master
    {
        ; Calculate position here in case screen resolution changes while
        ; the script is running:
        if vol_PosY < 0
        {
            ; Create the Wave bar just above the Master bar:
            WinGetPos, , vol_Wave_Posy, , , vol_Wave
            vol_Wave_Posy -= %vol_Thick%
            Progress, %vol_BarOptionsMaster% Y%vol_Wave_Posy%, , , vol_Master
        }
        else
            Progress, %vol_BarOptionsMaster%, , , vol_Master
        
        WinSet, Transparent, 155, vol_Master
    }
    ; Get both volumes in case the user or an external program changed them:
    ;SoundGet, vol_Master, Master
    ;SoundGet, vol_Wave, Wave
    
    Progress, 1:%vol_Master%
    Progress, 2:%vol_Wave%
    SetTimer, vol_BarOff, %vol_DisplayTime%
return

vol_BarOff:
    SetTimer, vol_BarOff, Off
    Progress, 1:Off
    Progress, 2:Off
    getLevels()
return

getLevels(){
    global vol_Master
    global vol_Wave
    ;RunWait, %A_ScriptDir%\3rdParty\soundvolumeview.exe /GetPercent Spotify.exe,, UseErrorLevel  
    RunWait, %A_ScriptDir%\3rdParty\soundvolumeview.exe /GetPercent Spotify.exe,, UseErrorLevel  
        vol_Wave:=Round(errorlevel / 10)
    OutputDebug vol_Wave %vol_Wave%
    RunWait, %A_ScriptDir%\3rdParty\soundvolumeview.exe /GetPercent Speakers,, UseErrorLevel
    vol_Master:= (errorlevel // 10)
    OutputDebug vol_Master %vol_Master%
Return    
}

isWindowFullScreen(WinID)
{
    ;checks if the specified window is full screen
    ;code from NiftyWindows source
    ;(with only slight modification)
    
    ;use WinExist of another means to get the Unique ID (HWND) of the desired window
    
    if ( !WinID )
        return
    
    WinGet, WinMinMax, MinMax, ahk_id %WinID%
    WinGetPos, WinX, WinY, WinW, WinH, ahk_id %WinID%
    
    if (WinMinMax = 0) && (WinX = 0) && (WinY = 0) && (WinW = A_ScreenWidth) && (WinH = A_ScreenHeight)
    {
        WinGetClass, WinClass, ahk_id %WinID%
        WinGet, WinProcessName, ProcessName, ahk_id %WinID%
        SplitPath, WinProcessName, , , WinProcessExt
        
        if (WinClass != "Progman") && (WinProcessExt != "scr")
        {
            ;program is full-screen
            return true
        }
    }
    OutputDebug, nie FS
}