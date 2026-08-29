[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$outputFolder = $PSScriptRoot
$scratchRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$scratch = Join-Path $scratchRoot ("Cadroue-fix-samples-" + [Guid]::NewGuid().ToString('N'))

function Invoke-Ffmpeg([string[]]$Arguments) {
    & ffmpeg -hide_banner -loglevel error @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "ffmpeg failed with exit code $LASTEXITCODE"
    }
}

function Read-Bytes([string]$Path) {
    return [System.IO.File]::ReadAllBytes($Path)
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

try {
    New-Item -ItemType Directory -Path $scratch | Out-Null

    $baseMp4 = Join-Path $scratch 'base.mp4'
    $baseTs = Join-Path $scratch 'base.ts'
    $baseAvi = Join-Path $scratch 'base-mpeg2.avi'
    $baseFfvone = Join-Path $scratch 'base-ffv1.mkv'
    $videoMp4 = Join-Path $scratch 'video.mp4'
    $baseMkv = Join-Path $scratch 'base.mkv'
    $chapters = Join-Path $scratch 'chapters.ffmeta'

    Invoke-Ffmpeg @(
        '-f', 'lavfi', '-i', 'testsrc2=size=160x90:rate=10',
        '-f', 'lavfi', '-i', 'sine=frequency=440:sample_rate=8000',
        '-t', '4', '-c:v', 'libx264', '-preset', 'ultrafast', '-g', '10',
        '-pix_fmt', 'yuv420p', '-c:a', 'aac', '-y', $baseMp4)
    Invoke-Ffmpeg @('-i', $baseMp4, '-c', 'copy', '-f', 'mpegts', '-y', $baseTs)
    Invoke-Ffmpeg @(
        '-f', 'lavfi', '-i', 'testsrc2=size=160x90:rate=10', '-t', '4',
        '-c:v', 'mpeg2video', '-g', '10', '-q:v', '4', '-an', '-y', $baseAvi)
    Invoke-Ffmpeg @(
        '-f', 'lavfi', '-i', 'testsrc2=size=160x90:rate=10', '-t', '4',
        '-c:v', 'ffv1', '-level', '3', '-coder', '1', '-context', '1',
        '-slicecrc', '1', '-an', '-y', $baseFfvone)
    Invoke-Ffmpeg @(
        '-f', 'lavfi', '-i', 'testsrc2=size=160x90:rate=10', '-t', '4',
        '-c:v', 'libx264', '-preset', 'ultrafast', '-g', '10',
        '-pix_fmt', 'yuv420p', '-an', '-y', $videoMp4)
    Invoke-Ffmpeg @('-i', $baseMp4, '-map', '0', '-c', 'copy', '-y', $baseMkv)

    # Container: invalid Matroska Tags element size, with the A/V tracks retained.
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

    # Truncation: retain ftyp and mdat, then remove the final moov atom.
    $base = Read-Bytes $baseMp4
    $moov = Find-Ascii $base 'moov'
    if ($moov -lt 4) { throw 'moov atom not found in base MP4' }
    $truncated = [byte[]]::new($moov - 4)
    [Array]::Copy($base, $truncated, $truncated.Length)
    Write-Bytes 'Truncation.mp4' $truncated

    # Transport: break a continuity counter and a later video PES length.
    $transport = Read-Bytes $baseTs
    $packets = [int]($transport.Length / 188)
    $seen = 0
    for ($packet = 0; $packet -lt $packets; $packet++) {
        $offset = $packet * 188
        if ($transport[$offset] -ne 0x47) { continue }
        $packetPid = (($transport[$offset + 1] -band 0x1f) -shl 8) -bor $transport[$offset + 2]
        $hasPayload = ($transport[$offset + 3] -band 0x10) -ne 0
        if ($packetPid -eq 0x100 -and $hasPayload) {
            $seen++
            if ($seen -eq 25) {
                $counter = $transport[$offset + 3] -band 0x0f
                $transport[$offset + 3] = [byte](
                    ($transport[$offset + 3] -band 0xf0) -bor (($counter + 5) -band 0x0f))
                break
            }
        }
    }
    if ($seen -lt 25) { throw 'Not enough video transport packets' }

    $pesSeen = 0
    for ($packet = 0; $packet -lt $packets; $packet++) {
        $offset = $packet * 188
        $packetPid = (($transport[$offset + 1] -band 0x1f) -shl 8) -bor $transport[$offset + 2]
        $payloadStart = ($transport[$offset + 1] -band 0x40) -ne 0
        if ($packetPid -ne 0x100 -or -not $payloadStart) { continue }
        $adaptation = ($transport[$offset + 3] -band 0x20) -ne 0
        $payload = $offset + 4
        if ($adaptation) { $payload += 1 + $transport[$offset + 4] }
        if ($payload + 6 -ge $offset + 188) { continue }
        if ($transport[$payload] -eq 0 -and
            $transport[$payload + 1] -eq 0 -and
            $transport[$payload + 2] -eq 1) {
            $pesSeen++
            if ($pesSeen -eq 3) {
                $transport[$payload + 4] = 0
                $transport[$payload + 5] = 1
                break
            }
        }
    }
    if ($pesSeen -lt 3) { throw 'Not enough video PES headers' }
    Write-Bytes 'Transport.ts' $transport

    # Metadata: make one MP4 track declare a 20-second timeline over 4 seconds of essence.
    $metadata = [byte[]]$base.Clone()
    $stts = Find-Ascii $metadata 'stts'
    $mvhd = Find-Ascii $metadata 'mvhd'
    $tkhd = Find-Ascii $metadata 'tkhd'
    $mdhd = Find-Ascii $metadata 'mdhd'
    if ($stts -lt 4 -or $mvhd -lt 4 -or $tkhd -lt 4 -or $mdhd -lt 4) {
        throw 'Required MP4 timing atom not found'
    }
    $sampleDeltaOffset = $stts + 16
    Write-U32BE $metadata $sampleDeltaOffset ((Read-U32BE $metadata $sampleDeltaOffset) * 5)
    $movieTimescale = Read-U32BE $metadata ($mvhd + 16)
    Write-U32BE $metadata ($tkhd + 24) ([uint32]($movieTimescale * 20))
    $mediaTimescale = Read-U32BE $metadata ($mdhd + 16)
    Write-U32BE $metadata ($mdhd + 20) ([uint32]($mediaTimescale * 20))
    $elst = Find-Ascii $metadata 'elst'
    if ($elst -gt 0) { Write-U32BE $metadata ($elst + 12) ([uint32]($movieTimescale * 20)) }
    Write-Bytes 'Metadata.mp4' $metadata

    # Index: redirect Matroska CueClusterPosition entries beyond EOF.
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
                $indexed[$cue + 1 + $sizeLength + $valueByte] = 0x7f
            }
            $cueCount++
        }
        $search = $cue + 2
    }
    if ($cueCount -eq 0) { throw 'No CueClusterPosition entries found' }
    Write-Bytes 'Index.mkv' $indexed

    # Framing: retain the MP4/H.264 declaration but make the first NAL length impossible.
    $framing = Read-Bytes $videoMp4
    $mdat = Find-Ascii $framing 'mdat'
    if ($mdat -lt 4) { throw 'mdat atom not found in video MP4' }
    Write-U32BE $framing ($mdat + 4) 0x7fffffff
    Write-Bytes 'Framing.mp4' $framing

    # Configuration: remove every H.264 SPS and PPS from an Annex-B stream.
    Invoke-Ffmpeg @(
        '-i', $baseMp4, '-an', '-c:v', 'copy', '-bsf:v', 'filter_units=remove_types=7|8',
        '-f', 'h264', '-y', (Join-Path $outputFolder 'Configuration.h264'))

    # Timing: AVI with B-frames exposes packets that have DTS but no PTS.
    Invoke-Ffmpeg @(
        '-f', 'lavfi', '-i', 'testsrc2=size=160x90:rate=10', '-t', '4',
        '-c:v', 'mpeg4', '-bf', '2', '-g', '10', '-q:v', '4', '-an',
        '-y', (Join-Path $outputFolder 'Timing.avi'))

    # Secondary data: two deliberately overlapping Matroska chapters.
    [System.IO.File]::WriteAllText($chapters, @'
;FFMETADATA1
[CHAPTER]
TIMEBASE=1/1000
START=0
END=2000
title=First
[CHAPTER]
TIMEBASE=1/1000
START=1000
END=3000
title=Overlapping
'@)
    Invoke-Ffmpeg @(
        '-i', $baseMp4, '-i', $chapters, '-map', '0', '-map_metadata', '1',
        '-map_chapters', '1', '-c', 'copy', '-y', (Join-Path $outputFolder 'Secondary data.mkv'))

    # Coded media: mutate MPEG-2 coded payload while keeping the AVI container readable.
    Invoke-Ffmpeg @(
        '-i', $baseAvi, '-c', 'copy', '-bsf:v', 'noise=amount=100',
        '-y', (Join-Path $outputFolder 'Coded media.avi'))

    # FFV1 integrity: mutate CRC-protected FFV1 slices without removing the FFV1 declaration.
    Invoke-Ffmpeg @(
        '-i', $baseFfvone, '-c', 'copy', '-bsf:v', 'noise=amount=100',
        '-y', (Join-Path $outputFolder 'FFV1 integrity.mkv'))

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
