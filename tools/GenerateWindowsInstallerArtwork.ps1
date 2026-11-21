param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

Add-Type -AssemblyName System.Drawing

$null = [System.IO.Directory]::CreateDirectory($OutputDirectory)
$artworkDirectory = Join-Path $PSScriptRoot '..\packaging\windows\artwork'
$logo = [System.Drawing.Image]::FromFile((Resolve-Path (Join-Path $artworkDirectory 'lizard-keyboard-sounds-icon.png')).Path)
$backgroundImage = [System.Drawing.Image]::FromFile((Resolve-Path (Join-Path $artworkDirectory 'installer-background.png')).Path)

function New-Brush([string]$Color) {
    return [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml($Color))
}

function New-Font([float]$Size, [System.Drawing.FontStyle]$Style = [System.Drawing.FontStyle]::Regular) {
    return [System.Drawing.Font]::new('Segoe UI', $Size, $Style, [System.Drawing.GraphicsUnit]::Pixel)
}

try {
    $banner = [System.Drawing.Bitmap]::new(493, 58)
    $bannerGraphics = [System.Drawing.Graphics]::FromImage($banner)
    $bannerGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $bannerGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $bannerGraphics.Clear([System.Drawing.ColorTranslator]::FromHtml('#11140F'))
    $bannerGraphics.DrawImage($logo, [System.Drawing.Rectangle]::new(7, 5, 48, 48))
    $bannerGraphics.DrawString('Lizard Keyboard Sounds', (New-Font 15 ([System.Drawing.FontStyle]::Bold)), (New-Brush '#F4F3EA'), 65, 7)
    $bannerGraphics.DrawString('PERSONAL KEYBOARD AUDIO', (New-Font 8.5 ([System.Drawing.FontStyle]::Bold)), (New-Brush '#C9F36B'), 66, 33)
    $bannerGraphics.FillRectangle((New-Brush '#C9F36B'), 0, 56, 493, 2)
    $banner.Save((Join-Path $OutputDirectory 'msi-top-banner.bmp'), [System.Drawing.Imaging.ImageFormat]::Bmp)
    $bannerGraphics.Dispose()
    $banner.Dispose()

    $dialogBackground = [System.Drawing.Bitmap]::new(493, 312)
    $dialogGraphics = [System.Drawing.Graphics]::FromImage($dialogBackground)
    $dialogGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $dialogGraphics.DrawImage($backgroundImage, [System.Drawing.Rectangle]::new(0, 0, 493, 312))
    $dialogGraphics.FillRectangle((New-Brush '#C9F36B'), 0, 307, 493, 5)
    $dialogBackground.Save((Join-Path $OutputDirectory 'msi-dialog-background.bmp'), [System.Drawing.Imaging.ImageFormat]::Bmp)
    $dialogGraphics.Dispose()
    $dialogBackground.Dispose()
}
finally {
    $logo.Dispose()
    $backgroundImage.Dispose()
}
