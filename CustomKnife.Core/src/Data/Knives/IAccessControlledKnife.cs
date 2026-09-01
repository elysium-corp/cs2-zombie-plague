namespace CustomKnife.Data.Knives;

/// <summary>
/// Описывает нож, доступ к которому может ограничиваться разрешением Admin.Core.
/// </summary>
internal interface IAccessControlledKnife
{
    /// <summary>
    /// Возвращает ключ требуемого разрешения либо <see langword="null"/> для общего доступа.
    /// </summary>
    string? RequiredPermission { get; }
}
