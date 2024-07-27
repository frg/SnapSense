// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Emgu.CV;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SnapSense;

public class FacePersistenceFrameHandler : IFrameHandler
{
    private readonly ILogger<FacePersistenceFrameHandler> _logger;
    private readonly FaceDetector _faceDetector;
    private readonly FacePersistencePersistenceHandler _persistenceHandler;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly FacePersistenceFrameHandlerOptions _options;

    public FacePersistenceFrameHandler(
        ILogger<FacePersistenceFrameHandler> logger,
        IOptions<FacePersistenceFrameHandlerOptions> options,
        FaceDetector faceDetector,
        FacePersistencePersistenceHandler persistenceHandler,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _options = options.Value;
        _faceDetector = faceDetector;
        _persistenceHandler = persistenceHandler;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public void HandleFrame(Mat frameMat)
    {
        // TODO: Stop using HostApplicationLifetime
        if (_hostApplicationLifetime.ApplicationStopping.IsCancellationRequested)
        {
            return;
        }

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

            if (_options.ShouldMarkFaces)
            {
                using (var markedFrameMat = _faceDetector.MarkFaces(frameMat, faces))
                {
                    _persistenceHandler.Save(markedFrameMat, _options.SavePath);
                }
            }
            else
            {
                _persistenceHandler.Save(frameMat, _options.SavePath);
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
