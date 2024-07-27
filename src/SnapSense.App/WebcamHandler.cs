// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using AForge.Video;
using AForge.Video.DirectShow;

namespace SnapSense;

public class WebcamHandler : IDisposable
{
    private readonly IEnumerable<IFrameHandler> _frameHandlers;
    private readonly VideoCaptureDevice _videoSource;

    public WebcamHandler(IEnumerable<IFrameHandler> frameHandlers)
    {
        _frameHandlers = frameHandlers;

        var videoDevice = TryGetIntegratedWebcam();
        _videoSource = new VideoCaptureDevice(videoDevice.MonikerString);
        _videoSource.NewFrame += OnNewFrame;
    }

    public void Start(CancellationToken cancellationToken)
    {
        _videoSource.Start();

        cancellationToken.WaitHandle.WaitOne();
    }

    private void OnNewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        var bitmap = (Bitmap)eventArgs.Frame.Clone();
        Parallel.ForEach(_frameHandlers, handler =>
        {
            handler.HandleFrame(bitmap);
        });
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

    public void Dispose()
    {
        if (_videoSource is not null)
        {
            _videoSource.SignalToStop();
            _videoSource.WaitForStop();
        }
    }
}
