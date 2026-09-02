namespace CustomKnife.Data.Knives;

/// <summary>
/// Предоставляет явные ключи Localization.Core для отображаемых полей ножа.
/// </summary>
internal interface ILocalizedKnife
{
    string DisplayNameKey { get; }

    string DescriptionKey { get; }
}
