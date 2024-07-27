namespace SnapSense;

internal static class Program
{
    private static void Main(string[] args)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        using (var webcam = new WebcamHandler(new []{ new FacePersistenceFrameHandler(new FaceDetector(), cancellationTokenSource) }))
        {
            webcam.Start(cancellationToken);
        }
    }
}
