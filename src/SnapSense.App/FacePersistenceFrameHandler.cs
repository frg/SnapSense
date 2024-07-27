// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SnapSense;

public class FacePersistenceFrameHandler : IFrameHandler
{
    private readonly ILogger<FacePersistenceFrameHandler> _logger;
    private readonly FaceDetector _faceDetector;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public FacePersistenceFrameHandler(ILogger<FacePersistenceFrameHandler> logger, FaceDetector faceDetector, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _faceDetector = faceDetector;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public void HandleFrame(Bitmap bitmap)
    {
        using (var image = bitmap.ConvertTo<Rgb24>())
        {
            if (_faceDetector.HasAnyFace(image))
            {
                // TODO: Defer this into another thread to free up resources
                using (var markedFacesImage = _faceDetector.MarkFaces(image))
                {
                    // TODO: Make path configurable
                    var path = $"photo__{DateTime.Now:yyyyMMdd_HHmmssffff}.jpg";
                    markedFacesImage.Save(path);

                    _logger.LogInformation("Photo saved at {Path}.", path);
                }

                _hostApplicationLifetime.StopApplication();
            }

            _logger.LogDebug("Face not detected which meets criteria.");
        }
    }
}
