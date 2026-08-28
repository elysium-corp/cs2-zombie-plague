namespace Common.Hooks;

/// <summary>Приоритет синхронного обработчика.</summary>
public enum HookPriority
{
    /// <summary>Вызывается после обработчиков обычного и высокого приоритета.</summary>
    Low,

    /// <summary>Приоритет по умолчанию.</summary>
    Normal,

    /// <summary>Вызывается раньше обработчиков обычного и низкого приоритета.</summary>
    High,
}
