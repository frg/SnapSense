// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SnapSense;

public class FacePersistenceFrameHandlerOptions
{
    public const string ConfigurationSection = "FrameHandlers:FacePersistence";

    public string SavePath { get; set; } = "faces";
    public bool ShouldMarkFaces { get; set; } = true;
}
