using Godot;

public partial class TimeManager : Node
{

	public static TimeManager Instance { get; private set; }
    private long currentTime = 0;
    private long startMsecs = 0;

    private long finalTime = 0;
    private bool started = false;

    private long pausedCurrentTime = 0;
    private long pausedStartTime = 0;
    private long pausedTotalTime = 0;
    private long pausedPrevTime = 0;
    private bool paused = false;

    public override void _Ready()
    {
		Instance = this; 
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Process(double delta)
    {
        if (started)
            currentTime = (long)Time.GetTicksMsec();

        if (GetTree().Paused && !paused)
        {
            pausedStartTime = (long)Time.GetTicksMsec();
            paused = true;
        }

        if (GetTree().Paused && paused)
        {
            pausedCurrentTime = (long)Time.GetTicksMsec();
            pausedTotalTime = pausedPrevTime + (pausedCurrentTime - pausedStartTime);
        }

        if (!GetTree().Paused && paused)
        {
            pausedPrevTime = pausedTotalTime;
            paused = false;
        }
    }

    public void StartStopwatch()
    {
        pausedTotalTime = 0;
        startMsecs = (long)Time.GetTicksMsec();
        started = true;
    }

    public void StopStopwatch()
    {
        started = false;
    }

    public long GetMsec()
    {
        return (currentTime - startMsecs) - pausedTotalTime;
    }

    public long GetSecs()
    {
        return GetMsec() / 1000;
    }

    public long GetMinutes()
    {
        return GetSecs() / 60;
    }

    public long GetHours()
    {
        return GetSecs() / 3600; // fixed: should be /3600, not /60
    }

    public string GetTimeString()
    {
        string msecString = (GetMsec() % 1000).ToString();
        string secString = (GetSecs() % 60).ToString();
        string minString = (GetMinutes() % 60).ToString();

        if (msecString.Length >= 3)
            msecString = msecString.Remove(2, 1);

        if (secString.Length <= 1)
            secString = secString.PadLeft(2, '0');

        if (minString.Length <= 1)
            minString = minString.PadLeft(2, '0');

        if (msecString.Length <= 1)
            msecString = msecString.PadLeft(2, '0');

        return $"{minString}:{secString}:{msecString}";
    }
}
