// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Emgu.CV;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

    public void HandleFrame(Mat frameMat)
    {
        var faces = _faceDetector.LocateFaces(frameMat);

        if (faces.Length == 0)
        {
            _logger.LogDebug("No face detected.");
            return;
        }

        _logger.LogInformation("Found {FacesCount} face(s).", faces.Length);
        _logger.LogInformation("Marking face(s).");

        var markedMat = _faceDetector.MarkFaces(frameMat, faces);

        // TODO: Make path configurable
        var path = $"photo__{DateTime.Now:yyyyMMdd_HHmmssffff}.jpg";
        markedMat.Save(path);

        _logger.LogInformation("Photo saved at {Path}.", path);

        _hostApplicationLifetime.StopApplication();
    }
}
