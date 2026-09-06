using Microsoft.Extensions.Logging;
using Npgsql;

namespace ZombiePlague.Core.Catalog;

internal sealed class ZombieCatalogNotInitializedException()
    : InvalidOperationException("В БД нет опубликованного каталога классов");

internal static class ZombieCatalogDiagnostics
{
    public const string InitializationHelp = "Проверьте права подключения на создание схемы и таблиц или откройте Elysium → Классы в веб-админке, выберите тот же игровой сервер "
        + "и импортируйте zombie_catalog.json либо пару zombie_class.json + ability.json; для нового сервера нажмите «Создать базовый каталог»";

    public static bool NeedsInitialization(Exception? error) => error is ZombieCatalogNotInitializedException
        or PostgresException { SqlState: PostgresErrorCodes.UndefinedTable or PostgresErrorCodes.InvalidSchemaName };

    public static void LogDatabaseFailure(ILogger logger, Exception error, string source)
    {
        if (NeedsInitialization(error))
        {
            // Ответ PostgreSQL об отсутствующих таблицах не означает обрыв соединения
            logger.LogWarning("[ZombieCatalog] Каталог в БД не подготовлен — источник {Source}; {Help}; "
                + "Повтор при старте карты или zp_classes_reload", source, InitializationHelp);
            return;
        }

        logger.LogWarning(error, "[ZombieCatalog] Не удалось загрузить каталог из БД — источник {Source}, "
            + "повтор при старте карты или zp_classes_reload", source);
    }
}
