// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SnapSense;

public class WebcamHostedService : IHostedService
{
    private readonly ILogger<WebcamHandler> _logger;
    private readonly WebcamHandler _webcamHandler;

    public WebcamHostedService(ILogger<WebcamHandler> logger, WebcamHandler webcamHandler)
    {
        _logger = logger;
        _webcamHandler = webcamHandler;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Started looking for faces...");

        _webcamHandler.Start();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
