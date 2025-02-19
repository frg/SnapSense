// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.Extensions.Logging;
using SnapSense.FrameHandlers;

namespace SnapSense;

public class WebcamHandler : IDisposable
{
    private readonly ILogger<WebcamHandler> _logger;
    private readonly IEnumerable<IFrameHandler> _frameHandlers;

    private readonly VideoCapture _capture;
    private bool _isCapturing;

    public WebcamHandler(ILogger<WebcamHandler> logger, IEnumerable<IFrameHandler> frameHandlers)
    {
        _logger = logger;
        _frameHandlers = frameHandlers;

        // Initialize the capture device (0 is typically the integrated webcam)
        _capture = new VideoCapture(0);
        if (!_capture.IsOpened)
        {
            _logger.LogError("No video devices found.");

            // TODO: Handle error gracefully
            throw new Exception("No video devices found.");
        }

        // Set desired frame width, height, and frame rate
        _capture.Set(CapProp.FrameWidth, 1280);
        _capture.Set(CapProp.FrameHeight, 720);
        _capture.Set(CapProp.Autofocus, 39);
        _capture.Set(CapProp.Fps, 1);

        _capture.ImageGrabbed += HandleOnImageGrabbed;
    }

    private void HandleOnImageGrabbed(object? sender, EventArgs e)
    {
        using var frameMat = new Mat();
        var capture = (VideoCapture)sender!;
        capture.Retrieve(frameMat);

        // Process the frame using the registered handlers
        Parallel.ForEach(_frameHandlers, handler => { handler.HandleFrame(frameMat); });
    }

    public void Start()
    {
        if (_isCapturing)
        {
            _logger.LogDebug("Attempted to start capturing while already capturing.");
            return;
        }

        _isCapturing = true;
        _capture.Start();
    }

    public void StopCapture()
    {
        if (!_isCapturing)
        {
            _logger.LogDebug("Attempted to stop capturing but wasn't capturing.");
            return;
        }

        _isCapturing = false;
        _capture.Stop();
    }

    public void Dispose()
    {
        StopCapture();
        _capture.ImageGrabbed -= HandleOnImageGrabbed;
        _capture.Dispose();
    }
}
