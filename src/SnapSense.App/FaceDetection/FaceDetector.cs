// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using Microsoft.Extensions.Logging;
using Rectangle = System.Drawing.Rectangle;

namespace SnapSense.FaceDetection;

public class FaceDetector
{
    private readonly ILogger<FaceDetector> _logger;
    private readonly Net _dnnFaceDetector;

    private static readonly Random _defaultRandom = new(DateTime.UtcNow.Microsecond);

    public FaceDetector(ILogger<FaceDetector> logger)
    {
        _logger = logger;
        _dnnFaceDetector = DnnInvoke.ReadNetFromCaffe(
            "opencv/data/dnn/deploy.prototxt",
            "opencv/data/dnn/res10_300x300_ssd_iter_140000_fp16.caffemodel"
        );
    }

    public IEnumerable<DetectedObject> LocateFaces(Mat frameMat, double confidenceThreshold = 0.95)
    {
        using var grayFrame = new Mat();
        CvInvoke.CvtColor(frameMat, grayFrame, ColorConversion.Bgr2Gray);
        CvInvoke.EqualizeHist(grayFrame, grayFrame);

        using (var blob = DnnInvoke.BlobFromImage(frameMat,
                   1.0,
                   new Size(300,
                       300),
                   new MCvScalar(104.0,
                       177.0,
                       123.0),
                   false,
                   false))
        {
            _dnnFaceDetector.SetInput(blob);
        }

        var detection = _dnnFaceDetector.Forward();

        var faces = new List<DetectedObject>();

        detection = detection.Reshape(1, (int)detection.Total);

        var data = new float[detection.Total];
        Marshal.Copy(detection.DataPointer, data, 0, data.Length);

        for (var i = 0; i < data.Length; i += 7)
        {
            var confidence = data[i + 2];
            if (!(confidence > confidenceThreshold))
            {
                continue;
            }

            var x1 = (int)(data[i + 3] * frameMat.Cols);
            var y1 = (int)(data[i + 4] * frameMat.Rows);
            var x2 = (int)(data[i + 5] * frameMat.Cols);
            var y2 = (int)(data[i + 6] * frameMat.Rows);
            faces.Add(new DetectedObject(new Rectangle(x1, y1, x2 - x1, y2 - y1), confidence));
        }

        return faces;
    }

    public Mat MarkFaces(Mat frameMat, IEnumerable<DetectedObject> faces)
    {
        var clonedFrameMat = frameMat.Clone();
        foreach (var face in faces)
        {
            var color = GenerateRandomHighlighterColor();
            CvInvoke.Rectangle(clonedFrameMat, face.Position, color, 1);

            // Draw the confidence level text
            var text = $"Confidence: {face.Confidence.ToString("0.00", CultureInfo.InvariantCulture)}";
            var point = new Point(face.Position.X, face.Position.Y - 10); // Position above the rectangle
            const FontFace textFont = FontFace.HersheySimplex;
            const double textFontSize = 0.4;
            const int textFontThickness = 1;
            var baseline = 0;
            var textSize = CvInvoke.GetTextSize(text, textFont, textFontSize, textFontThickness, ref baseline);
            var textBackground = new Rectangle(point.X, point.Y - textSize.Height, textSize.Width, textSize.Height + baseline);

            // Draw text background for better legibility
            CvInvoke.Rectangle(clonedFrameMat, textBackground, new MCvScalar(75, 75, 75), -1);

            // Draw the text
            CvInvoke.PutText(clonedFrameMat, text, point, textFont, textFontSize, new MCvScalar(255, 255, 255), textFontThickness);
        }

        return clonedFrameMat;
    }

    private static MCvScalar GenerateRandomHighlighterColor(Random? random = null)
    {
        random ??= _defaultRandom;

        var baseValue = (byte)random.Next(200, 256);
        var red = baseValue;
        var green = (byte)random.Next(150, 256);
        var blue = (byte)random.Next(0, 100);

        var colors = new[] { red, green, blue };
        colors = colors.OrderBy(c => random.Next()).ToArray();

        return new MCvScalar(colors[0], colors[1], colors[2]);
    }
}
