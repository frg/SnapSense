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

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public FacePersistenceFrameHandler(ILogger<FacePersistenceFrameHandler> logger, FaceDetector faceDetector, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _faceDetector = faceDetector;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public void HandleFrame(Mat frameMat)
    {
        // Try to enter the lock. If it's already taken, return immediately.
        if (!_semaphore.Wait(0))
        {
            _logger.LogDebug("Frame handling is already in progress. Skipping this frame.");
            return;
        }

        try
        {
            var faces = _faceDetector.LocateFaces(frameMat).ToList();

            if (faces.Count == 0)
            {
                _logger.LogDebug("No face detected.");
                return;
            }

            _logger.LogInformation("Found {FacesCount} face(s).", faces.Count);
            // TODO: Defer this to free up handler
            _logger.LogInformation("Marking face(s).");

            using (var markedMat = _faceDetector.MarkFaces(frameMat, faces))
            {
                // TODO: Make path configurable
                var path = $"photo__{DateTime.Now:yyyyMMdd_HHmmssffff}.jpg";
                markedMat.Save(path);

                _logger.LogInformation("Photo saved at {Path}.", path);
            }

            _hostApplicationLifetime.StopApplication();
        }
        finally
        {
            // Release the lock so that the next frame can be handled.
            _semaphore.Release();
        }
    }
}
