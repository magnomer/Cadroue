[CmdletBinding()]
param(
    # 0 (default) draws a fresh random seed each run so every generation is different.
    # Pass a positive seed to reproduce one exact set (printed at the end of every run).
    [int]$Seed = 0
)

$ErrorActionPreference = 'Stop'
$outputFolder = $PSScriptRoot
$scratchRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$explicitSeed = $PSBoundParameters.ContainsKey('Seed') -and $Seed -gt 0
$rng = $null

function Get-RandInt([int]$Low, [int]$High) { return $rng.Next($Low, $High + 1) }         # inclusive
function Get-RandPick($Items) { return $Items[$rng.Next(0, $Items.Count)] }

function Invoke-Ffmpeg([string[]]$Arguments) {
    $log = (& ffmpeg -hide_banner -loglevel error @Arguments 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0) {
        throw "ffmpeg failed (exit $LASTEXITCODE): ffmpeg $($Arguments -join ' ')`n$log"
    }
}

# Read the merged ffmpeg/ffprobe log for one file at a chosen verbosity, as a single string.
function Read-FfmpegLog([string[]]$Arguments, [string]$Level = 'error') {
    return (& ffmpeg -hide_banner -nostdin -loglevel $Level @Arguments 2>&1 | Out-String)
}

function Read-FfprobeLog([string[]]$Arguments, [string]$Level = 'error') {
    return (& ffprobe -hide_banner -loglevel $Level @Arguments 2>&1 | Out-String)
}

# Every crafted sample must actually exhibit its defect. A bad random draw that produced a
# clean file would silently weaken the test suite, so each defect is asserted right after it
# is written and the run fails loudly (with the reproducing seed) if the evidence is absent.
function Assert-Match([string]$Defect, [string]$Log, [string]$Pattern) {
    if ($Log -notmatch $Pattern) {
        throw "Sample '$Defect' does not exhibit its defect (seed $Seed): expected /$Pattern/ in`n$Log"
    }
}

function Read-Bytes([string]$Path) {
    # The unary comma keeps ReadAllBytes' byte[] intact; a bare return unrolls it into the
    # pipeline and the caller receives an Object[] of boxed bytes, which silently breaks any
    # helper that mutates a [byte[]] parameter (the length-prefix and duration writers).
    return , ([System.IO.File]::ReadAllBytes($Path))
}

function Write-Bytes([string]$Name, [byte[]]$Bytes) {
    [System.IO.File]::WriteAllBytes((Join-Path $outputFolder $Name), $Bytes)
}

function Find-Ascii([byte[]]$Bytes, [string]$Text, [int]$Start = 0) {
    return Find-Sequence $Bytes ([System.Text.Encoding]::ASCII.GetBytes($Text)) $Start
}

function Find-Sequence([byte[]]$Bytes, [byte[]]$Needle, [int]$Start = 0) {
    for ($i = $Start; $i -le $Bytes.Length - $Needle.Length; $i++) {
        $matched = $true
        for ($j = 0; $j -lt $Needle.Length; $j++) {
            if ($Bytes[$i + $j] -ne $Needle[$j]) {
                $matched = $false
                break
            }
        }

        if ($matched) { return $i }
    }

    return -1
}

function Read-U32BE([byte[]]$Bytes, [int]$Offset) {
    return [uint32](([uint32]$Bytes[$Offset] -shl 24) -bor
        ([uint32]$Bytes[$Offset + 1] -shl 16) -bor
        ([uint32]$Bytes[$Offset + 2] -shl 8) -bor
        [uint32]$Bytes[$Offset + 3])
}

function Write-U32BE([byte[]]$Bytes, [int]$Offset, [uint32]$Value) {
    $Bytes[$Offset] = [byte](($Value -shr 24) -band 0xff)
    $Bytes[$Offset + 1] = [byte](($Value -shr 16) -band 0xff)
    $Bytes[$Offset + 2] = [byte](($Value -shr 8) -band 0xff)
    $Bytes[$Offset + 3] = [byte]($Value -band 0xff)
}

# Some corruptions ride on ffmpeg steps (the SPS/PPS stripping, the synthetic encodes) that
# fail on rare content draws with a plain EINVAL or a native crash. When the seed is automatic,
# a failed attempt is retried with a fresh seed so an unattended generation reliably completes;
# an explicit -Seed is honoured once for reproducibility and never silently re-rolled.
for ($attempt = 1; ; $attempt++) {
    if (-not $explicitSeed) { $Seed = [System.Math]::Abs([System.Guid]::NewGuid().GetHashCode()) }
    $rng = [System.Random]::new($Seed)
    $scratch = Join-Path $scratchRoot ("Cadroue-fix-samples-" + [System.Guid]::NewGuid().ToString('N'))
    try {
    try {
    New-Item -ItemType Directory -Path $scratch | Out-Null

    # Randomized source parameters: each run varies geometry, frame rate, duration, audio
    # pitch and GOP so the corruption logic is exercised against a different byte layout every
    # time, not one frozen fixture.
    $size = Get-RandPick @('160x90', '176x120', '192x108', '208x120', '160x120', '240x135')
    $rate = Get-RandInt 10 18
    # Duration stays high enough that the Matroska segment is large: short clips give
    # single-byte Cue cluster positions that cannot be redirected past end of file, so the
    # index corruption would not actually break seeking.
    $duration = Get-RandInt 6 10
    $gop = Get-RandInt 8 12
    $audioHz = Get-RandInt 300 900
    $audioRate = Get-RandPick @(8000, 11025, 16000)
    # testsrc2 is the one synthetic source that encodes cleanly across every geometry and
    # rate; it already varies enough per size/rate/hue. The corruption coverage comes from
    # randomizing container/codec structure and the mutation itself, not the pixel pattern.
    $hue = Get-RandInt 0 359

    $baseMp4 = Join-Path $scratch 'base.mp4'
    $baseTs = Join-Path $scratch 'base.ts'
    $baseAvi = Join-Path $scratch 'base-mpeg2.avi'
    $baseFfvone = Join-Path $scratch 'base-ffv1.mkv'
    $videoMp4 = Join-Path $scratch 'video.mp4'
    $baseMkv = Join-Path $scratch 'base.mkv'
    $chapters = Join-Path $scratch 'chapters.ffmeta'

    $videoSource = "testsrc2=size=${size}:rate=${rate},hue=h=${hue}"
    Invoke-Ffmpeg @(
        '-f', 'lavfi', '-i', $videoSource,
        '-f', 'lavfi', '-i', "sine=frequency=${audioHz}:sample_rate=${audioRate}",
        '-t', "$duration", '-c:v', 'libx264', '-preset', 'ultrafast', '-g', "$gop",
        '-pix_fmt', 'yuv420p', '-c:a', 'aac', '-y', $baseMp4)
    Invoke-Ffmpeg @('-i', $baseMp4, '-c', 'copy', '-f', 'mpegts', '-y', $baseTs)
    # -bf 0 keeps the MPEG-2 stream free of B-frames so its only defect is the coded-payload
    # mutation below; B-frames would drop per-packet PTS in AVI and add an unintended timing
    # defect on top of the coded one.
    Invoke-Ffmpeg @(
        '-f', 'lavfi', '-i', $videoSource, '-t', "$duration",
        '-c:v', 'mpeg2video', '-g', "$gop", '-bf', '0', '-q:v', '4', '-an', '-y', $baseAvi)
    Invoke-Ffmpeg @(
        '-f', 'lavfi', '-i', $videoSource, '-t', "$duration",
        '-c:v', 'ffv1', '-level', '3', '-coder', '1', '-context', '1',
        '-slicecrc', '1', '-an', '-y', $baseFfvone)
    Invoke-Ffmpeg @(
        '-f', 'lavfi', '-i', $videoSource, '-t', "$duration",
        '-c:v', 'libx264', '-preset', 'ultrafast', '-g', "$gop",
        '-pix_fmt', 'yuv420p', '-an', '-y', $videoMp4)
    Invoke-Ffmpeg @('-i', $baseMp4, '-map', '0', '-c', 'copy', '-y', $baseMkv)

    # Container: zero the size field of the last Matroska Tags element, leaving A/V intact.
    $container = Read-Bytes $baseMkv
    $tags = -1
    $tagSearch = 0
    while (($tagElement = Find-Sequence $container ([byte[]](0x12, 0x54, 0xc3, 0x67)) $tagSearch) -ge 0) {
        $tags = $tagElement
        $tagSearch = $tagElement + 4
    }
    if ($tags -lt 0) { throw 'Matroska Tags element not found' }
    $container[$tags + 4] = 0
    Write-Bytes 'Container.mkv' $container
    Assert-Match 'Container.mkv' (Read-FfmpegLog @('-i', (Join-Path $outputFolder 'Container.mkv'), '-map', '0', '-c', 'copy', '-f', 'null', '-')) 'EBML|invalid|element size'

    # Truncation: keep ftyp and mdat, drop the trailing moov atom.
    $base = Read-Bytes $baseMp4
    $moov = Find-Ascii $base 'moov'
    if ($moov -lt 4) { throw 'moov atom not found in base MP4' }
    $truncated = [byte[]]::new($moov - 4)
    [Array]::Copy($base, $truncated, $truncated.Length)
    Write-Bytes 'Truncation.mp4' $truncated
    Assert-Match 'Truncation.mp4' (Read-FfprobeLog @('-show_error', '-i', (Join-Path $outputFolder 'Truncation.mp4'))) 'moov atom not found'

    # Transport: break a random video continuity counter and a later video PES length.
    $transport = Read-Bytes $baseTs
    $packets = [int]($transport.Length / 188)
    $videoPayloadPackets = [System.Collections.Generic.List[int]]::new()
    for ($packet = 0; $packet -lt $packets; $packet++) {
        $offset = $packet * 188
        if ($transport[$offset] -ne 0x47) { continue }
        $packetPid = (($transport[$offset + 1] -band 0x1f) -shl 8) -bor $transport[$offset + 2]
        $hasPayload = ($transport[$offset + 3] -band 0x10) -ne 0
        if ($packetPid -eq 0x100 -and $hasPayload) { $videoPayloadPackets.Add($offset) }
    }
    if ($videoPayloadPackets.Count -lt 12) { throw 'Not enough video transport packets' }
    $continuityTarget = $videoPayloadPackets[(Get-RandInt 8 ($videoPayloadPackets.Count - 3))]
    $counter = $transport[$continuityTarget + 3] -band 0x0f
    $skip = Get-RandInt 3 9
    $transport[$continuityTarget + 3] = [byte](
        ($transport[$continuityTarget + 3] -band 0xf0) -bor (($counter + $skip) -band 0x0f))

    $pesHeaders = [System.Collections.Generic.List[int]]::new()
    for ($packet = 0; $packet -lt $packets; $packet++) {
        $offset = $packet * 188
        $packetPid = (($transport[$offset + 1] -band 0x1f) -shl 8) -bor $transport[$offset + 2]
        $payloadStart = ($transport[$offset + 1] -band 0x40) -ne 0
        if ($packetPid -ne 0x100 -or -not $payloadStart) { continue }
        $adaptation = ($transport[$offset + 3] -band 0x20) -ne 0
        $payload = $offset + 4
        if ($adaptation) { $payload += 1 + $transport[$offset + 4] }
        if ($payload + 6 -ge $offset + 188) { continue }
        if ($transport[$payload] -eq 0 -and $transport[$payload + 1] -eq 0 -and $transport[$payload + 2] -eq 1) {
            $pesHeaders.Add($payload)
        }
    }
    if ($pesHeaders.Count -lt 3) { throw 'Not enough video PES headers' }
    $pesTarget = $pesHeaders[(Get-RandInt 2 ($pesHeaders.Count - 1))]
    $transport[$pesTarget + 4] = 0
    $transport[$pesTarget + 5] = 1
    Write-Bytes 'Transport.ts' $transport
    Assert-Match 'Transport.ts' (Read-FfmpegLog @('-i', (Join-Path $outputFolder 'Transport.ts'), '-map', '0', '-c', 'copy', '-f', 'null', '-') 'warning') 'PES packet|corrupt'

    # Metadata: make one MP4 track declare a stretched timeline over the real essence.
    $factor = Get-RandInt 4 8
    $metadata = [byte[]]$base.Clone()
    $stts = Find-Ascii $metadata 'stts'
    $mvhd = Find-Ascii $metadata 'mvhd'
    $tkhd = Find-Ascii $metadata 'tkhd'
    $mdhd = Find-Ascii $metadata 'mdhd'
    if ($stts -lt 4 -or $mvhd -lt 4 -or $tkhd -lt 4 -or $mdhd -lt 4) {
        throw 'Required MP4 timing atom not found'
    }
    $sampleDeltaOffset = $stts + 16
    Write-U32BE $metadata $sampleDeltaOffset ((Read-U32BE $metadata $sampleDeltaOffset) * $factor)
    $movieTimescale = Read-U32BE $metadata ($mvhd + 16)
    Write-U32BE $metadata ($tkhd + 24) ([uint32]($movieTimescale * $duration * $factor))
    $mediaTimescale = Read-U32BE $metadata ($mdhd + 16)
    Write-U32BE $metadata ($mdhd + 20) ([uint32]($mediaTimescale * $duration * $factor))
    $elst = Find-Ascii $metadata 'elst'
    if ($elst -gt 0) { Write-U32BE $metadata ($elst + 12) ([uint32]($movieTimescale * $duration * $factor)) }
    Write-Bytes 'Metadata.mp4' $metadata
    $metaVideo = [double](Read-FfprobeLog @('-select_streams', 'v', '-show_entries', 'stream=duration', '-of', 'csv=p=0', '-i', (Join-Path $outputFolder 'Metadata.mp4'))).Trim()
    if ($metaVideo -lt ($duration * 2)) { throw "Metadata.mp4 timeline not stretched (seed $Seed): video=$metaVideo" }

    # Index: redirect Matroska CueClusterPosition entries far beyond EOF.
    $indexed = Read-Bytes $baseMkv
    $cues = -1
    $cueSearch = 0
    while (($cueElement = Find-Sequence $indexed ([byte[]](0x1c, 0x53, 0xbb, 0x6b)) $cueSearch) -ge 0) {
        $cues = $cueElement
        $cueSearch = $cueElement + 4
    }
    if ($cues -lt 0) { throw 'Matroska Cues element not found' }
    $search = $cues + 4
    $cueCount = 0
    $fill = [byte](Get-RandInt 0x70 0x7f)
    while (($cue = Find-Sequence $indexed ([byte[]](0xf1)) $search) -ge 0) {
        $sizeMarker = $indexed[$cue + 1]
        $sizeLength = 1
        while ($sizeLength -le 8 -and ($sizeMarker -band (0x100 -shr $sizeLength)) -eq 0) {
            $sizeLength++
        }
        if ($sizeLength -gt 8) { break }
        $valueLength = $sizeMarker -band (0xff -shr $sizeLength)
        for ($sizeByte = 1; $sizeByte -lt $sizeLength; $sizeByte++) {
            $valueLength = ($valueLength -shl 8) -bor $indexed[$cue + 1 + $sizeByte]
        }
        if ($valueLength -gt 0 -and $valueLength -le 8) {
            for ($valueByte = 0; $valueByte -lt $valueLength; $valueByte++) {
                $indexed[$cue + 1 + $sizeLength + $valueByte] = $fill
            }
            $cueCount++
        }
        $search = $cue + 2
    }
    if ($cueCount -eq 0) { throw 'No CueClusterPosition entries found' }
    Write-Bytes 'Index.mkv' $indexed
    # Verify with the exact probe the scanner's index detector uses (a seek to one second
    # before end, forcing a Cue lookup), so a sample that passes generation is one the
    # scanner will flag.
    $indexSeek = Read-FfmpegLog @('-sseof', '-1', '-i', (Join-Path $outputFolder 'Index.mkv'), '-map', '0', '-c', 'copy', '-f', 'null', '-')
    if ([string]::IsNullOrWhiteSpace($indexSeek)) { throw "Index.mkv seek did not fail (seed $Seed)" }

    # Framing: keep the MP4/H.264 declaration but make the first NAL length impossible.
    $framing = Read-Bytes $videoMp4
    $mdat = Find-Ascii $framing 'mdat'
    if ($mdat -lt 4) { throw 'mdat atom not found in video MP4' }
    $badLength = [uint32](0x40000000 -bor (Get-RandInt 1 0x3fffffff))
    Write-U32BE $framing ($mdat + 4) $badLength
    Write-Bytes 'Framing.mp4' $framing
    Assert-Match 'Framing.mp4' (Read-FfmpegLog @('-i', (Join-Path $outputFolder 'Framing.mp4'), '-map', '0', '-c', 'copy', '-f', 'null', '-')) 'NAL unit size'

    # Configuration: strip every H.264 SPS and PPS from an Annex-B stream.
    $configPath = Join-Path $outputFolder 'Configuration.h264'
    Invoke-Ffmpeg @(
        '-i', $baseMp4, '-an', '-c:v', 'copy', '-bsf:v', 'filter_units=remove_types=7|8',
        '-f', 'h264', '-y', $configPath)
    Assert-Match 'Configuration.h264' (Read-FfprobeLog @('-i', $configPath) 'error') 'non-existing (PPS|SPS)|SPS|PPS'

    # Timing: an AVI MPEG-4 stream with B-frames yields packets that carry DTS but no PTS.
    $bframes = Get-RandInt 2 3
    $timingPath = Join-Path $outputFolder 'Timing.avi'
    Invoke-Ffmpeg @(
        '-f', 'lavfi', '-i', $videoSource, '-t', "$duration",
        '-c:v', 'mpeg4', '-bf', "$bframes", '-g', "$gop", '-q:v', '4', '-an', '-y', $timingPath)
    $timingPts = & ffprobe -hide_banner -v error -show_packets -show_entries packet=pts -of csv=p=0 $timingPath 2>&1
    $ptsMissing = ($timingPts | Where-Object { $_ -match 'N/A' }).Count
    $ptsPresent = ($timingPts | Where-Object { $_ -notmatch 'N/A' -and $_.Trim() -ne '' }).Count
    if ($ptsMissing -lt 1 -or $ptsPresent -lt 1) {
        throw "Timing.avi is not a partial-PTS defect (seed $Seed): missing=$ptsMissing present=$ptsPresent"
    }

    # Secondary data: two deliberately overlapping Matroska chapters.
    $firstEnd = Get-RandInt 1500 2500
    $secondStart = Get-RandInt 500 ($firstEnd - 200)
    $secondEnd = $firstEnd + (Get-RandInt 500 1500)
    [System.IO.File]::WriteAllText($chapters, @"
;FFMETADATA1
[CHAPTER]
TIMEBASE=1/1000
START=0
END=$firstEnd
title=First
[CHAPTER]
TIMEBASE=1/1000
START=$secondStart
END=$secondEnd
title=Overlapping
"@)
    Invoke-Ffmpeg @(
        '-i', $baseMp4, '-i', $chapters, '-map', '0', '-map_metadata', '1',
        '-map_chapters', '1', '-c', 'copy', '-y', (Join-Path $outputFolder 'Secondary data.mkv'))

    # Coded media: mutate MPEG-2 coded payload while keeping the AVI container readable.
    $codedAmount = Get-RandInt 60 100
    $codedPath = Join-Path $outputFolder 'Coded media.avi'
    Invoke-Ffmpeg @(
        '-i', $baseAvi, '-c', 'copy', '-bsf:v', "noise=amount=$codedAmount",
        '-y', $codedPath)
    Assert-Match 'Coded media.avi' (Read-FfmpegLog @('-err_detect', '+explode', '-i', $codedPath, '-an', '-map', '0:v?', '-f', 'null', '-')) 'error while decoding|damaged|slice|Invalid data'

    # FFV1 integrity: mutate CRC-protected FFV1 slices without removing the FFV1 declaration.
    $ffvoneAmount = Get-RandInt 60 100
    $ffvonePath = Join-Path $outputFolder 'FFV1 integrity.mkv'
    Invoke-Ffmpeg @(
        '-i', $baseFfvone, '-c', 'copy', '-bsf:v', "noise=amount=$ffvoneAmount",
        '-y', $ffvonePath)
    Assert-Match 'FFV1 integrity.mkv' (Read-FfmpegLog @('-err_detect', '+crccheck', '-i', $ffvonePath, '-an', '-map', '0:v?', '-f', 'null', '-')) 'CRC'

    Write-Host "Generated with seed $Seed (rerun with -Seed $Seed to reproduce)."
    Get-ChildItem -LiteralPath $outputFolder -File |
        Where-Object Name -ne 'make-fix-corruptions.ps1' |
        Sort-Object Name |
        Select-Object Name, Length
    }
    finally {
        if (Test-Path -LiteralPath $scratch) {
            $resolvedScratch = [System.IO.Path]::GetFullPath($scratch)
            if ($resolvedScratch.StartsWith($scratchRoot, [StringComparison]::OrdinalIgnoreCase) -and
                [System.IO.Path]::GetFileName($resolvedScratch).StartsWith('Cadroue-fix-samples-', [StringComparison]::Ordinal)) {
                Remove-Item -LiteralPath $resolvedScratch -Recurse -Force
            }
        }
    }
    break
    }
    catch {
        if ($explicitSeed -or $attempt -ge 6) { throw }
        $reason = ($_.Exception.Message -split "`n")[0]
        Write-Host "Generation attempt $attempt failed ($reason); retrying with a fresh seed."
    }
}
