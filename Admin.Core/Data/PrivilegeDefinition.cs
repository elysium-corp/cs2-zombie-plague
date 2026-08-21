namespace Admin.Core.Data;

/// <summary>
/// Представляет определение привилегии,
/// загруженное из persistent-хранилища.
/// </summary>
/// <param name="Id">
/// Идентификатор привилегии внутри группы.
/// </param>
/// <param name="Group">
/// Логическая группа привилегии.
/// </param>
/// <param name="Permissions">
/// Набор разрешений, предоставляемых привилегией.
/// </param>
/// <remarks>
/// Полный runtime-ключ формируется как <c>{Group}.{Id}</c>.
/// </remarks>
internal sealed record PrivilegeDefinition(
    string Id,
    string Group,
    IReadOnlySet<string> Permissions
);