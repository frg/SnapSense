// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.Extensions.Logging;
using Rectangle = System.Drawing.Rectangle;

namespace SnapSense;

public class FaceDetector
{
    private readonly ILogger<FaceDetector> _logger;

    private CascadeClassifier _faceCascade;

    public FaceDetector(ILogger<FaceDetector> logger)
    {
        _logger = logger;
        _faceCascade = new CascadeClassifier("opencv/data/haarcascades/haarcascade_frontalface_default.xml");
    }

    public Rectangle[] LocateFaces(Mat frameMat)
    {
        var grayFrame = new Mat();
        CvInvoke.CvtColor(frameMat, grayFrame, ColorConversion.Bgr2Gray);
        var faces = _faceCascade.DetectMultiScale(grayFrame, 1.1, 10, System.Drawing.Size.Empty);

        return faces;
    }

    public Mat MarkFaces(Mat frameMat, Rectangle[] faces)
    {
        var clonedFrameMat = frameMat.Clone();
        foreach (var face in faces)
        {
            var color = GenerateRandomHighlighterColor();
            CvInvoke.Rectangle(clonedFrameMat, face, color, 1);
        }

        return clonedFrameMat;
    }

    private static readonly Random _defaultRandom = new(DateTime.UtcNow.Microsecond);
    public static MCvScalar GenerateRandomHighlighterColor(Random? random = null)
    {
        random ??= _defaultRandom;

        var baseValue = (byte)random.Next(200, 256);
        var red = baseValue;
        var green = (byte)random.Next(150, 256);
        var blue = (byte)random.Next(0, 100);

        var colors = new byte[] { red, green, blue };
        colors = colors.OrderBy(c => random.Next()).ToArray();

        return new MCvScalar(colors[0], colors[1], colors[2]);
    }
}
