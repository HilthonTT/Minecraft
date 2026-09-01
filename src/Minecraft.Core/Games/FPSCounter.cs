namespace Minecraft.Core.Games;

public sealed class FPSCounter
{
    private long _totalElapsedFrames;
    private double _totalElapsedTimeInSeconds;

    public void IncrementFrameCounter()
    {
        _totalElapsedFrames++;
    }

    public void AddElapsedTime(double seconds)
    {
        _totalElapsedTimeInSeconds += seconds;
    }

    public int GetAverageFPS()
    {
        if (_totalElapsedTimeInSeconds <= 0)
        {
            return 0;
        }

        return (int)(_totalElapsedFrames / _totalElapsedTimeInSeconds);
    }
}
