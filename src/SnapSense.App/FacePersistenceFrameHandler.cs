// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SnapSense;

public class FacePersistenceFrameHandler : IFrameHandler
{
    private readonly FaceDetector _faceDetector;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public FacePersistenceFrameHandler(FaceDetector faceDetector, CancellationTokenSource cancellationTokenSource)
    {
        _faceDetector = faceDetector;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public void HandleFrame(Bitmap bitmap)
    {
        using (var image = bitmap.ConvertTo<Rgb24>())
        {
            if (_faceDetector.HasAnyFace(image))
            {
                // TODO: Defer this into another thread to free up resources
                Console.WriteLine("Face detected.");

                using (var markedFacesImage = _faceDetector.MarkFaces(image))
                {
                    // TODO: Make path configurable
                    var path = $"photo__{DateTime.Now:yyyyMMdd_HHmmssffff}.jpg";
                    markedFacesImage.Save(path);

                    Console.WriteLine($"Photo saved at '{path}'.");
                }

                _cancellationTokenSource.Cancel();
            }

            Console.WriteLine("Face NOT detected.");
        }
    }
}
