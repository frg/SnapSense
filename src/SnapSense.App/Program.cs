using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using AForge.Video;
using AForge.Video.DirectShow;
using FaceAiSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SnapSense;

internal static class Program
{
    private static readonly IFaceDetector _detector = FaceAiSharpBundleFactory.CreateFaceDetector();
    private static readonly IFaceDetectorWithLandmarks _detectorWithLandmarks = FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks();

    private static readonly ManualResetEvent _photoSavedEvent = new(false);

    private static void Main(string[] args)
    {
        Console.WriteLine("Initializing webcam...");

        var videoDevice = TryGetIntegratedWebcam();

        VideoCaptureDevice? videoSource = null;
        try
        {
            videoSource = new VideoCaptureDevice(videoDevice.MonikerString);
            videoSource.NewFrame += video_NewFrame;

            videoSource.Start();

            // Wait for the photo to be saved
            _photoSavedEvent.WaitOne();
        }
        finally
        {
            if (videoSource is not null)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
        }
    }

    private static FilterInfo TryGetIntegratedWebcam()
    {
        var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

        if (videoDevices.Count == 0)
        {
            Console.WriteLine("No video devices found.");
            throw new Exception("No video devices found.");
        }

        FilterInfo? videoDevice = null;
        foreach (FilterInfo device in videoDevices)
        {
            // Attempt to use the Integrated Webcam directly
            if (device.Name.Contains("integrated", StringComparison.InvariantCultureIgnoreCase))
            {
                videoDevice = device;
            }
        }

        if (videoDevice == default)
        {
            videoDevice = videoDevices[0];
        }

        return videoDevice;
    }

    private static void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        var bitmap = eventArgs.Frame;

        var image = BitmapToImageRgb24(bitmap);
        if (DetectFace(image))
        {
            // TODO: Defer this into another thread to free up resources
            Console.WriteLine("Face detected.");

            DrawFaces(image);

            var path = $"photo__{DateTime.Now:yyyyMMdd_HHmmssffff}.jpg";
            image.Save(path);

            Console.WriteLine($"Photo saved at '{path}'.");

            // Signal that the photo has been saved
            _photoSavedEvent.Set();
        }

        Console.WriteLine("Face NOT detected.");
    }

    private static void DrawFaces(Image<Rgb24> image)
    {
        var faces = _detectorWithLandmarks.DetectFaces(image);

        var random = new Random(DateTime.UtcNow.Microsecond);
        var font = SixLabors.Fonts.SystemFonts.CreateFont("Arial", 12);

        foreach (var face in faces)
        {
            var color = GetBrightColor(random);
            var rectangle = new SixLabors.ImageSharp.Drawing.RectangularPolygon((int)face.Box.X, (int)face.Box.Y, (int)face.Box.Width, (int)face.Box.Height);

            image.Mutate(ctx => ctx.Draw(color, 2, rectangle));

            var confidenceText = $"Confidence: {face.Confidence:F2}";
            var location = new Vector2(face.Box.X, face.Box.Y - 20);

            image.Mutate(ctx => ctx.DrawText(confidenceText, font, color, location));
        }
    }

    private static Rgb24 GetBrightColor(Random random)
    {
        // Start with a high base value for brightness
        var baseValue = (byte)random.Next(200, 256);

        // Adjust one or two channels to ensure the color looks like a highlighter
        var red = baseValue;
        var green = (byte)random.Next(150, 256);
        var blue = (byte)random.Next(0, 100);

        // Randomly shuffle the color channels to create different highlighter colors
        var colors = new[] { red, green, blue };
        colors = colors.OrderBy(c => random.Next()).ToArray();

        return new Rgb24(colors[0], colors[1], colors[2]);
    }

    private static bool DetectFace(Image<Rgb24> image)
    {
        // Detect faces
        var faces = _detector.DetectFaces(image);

        // Return true if at least one face is detected
        return faces.Any(result => (result.Confidence ?? 0) > 1.5);
    }

    private static Image<Rgb24> BitmapToImageRgb24(Bitmap bitmap)
    {
        using var ms = new MemoryStream();

        bitmap.Save(ms, ImageFormat.Png);
        ms.Seek(0, SeekOrigin.Begin);
        var image = SixLabors.ImageSharp.Image.Load<Rgb24>(ms);

        return image;
    }
}
