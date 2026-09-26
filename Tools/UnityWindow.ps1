param([ValidateSet('Refresh','Capture','Play','Stop','Focus','Key')][string]$Action='Capture',[string]$Keys='')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class UnityWindowNative {
 [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr handle);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr handle,out Rect rect);
}
'@
$workspace=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$processes=Get-Process Unity -ErrorAction Stop | Where-Object {$_.MainWindowHandle -ne 0}
$candidates=@($processes | Where-Object {
 $details=Get-CimInstance Win32_Process -Filter ("ProcessId = " + $_.Id)
 $details.CommandLine.Contains($workspace)
})
if($candidates.Count -ne 1){throw 'Expected exactly one visible Unity editor.'}
$target=$candidates[0]
[UnityWindowNative]::SetForegroundWindow($target.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 250
if($Action -eq 'Refresh'){[System.Windows.Forms.SendKeys]::SendWait('^r')}
if($Action -eq 'Play' -or $Action -eq 'Stop'){[System.Windows.Forms.SendKeys]::SendWait('^p')}
if($Action -eq 'Key'){[System.Windows.Forms.SendKeys]::SendWait($Keys)}
if($Action -eq 'Capture'){
 $rect=New-Object UnityWindowNative+Rect
 [UnityWindowNative]::GetWindowRect($target.MainWindowHandle,[ref]$rect) | Out-Null
 $bitmap=New-Object Drawing.Bitmap(($rect.Right-$rect.Left),($rect.Bottom-$rect.Top))
 $graphics=[Drawing.Graphics]::FromImage($bitmap)
 $graphics.CopyFromScreen($rect.Left,$rect.Top,0,0,$bitmap.Size)
 $path=Join-Path $PSScriptRoot 'verification-editor.png'
 $bitmap.Save($path,[Drawing.Imaging.ImageFormat]::Png)
 $graphics.Dispose();$bitmap.Dispose();Write-Output $path
}
