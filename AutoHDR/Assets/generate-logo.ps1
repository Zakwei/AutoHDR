# Generowanie logo AutoHDR (PNG + ICO)

Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = [System.Drawing.Bitmap]::new($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

# Tło
$g.Clear([System.Drawing.Color]::FromArgb(26, 26, 46))

# Zaokrąglony prostokąt
$path = [System.Drawing.Drawing2D.GraphicsPath]::new()
$rect = [System.Drawing.Rectangle]::new(20, 20, ($size - 40), ($size - 40))
$radius = 40
$path.AddArc($rect.X, $rect.Y, ($radius * 2), ($radius * 2), 180, 90)
$path.AddArc(($rect.Right - $radius * 2), $rect.Y, ($radius * 2), ($radius * 2), 270, 90)
$path.AddArc(($rect.Right - $radius * 2), ($rect.Bottom - $radius * 2), ($radius * 2), ($radius * 2), 0, 90)
$path.AddArc($rect.X, ($rect.Bottom - $radius * 2), ($radius * 2), ($radius * 2), 90, 90)
$path.CloseFigure()

# Gradient
$brush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    $rect,
    [System.Drawing.Color]::FromArgb(22, 199, 132),
    [System.Drawing.Color]::FromArgb(6, 182, 212),
    [System.Drawing.Drawing2D.LinearGradientMode]::Diagonal
)
$g.FillPath($brush, $path)

# Cień pod tekstem
$shadowFont = [System.Drawing.Font]::new("Segoe UI", 86, [System.Drawing.FontStyle]::Bold)
$shadowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(60, 0, 0, 0))
$sf = [System.Drawing.StringFormat]::new()
$sf.Alignment = [System.Drawing.StringAlignment]::Center
$sf.LineAlignment = [System.Drawing.StringAlignment]::Center
$g.DrawString("AH", $shadowFont, $shadowBrush, 132, 138, $sf)

# Tekst
$font = [System.Drawing.Font]::new("Segoe UI", 86, [System.Drawing.FontStyle]::Bold)
$textBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
$g.DrawString("AH", $font, $textBrush, 128, 128, $sf)

# PNG
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$pngPath = Join-Path $root "logo.png"
$bmp.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# ICO (z jednym obrazem 256x256 PNG)
$icoPath = Join-Path $root "logo.ico"
$pngBytes = [System.IO.File]::ReadAllBytes($pngPath)

$ico = [System.Collections.Generic.List[byte]]::new()
# Nagłówek ICONDIR
$ico.AddRange([byte[]]@(0, 0))
$ico.AddRange([byte[]]@(1, 0))
$ico.AddRange([byte[]]@(1, 0))
# ICONDIRENTRY
$ico.Add(0)
$ico.Add(0)
$ico.Add(0)
$ico.Add(0)
$ico.AddRange([BitConverter]::GetBytes([ushort]1))
$ico.AddRange([BitConverter]::GetBytes([ushort]32))
$ico.AddRange([BitConverter]::GetBytes($pngBytes.Length))
$ico.AddRange([BitConverter]::GetBytes(22))
$ico.AddRange($pngBytes)

[System.IO.File]::WriteAllBytes($icoPath, $ico.ToArray())

Write-Host "Utworzono: $pngPath"
Write-Host "Utworzono: $icoPath"
