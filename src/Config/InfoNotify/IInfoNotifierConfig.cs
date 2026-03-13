namespace CS2ZombiePlague.Config.InfoNotify;

public interface IInfoNotifierConfig
{
    public bool Enable { get; set; }
    public List<string> RoundEndMessages { get; set; }
    public List<string> RoundStartMessages { get; set; }
    public List<string> RoundEventMessages  { get; set; }
    public List<string> PlayerConnectMessages  { get; set; }
    public float TimeBetweenEventMessagesPerSeconds {get; set;}
    public float DelayBeforeFirstEventMessagesPerSeconds {get; set;}
    public bool RandomEventMessagesEnable { get; set; }
    public short CountRandomEventMessages { get; set; }
}