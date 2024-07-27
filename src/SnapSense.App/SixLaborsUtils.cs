// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Drawing.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace SnapSense;

public static class SixLaborsUtils
{
    private static readonly Random _defaultRandom = new(DateTime.UtcNow.Microsecond);

    public static Image<T> ConvertTo<T>(this Bitmap bitmap) where T : unmanaged, IPixel<T>
    {
        using var stream = new MemoryStream();
        // TODO: make this platform agnostic
        bitmap.Save(stream, ImageFormat.Png);
        stream.Seek(0, SeekOrigin.Begin);
        return Image.Load<T>(stream);
    }

    public static Rgb24 GenerateRandomHighlighterColor(Random? random = null)
    {
        random ??= _defaultRandom;

        var baseValue = (byte)random.Next(200, 256);
        var red = baseValue;
        var green = (byte)random.Next(150, 256);
        var blue = (byte)random.Next(0, 100);

        var colors = new byte[] { red, green, blue };
        colors = colors.OrderBy(c => random.Next()).ToArray();

        return new Rgb24(colors[0], colors[1], colors[2]);
    }
}

