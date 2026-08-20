namespace Admin.Core.Data;

/// <summary>
/// Описывает данные, необходимые для регистрации новой привилегии.
/// </summary>
/// <param name="Id">
/// Идентификатор привилегии внутри группы, например <c>owner</c> или <c>staff</c>.
/// </param>
/// <param name="Group">
/// Логическая группа привилегии, например <c>admin</c> или <c>vip</c>.
/// </param>
/// <param name="Permissions">
/// Набор разрешений, предоставляемых привилегией.
/// </param>
/// <remarks>
/// Полный ключ привилегии формируется как <c>{Group}.{Id}</c>.
///
/// Этот тип описывает саму привилегию, а не её назначение конкретному игроку.
/// </remarks>
public sealed record PrivilegeDefinition(
    string Id,
    string Group,
    IReadOnlySet<string> Permissions
);