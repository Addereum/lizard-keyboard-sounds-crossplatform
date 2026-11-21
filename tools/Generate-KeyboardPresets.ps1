$ErrorActionPreference = 'Stop'
$sampleRate = 44100
$duration = 0.24
$count = [int]($sampleRate * $duration)
$outputDir = Join-Path $PSScriptRoot '..\Presets'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

function New-Preset([string]$name, [string]$profile) {
    $seed = if ($profile -eq 'thock') { 1729 } else { 31337 }
    $rng = [Random]::new($seed)
    $samples = [double[]]::new($count)
    $filteredNoise = 0.0
    $previousNoise = 0.0
    $peak = 0.0

    for ($i = 0; $i -lt $count; $i++) {
        $t = $i / [double]$sampleRate
        $noise = ($rng.NextDouble() * 2.0) - 1.0
        $sample = 0.0
        if ($profile -eq 'thock') {
            $sample += 0.34 * [Math]::Sin(2 * [Math]::PI * 82 * $t) * [Math]::Exp(-$t / 0.105)
            $sample += 0.46 * [Math]::Sin(2 * [Math]::PI * 178 * $t + 0.25) * [Math]::Exp(-$t / 0.067)
            $sample += 0.20 * [Math]::Sin(2 * [Math]::PI * 410 * $t + 0.55) * [Math]::Exp(-$t / 0.040)
            $sample += 0.075 * [Math]::Sin(2 * [Math]::PI * 1320 * $t) * [Math]::Exp(-$t / 0.009)
            $sample += 0.12 * ($noise - $previousNoise) * [Math]::Exp(-$t / 0.004)
            $previousNoise = $noise
        }
        else {
            $filteredNoise = 0.17 * $noise + 0.83 * $filteredNoise
            $sample += 0.28 * [Math]::Sin(2 * [Math]::PI * 118 * $t + 0.2) * [Math]::Exp(-$t / 0.16)
            $sample += 0.43 * [Math]::Sin(2 * [Math]::PI * 236 * $t + 0.45) * [Math]::Exp(-$t / 0.105)
            $sample += 0.24 * [Math]::Sin(2 * [Math]::PI * 505 * $t + 0.8) * [Math]::Exp(-$t / 0.069)
            $sample += 0.085 * [Math]::Sin(2 * [Math]::PI * 980 * $t) * [Math]::Exp(-$t / 0.028)
            $sample += 0.10 * $filteredNoise * [Math]::Exp(-$t / 0.006)
        }

        # A short, quiet reflection gives the body a little space without muddying fast typing.
        $reflectionDelay = [int](0.013 * $sampleRate)
        if ($i -ge $reflectionDelay) {
            $sample += $samples[$i - $reflectionDelay] * 0.075
        }
        $samples[$i] = $sample
        $peak = [Math]::Max($peak, [Math]::Abs($sample))
    }

    $path = Join-Path $outputDir "$name.wav"
    $stream = [IO.File]::Create($path)
    $writer = [IO.BinaryWriter]::new($stream)
    try {
        $dataSize = $count * 2 * 2 # 16-bit stereo PCM
        $writer.Write([Text.Encoding]::ASCII.GetBytes('RIFF'))
        $writer.Write([int](36 + $dataSize))
        $writer.Write([Text.Encoding]::ASCII.GetBytes('WAVE'))
        $writer.Write([Text.Encoding]::ASCII.GetBytes('fmt '))
        $writer.Write([int]16)
        $writer.Write([short]1)
        $writer.Write([short]2)
        $writer.Write([int]$sampleRate)
        $writer.Write([int]($sampleRate * 4))
        $writer.Write([short]4)
        $writer.Write([short]16)
        $writer.Write([Text.Encoding]::ASCII.GetBytes('data'))
        $writer.Write([int]$dataSize)
        $gain = 0.74 / $peak
        foreach ($sample in $samples) {
            $value = [Math]::Max(-1.0, [Math]::Min(1.0, $sample * $gain))
            $pcm = [int][Math]::Round($value * 32767)
            $writer.Write([short]$pcm)
            $writer.Write([short]$pcm)
        }
    }
    finally { $writer.Dispose() }
    Write-Output "$name : $path"
}

New-Preset 'Thock' 'thock'
New-Preset 'Creamy' 'creamy'
