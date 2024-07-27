// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Emgu.CV;

namespace SnapSense.FrameHandlers;

public interface IFrameHandler
{
    public void HandleFrame(Mat frameMat);
}
