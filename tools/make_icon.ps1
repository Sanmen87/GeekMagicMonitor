#requires -Version 5.1
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot=Split-Path -Parent $PSScriptRoot
$iconPath=Join-Path $projectRoot 'monitor\GeekMagicMonitor\Assets\geekmagic.ico'
$sizes=@(16,24,32,48,64,128,256)
$images=New-Object 'System.Collections.Generic.List[byte[]]'
foreach($size in $sizes) {
    $bitmap=New-Object Drawing.Bitmap($size,$size)
    $graphics=[Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.ScaleTransform($size/256.0,$size/256.0)
    $graphics.Clear([Drawing.Color]::FromArgb(9,19,33))
    $faces=@(
        @{Color='#31d7ed';Points=@(128,24,226,79,128,135,30,79)},
        @{Color='#16778e';Points=@(30,87,121,140,121,238,30,184)},
        @{Color='#9e7be8';Points=@(135,140,226,87,226,184,135,238)}
    )
    foreach($face in $faces) {
        $points=New-Object 'Drawing.PointF[]' 4
        for($index=0;$index -lt 4;$index++) { $points[$index]=New-Object Drawing.PointF($face.Points[$index*2],$face.Points[$index*2+1]) }
        $brush=New-Object Drawing.SolidBrush([Drawing.ColorTranslator]::FromHtml($face.Color))
        $graphics.FillPolygon($brush,$points);$brush.Dispose()
    }
    $pulse=@(55,159,77,159,91,128,108,189,125,160,154,160,167,133,185,157,205,157)
    $pulsePoints=New-Object 'Drawing.PointF[]' 9
    for($index=0;$index -lt 9;$index++) { $pulsePoints[$index]=New-Object Drawing.PointF($pulse[$index*2],$pulse[$index*2+1]) }
    $pen=New-Object Drawing.Pen([Drawing.Color]::FromArgb(237,245,255),10)
    $pen.LineJoin=[Drawing.Drawing2D.LineJoin]::Round
    $graphics.DrawLines($pen,$pulsePoints)
    $memory=New-Object IO.MemoryStream
    $bitmap.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
    $images.Add($memory.ToArray())
    $memory.Dispose();$pen.Dispose();$graphics.Dispose();$bitmap.Dispose()
}
$stream=[IO.File]::Create($iconPath)
$writer=New-Object IO.BinaryWriter($stream)
try {
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sizes.Count)
    $offset=6+16*$sizes.Count
    for($index=0;$index -lt $sizes.Count;$index++) {
        $dimension=if($sizes[$index] -eq 256){0}else{$sizes[$index]}
        $writer.Write([byte]$dimension);$writer.Write([byte]$dimension)
        $writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32)
        $writer.Write([uint32]$images[$index].Length);$writer.Write([uint32]$offset)
        $offset+=$images[$index].Length
    }
    foreach($data in $images){$writer.Write([byte[]]$data)}
} finally {$writer.Dispose();$stream.Dispose()}
Write-Output $iconPath
