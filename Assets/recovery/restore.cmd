@echo off
setlocal EnableExtensions EnableDelayedExpansion
title PC MEDIC - RESTORE IMAGE

rem ---------------------------------------------------------------------------
rem  PC Medic recovery menu, for Windows Recovery (Windows RE).
rem
rem  Windows RE has no .NET and no PowerShell, so this is cmd plus the wimlib
rem  files sitting next to it. It never partitions and never formats: it applies
rem  the image to a volume you name and then writes boot files with bcdboot.
rem ---------------------------------------------------------------------------

set "ROOT=%~dp0"
set "WIMLIB=%ROOT%wimlib-imagex.exe"
set "WIMDLL=%ROOT%libwim-15.dll"
set "POINTER=%ROOT%backup-location.json"
set "LOG=%ROOT%restore-log.txt"
set "WORK=%TEMP%\pcmedic"
if not exist "%WORK%" md "%WORK%" >nul 2>&1

set "IMG="
set "LABEL="
set "INDEX=1"
set "FIRMWARE="
set "TGT="

:menu
cls
echo.
echo  ==================================================================
echo    PC MEDIC - RECOVERY MENU                  stick: %ROOT%
echo  ==================================================================
echo.
if defined IMG (
  echo    Image  : !IMG!
  echo    Volume : "!LABEL!"     image #!INDEX!     boot files: !FIRMWARE!
) else (
  echo    Image  : not located yet
)
echo.
echo    [1] Read this stick, locate the image and verify it
echo    [2] Restore the image to a volume
echo    [3] About this stick
echo    [4] Command prompt
echo    [5] Restart the PC
echo    [0] Turn the PC off
echo    [9] Leave this menu
echo.
set "PICK="
set /p "PICK=Choose 1-5, 9 to leave, or 0: "
if "%PICK%"=="1" goto do_locate
if "%PICK%"=="2" goto do_restore
if "%PICK%"=="3" goto about
if "%PICK%"=="4" goto shell
if "%PICK%"=="5" goto reboot
if "%PICK%"=="0" goto poweroff
if "%PICK%"=="9" goto :eof
goto menu

rem ---------------------------------------------------------------------------
rem  1. Read the pointer, find the image, verify the archive
rem ---------------------------------------------------------------------------
:do_locate
cls
call :locate
if errorlevel 1 (
  echo.
  call :hold
  goto menu
)
echo.
echo  Verifying the archive. This reads the whole image and can take a while.
echo.
"%WIMLIB%" verify "!IMG!"
if errorlevel 1 (
  echo.
  echo  [X] The archive FAILED its integrity check. Do not restore from it.
  call :log "VERIFY FAILED for !IMG!"
  echo.
  call :hold
  goto menu
)
echo.
echo  [OK] Pointer read, image found and archive verified.
call :log "VERIFY OK !IMG!"
echo.
call :hold
goto menu

rem ---------------------------------------------------------------------------
rem  2. Restore
rem ---------------------------------------------------------------------------
:do_restore
cls
if not defined IMG call :locate
if errorlevel 1 (
  echo.
  call :hold
  goto menu
)
if not defined IMG goto menu

call :listdisks
echo.
echo  ==================================================================
echo    TARGET VOLUME
echo  ==================================================================
echo.
echo    Type the letter of the volume the image should be applied to.
echo    Everything on that volume will be replaced.
echo.
set "LETTER="
set /p "LETTER=Target volume letter (blank to cancel): "
if not defined LETTER goto menu
set "TGT=!LETTER:~0,1!:"
if not exist "!TGT!\" (
  echo.
  echo  [X] !TGT! is not a readable volume.
  echo.
  call :hold
  goto menu
)
if /i "!TGT!"=="%ROOT:~0,2%" (
  echo.
  echo  [X] That is this stick. Choose the volume the Windows image belongs on.
  echo.
  call :hold
  goto menu
)

call :volname !TGT:~0,1!
echo.
echo  ==================================================================
echo    CONFIRM
echo  ==================================================================
echo.
echo    Image    : !IMG!   (image #!INDEX!)
echo    Target   : !TGT!  volume "!VOL!"
echo    Boot mode: !FIRMWARE!
echo.
echo    EVERYTHING ON !TGT! WILL BE REPLACED BY THE IMAGE.
echo    There is no undo. Type ERASE in capitals to continue.
echo.
set "CONFIRM="
set /p "CONFIRM=Type ERASE: "
if /i not "!CONFIRM!"=="ERASE" (
  echo.
  echo  Cancelled - nothing was written.
  echo.
  call :hold
  goto menu
)

call :log "APPLY START !IMG! #!INDEX! to !TGT!"
echo.
echo  Applying the image. Do not remove the stick or the backup disk.
echo.
"%WIMLIB%" apply "!IMG!" !INDEX! "!TGT!\\" --check
if errorlevel 1 (
  echo.
  echo  [X] The apply failed. Nothing else was attempted.
  call :log "APPLY FAILED"
  echo.
  call :hold
  goto menu
)
echo.
echo  [OK] Files restored.
call :log "APPLY OK"

call :bootstrap
echo.
call :hold
goto menu

rem ---------------------------------------------------------------------------
rem  Boot files
rem ---------------------------------------------------------------------------
:bootstrap
set "WIN=!TGT!\Windows"
echo.
echo  Writing boot files for !WIN! ...
if not exist "!WIN!\System32\ntoskrnl.exe" (
  echo  [X] !WIN! does not look like a Windows installation.
  echo      Boot files were NOT written. Use BOOT REPAIR from PC Medic in Windows.
  call :log "BCD SKIP no windows at !WIN!"
  exit /b 1
)

if /i "!FIRMWARE!"=="BIOS" (
  echo  bcdboot "!WIN!" /f BIOS /s !TGT!
  bcdboot "!WIN!" /f BIOS /s !TGT!
  if errorlevel 1 (
    echo  Retrying with /f ALL ...
    bcdboot "!WIN!" /f ALL /s !TGT!
  )
  goto bcd_done
)

call :findesp
if defined ESP (
  echo  bcdboot "!WIN!" /f UEFI /s !ESP!
  bcdboot "!WIN!" /f UEFI /s !ESP!
  if errorlevel 1 (
    echo  Retrying with /f ALL ...
    bcdboot "!WIN!" /f ALL /s !ESP!
  )
  goto bcd_done
)

echo  [X] No EFI system partition was found to write UEFI boot files to.
echo      Trying /f ALL on the Windows volume instead.
bcdboot "!WIN!" /f ALL /s !TGT!

:bcd_done
if errorlevel 1 (
  echo.
  echo  [X] bcdboot failed. The files are restored but the volume may not boot.
  echo      In Windows, use Backup and Restore - REPAIR BOOT.
  call :log "BCD FAILED"
  exit /b 1
)
echo.
echo  [OK] Boot files written. Restart and remove the stick.
call :log "BCD OK"
exit /b 0

rem ---------------------------------------------------------------------------
rem  diskpart: list disks with model and size
rem ---------------------------------------------------------------------------
:listdisks
echo.
echo  ==================================================================
echo    DISKS ON THIS MACHINE
echo  ==================================================================
echo.
call :iselevated
if errorlevel 1 goto disks_unavailable
> "%WORK%\dp_list.txt" echo list disk
diskpart /s "%WORK%\dp_list.txt" > "%WORK%\dp_out.txt" 2>&1
if errorlevel 1 goto disks_unavailable

for /f "tokens=2" %%N in ('findstr /r /c:"^  Disk [0-9]" "%WORK%\dp_out.txt"') do call :showdisk %%N
exit /b 0

:disks_unavailable
echo  [!] diskpart could not list the disks here, so no model or size is shown.
echo      In Windows Recovery this works; from a non-elevated prompt, or when the
echo      machine cannot see the disk, it does not.
echo.
exit /b 1

:showdisk
echo  ------------------------------------------------------------------
> "%WORK%\dp_one.txt" echo select disk %1
>>"%WORK%\dp_one.txt" echo detail disk
diskpart /s "%WORK%\dp_one.txt" > "%WORK%\dp_one_out.txt" 2>&1
findstr /v /c:"Microsoft DiskPart" /c:"Microsoft Corporation" /c:"On computer" "%WORK%\dp_one_out.txt"
echo  ------------------------------------------------------------------
echo.
exit /b 0

rem ---------------------------------------------------------------------------
rem  Locate the image: the pointer's path first, then its volume label
rem ---------------------------------------------------------------------------
:locate
set "IMG="
call :readpointer
if errorlevel 1 exit /b 1

echo  Pointer : %POINTER%
echo  Image   : !IMG!
echo  Volume  : "!LABEL!"     image #!INDEX!     boot files: !FIRMWARE!
echo.

if exist "!IMG!" (
  echo  The image is where the pointer says it is.
  exit /b 0
)

echo  That path is not available. Looking for volume "!LABEL!" ...
call :findimage
if not defined IMG (
  echo.
  echo  [X] No volume labelled "!LABEL!" carries that file.
  echo      Attach the disk that holds the backup, or type the path by hand.
  echo.
  set "MANUAL="
  set /p "MANUAL=Image path (blank to cancel): "
  if defined MANUAL if exist "!MANUAL!" set "IMG=!MANUAL!"
)
if not defined IMG exit /b 1
echo  Found it: !IMG!
exit /b 0

:findimage
rem IMG still holds the pointer's stale path here, so the search needs its own variable.
set "FOUND="
set "REL=!IMG:~3!"
if not defined REL exit /b 0
for %%D in (D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
  if not defined FOUND if exist "%%D:\!REL!" (
    call :volname %%D
    if /i "!VOL!"=="!LABEL!" set "FOUND=%%D:\!REL!"
  )
)
if defined FOUND set "IMG=!FOUND!"
exit /b 0

:readpointer
if not exist "%POINTER%" (
  echo  [X] This stick has no backup-location.json.
  echo      Stamp it with BACK UP TO DISK in PC Medic first.
  exit /b 1
)
call :jsonstring ImagePath IMG
call :jsonstring ImageVolumeLabel LABEL
call :jsonstring Firmware FIRMWARE
call :jsonnumber ImageIndex INDEX
if not defined IMG (
  echo  [X] %POINTER% could not be read as a recovery pointer.
  exit /b 1
)
if not defined INDEX set "INDEX=1"
if not defined FIRMWARE set "FIRMWARE=UEFI"
exit /b 0

rem Pull one indented "Name": "value", line out of the pointer. The writer is
rem the same code that stamps every stick, so the layout is known.
:jsonstring
set "LINE="
for /f "usebackq delims=" %%L in (`findstr /c:"\"%~1\":" "%POINTER%"`) do if not defined LINE set "LINE=%%L"
if not defined LINE ( set "%~2=" & exit /b 0 )
set "V=!LINE:*:=!"
set "V=!V:~2!"
set "V=!V:~0,-2!"
set "V=!V:\\=\!"
set "%~2=!V!"
exit /b 0

:jsonnumber
set "LINE="
for /f "usebackq delims=" %%L in (`findstr /c:"\"%~1\":" "%POINTER%"`) do if not defined LINE set "LINE=%%L"
if not defined LINE ( set "%~2=" & exit /b 0 )
set "V=!LINE:*:=!"
set "V=!V: =!"
if not defined V ( set "%~2=" & exit /b 0 )
set "V=!V:~0,-1!"
set "%~2=!V!"
exit /b 0

:iselevated
whoami /groups 2>nul | findstr /c:"S-1-16-12288" >nul && exit /b 0
whoami /groups 2>nul | findstr /c:"S-1-16-16384" >nul && exit /b 0
exit /b 1

:volname
set "VOL="
for /f "tokens=5,*" %%a in ('vol %1: 2^>nul') do if /i "%%a"=="is" set "VOL=%%b"
exit /b 0

rem ---------------------------------------------------------------------------
rem  EFI system partition: the "System" partition on the target's disk
rem ---------------------------------------------------------------------------
:findesp
set "ESP="
call :iselevated
if errorlevel 1 exit /b 0
if not exist "%WORK%\dp_list.txt" (
  > "%WORK%\dp_list.txt" echo list disk
  diskpart /s "%WORK%\dp_list.txt" > "%WORK%\dp_out.txt" 2>&1
)
if errorlevel 1 exit /b 0
for /f "tokens=2" %%N in ('findstr /r /c:"^  Disk [0-9]" "%WORK%\dp_out.txt"') do (
  if not defined ESP call :esp_on_disk %%N
)
exit /b 0

:esp_on_disk
> "%WORK%\dp_esp.txt" echo select disk %1
>>"%WORK%\dp_esp.txt" echo list partition
diskpart /s "%WORK%\dp_esp.txt" > "%WORK%\dp_esp_out.txt" 2>&1
if errorlevel 1 exit /b 0
for /f "tokens=2,3" %%P in ('findstr /c:"System" "%WORK%\dp_esp_out.txt"') do (
  if not defined ESP (
    > "%WORK%\dp_assign.txt" echo select disk %1
    >>"%WORK%\dp_assign.txt" echo select partition %%P
    >>"%WORK%\dp_assign.txt" echo assign letter=S
    diskpart /s "%WORK%\dp_assign.txt" >nul 2>&1
    if exist "S:\" set "ESP=S:"
  )
)
exit /b 0

rem ---------------------------------------------------------------------------
rem  3-5. About, shell, power
rem ---------------------------------------------------------------------------
:about
cls
echo.
echo  PC MEDIC RECOVERY STICK
echo  ==================================================================
echo.
echo   This folder holds a pointer to the image, not the image itself:
echo     backup-location.json   image path, volume label, image number
echo     RESTORE-README.txt     the same thing in plain English
echo     restore.cmd            this menu
echo     wimlib-imagex.exe      the imaging engine (GPLv3, wimlib.net)
echo     libwim-15.dll          its library
echo.
echo   Nothing is written to a disk without the ERASE confirmation.
echo.
call :hold
goto menu

:shell
echo.
echo  Type EXIT to come back to this menu.
echo.
cmd /k
goto menu

:reboot
echo.
wpeutil reboot
goto menu

:poweroff
echo.
wpeutil shutdown
goto menu

:hold
rem Enter rather than any key: a stray keypress cannot skip a warning, and a scripted
rem run can feed it a blank line.
set "HOLD="
set /p "HOLD=Press Enter to continue... "
exit /b 0

rem Keep the message free of > and <: cmd would read them as redirection, not text.
:log
>> "%LOG%" echo %DATE% %TIME%  %~1
exit /b 0
