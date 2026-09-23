namespace MapRotation.Core;

internal static class RotationText
{
    private static readonly Dictionary<string, (string Ru, string En)> Entries = new()
    {
        ["Loading"] = ("Ротация карт загружается", "Map rotation is loading"),
        ["TimeLeft"] = ("До смены карты", "Time left"),
        ["NextMap"] = ("Следующая карта", "Next map"),
        ["LastRound"] = ("Последний раунд", "Final round"),
        ["NotSelected"] = ("Пока не выбрана", "Not selected yet"),
        ["RtvDelay"] = ("RTV доступен через", "RTV available in"),
        ["RtvRemaining"] = ("Осталось голосов: {count}", "{count} more votes needed"),
        ["Accepted"] = ("Сохранено", "Saved"),
        ["Duplicate"] = ("Ваш голос уже учтён", "Your vote has already been counted"),
        ["Disabled"] = ("Функция отключена", "Feature disabled"),
        ["TooFewPlayers"] = ("Недостаточно игроков для RTV", "Not enough players for RTV"),
        ["NotEligible"] = ("Вы не можете участвовать", "You cannot participate"),
        ["Locked"] = ("Следующая карта уже выбирается или выбрана", "The next map is being selected or has been selected"),
        ["InvalidMap"] = ("Карта недоступна", "Map unavailable"),
        ["NominationTitle"] = ("Номинация карты", "Nominate a map"),
        ["NominationSubtitle"] = ("Выберите карту для следующего голосования", "Choose a map for the next vote"),
        ["VoteTitle"] = ("Выберите следующую карту", "Choose the next map"),
        ["VoteSubtitle"] = ("Голосование за следующую карту", "Vote for the next map"),
        ["YourVote"] = ("Ваш голос", "Your vote"),
        ["Votes"] = ("Голосов: {count}", "Votes: {count}"),
        ["Close"] = ("Закрыть", "Close"),
        ["NoMaps"] = ("Нет доступных карт", "No maps available"),
        ["ResultTitle"] = ("Голосование завершено", "Voting complete"),
        ["LastRoundDescription"] = ("Текущий раунд — последний\nКарта сменится после завершения раунда", "This is the final round\nThe map changes when the round ends"),
        ["ScheduledResult"] = ("Карта сменится после окончания времени и раунда", "The map changes after the time limit and the final round"),
        ["RtvAdded"] = ("Игрок {player} поддержал RTV ({votes} / {required})", "{player} requested RTV ({votes} / {required})"),
        ["VoteStarted"] = ("Открыто голосование за следующую карту", "Voting for the next map has started"),
        ["ForcedChange"] = ("Последний раунд затянулся — выполняется смена карты", "Final round timeout — changing map"),
        ["HudUnavailable"] = ("Интерфейс меню временно недоступен", "Menu interface is temporarily unavailable"),
    };
    public static string Fallback(string key, string language, params (string Name, string Value)[] values)
    {
        if (!Entries.TryGetValue(key, out var entry)) return key;
        var text = language == "ru" ? entry.Ru : entry.En;
        foreach (var (name, value) in values) text = text.Replace("{" + name + "}", value, StringComparison.Ordinal);
        return text;
    }
}
