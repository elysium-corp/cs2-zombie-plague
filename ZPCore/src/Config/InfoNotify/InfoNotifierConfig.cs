namespace ZPCore.Config.InfoNotify;

public sealed class InfoNotifierConfig : IInfoNotifierConfig
{
    // Включить вывод информации в чат.
    public bool Enable { get; set; }
    // Список сообщений, который будет выведен в конце раунда.
    public List<string> RoundEndMessages { get; set; } = [""];
    // Список сообщений, который будет выведен в начале раунда.
    public List<string> RoundStartMessages { get; set; } = [""];
    // Список сообщений, который будет выводиться периодически, каждые n секунд.
    public List<string> RoundEventMessages { get; set; } = [""];
    // Список сообщений, который будет выведен, когда игрок подключится к серверу.
    public List<string> PlayerConnectMessages { get; set; } = [""];
    // Время между RoundEventMessages.
    public float TimeBetweenEventMessagesPerSeconds { get; set; } = 60.0f;
    // Время перед первым RoundEventMessages.
    public float DelayBeforeFirstEventMessagesPerSeconds { get; set; } = 60.0f;
    // Если включен, то будут выводиться случайные сообщения из RoundEventMessages.
    public bool RandomEventMessagesEnable { get; set; } = false;
    // Количество случайных сообщений, которые будут выведены.
    public short CountRandomEventMessages { get; set; } = 2;
}