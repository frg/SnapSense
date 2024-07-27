// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using FaceAiSharp;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SnapSense;

public class FaceDetector
{
    private readonly ILogger<FaceDetector> _logger;
    private readonly IFaceDetector _detector = FaceAiSharpBundleFactory.CreateFaceDetector();
    private readonly IFaceDetectorWithLandmarks _detectorWithLandmarks = FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks();

    public FaceDetector(ILogger<FaceDetector> logger)
    {
        _logger = logger;
    }

    public bool HasAnyFace(Image<Rgb24> image, float confidenceThreshold = 1.45F)
    {
        var faces = _detector.DetectFaces(image);
        var hasAnyFace = faces.Any(result => (result.Confidence ?? 0) > confidenceThreshold);

        if (hasAnyFace)
        {
            _logger.LogInformation("Found at least one face which meets confidence criteria.");
        }

        return hasAnyFace;
    }

    public Image<T> MarkFaces<T>(Image<T> image) where T : unmanaged, IPixel<T>
    {
        var clonedImage = image.CloneAs<Rgb24>();
        var faces = _detectorWithLandmarks.DetectFaces(clonedImage);

        _logger.LogInformation("Found {FaceCount} faces, with confidence {FaceConfidences} respectively.", faces.Count, faces.Select(x => x.Confidence));

        var font = SixLabors.Fonts.SystemFonts.CreateFont("Arial", 12);

        _logger.LogInformation("Marking faces...");

        foreach (var face in faces)
        {
            var color = SixLaborsUtils.GenerateRandomHighlighterColor();
            var rectangle = new SixLabors.ImageSharp.Drawing.RectangularPolygon((int)face.Box.X, (int)face.Box.Y, (int)face.Box.Width, (int)face.Box.Height);

            clonedImage.Mutate(ctx => ctx.Draw(color, 2, rectangle));

            var confidenceText = $"Confidence: {face.Confidence:F2}";
            var location = new Vector2(face.Box.X, face.Box.Y - 20);

            clonedImage.Mutate(ctx => ctx.DrawText(confidenceText, font, color, location));
        }

        // TODO: Do not clone if type is Rgb24 (which is likely)
        var converted = clonedImage.CloneAs<T>();
        clonedImage.Dispose();

        return converted;
    }
}
