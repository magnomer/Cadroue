using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests.Conduct;

internal static partial class TInterface
{
    internal static LRosterRow TRosterRowCreate(
        Guid id,
        bool high,
        TimeSpan length,
        bool running,
        double progress,
        string token,
        bool claimed,
        bool owned,
        bool local) => new(id, high, length, running, progress, token, claimed, owned, local);
    internal static LRosterSide TRosterSideCreate(
        bool media,
        bool video,
        int width,
        int height,
        double framerate,
        double? keyframe,
        TimeSpan? duration,
        string? codec,
        double? kilobits,
        int? samplerate,
        double? loudness,
        string path) =>
        new(media, video, width, height, framerate, keyframe, duration, codec, kilobits, samplerate, loudness, path);
    internal static LRosterEncoding TRosterEncodingCreate(
        string mode,
        string stream,
        bool reencode,
        string encoder,
        string control,
        string quality,
        string? speed,
        string pixel,
        string size,
        string framerate,
        IReadOnlyDictionary<string, string>? extras,
        string samplerate,
        string channels) =>
        new(
            mode,
            stream,
            reencode,
            encoder,
            control,
            quality,
            speed,
            pixel,
            size,
            framerate,
            extras,
            samplerate,
            channels);
}
