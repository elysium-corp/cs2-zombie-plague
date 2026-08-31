namespace InfoNotify.Core.Data.Configs;

public sealed class InfoNotifyConfig
{
    // Включить вывод информации в чат.
    public bool Enable { get; set; }
    // Ключи ElysiumLocalization, выводимые в конце раунда.
    public List<string> RoundEndMessages { get; set; } = [];
    // Ключи ElysiumLocalization, выводимые в начале раунда.
    public List<string> RoundStartMessages { get; set; } = [];
    // Ключи ElysiumLocalization, выводимые периодически, каждые n секунд.
    public List<string> RoundEventMessages { get; set; } = [];
    // Ключи ElysiumLocalization, выводимые при подключении игрока.
    public List<string> PlayerConnectMessages { get; set; } = [];
    // Время между RoundEventMessages.
    public float TimeBetweenEventMessagesPerSeconds { get; set; } = 60.0f;
    // Время перед первым RoundEventMessages.
    public float DelayBeforeFirstEventMessagesPerSeconds { get; set; } = 60.0f;
    // Если включен, то будут выводиться случайные сообщения из RoundEventMessages.
    public bool RandomEventMessagesEnable { get; set; } = false;
    // Количество случайных сообщений, которые будут выведены.
    public short CountRandomEventMessages { get; set; } = 2;
}
