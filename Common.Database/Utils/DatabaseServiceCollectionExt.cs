using Common.Database.Abstractions;
using Common.Database.Migrator;
using Common.Database.Providers;
using Common.Database.Storage;
using Common.Database.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SwiftlyS2.Shared;

namespace Common.Database.Utils;

public static class DatabaseServiceCollectionExt
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPostgreSqlDatabase<TContext>(
            ISwiftlyCore core,
            DatabaseOptions databaseOptions
        ) where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(core);
            ArgumentNullException.ThrowIfNull(databaseOptions);
            
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseOptions.ConnectionName);
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseOptions.Schema);
            
            var connectionProvider = new DatabaseConnectionProvider(core);
            var connectionString = connectionProvider.GetPostgreSqlConnectionString(databaseOptions.ConnectionName);
            
            services.AddDbContextFactory<TContext>(options =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsql =>
                    {
                        npgsql.CommandTimeout(databaseOptions.CommandTimeoutSeconds);

                        npgsql.MigrationsHistoryTable(
                            databaseOptions.MigrationsHistoryTable,
                            databaseOptions.Schema
                        );

                        if (databaseOptions.RetryCount > 0)
                        {
                            npgsql.EnableRetryOnFailure(
                                databaseOptions.RetryCount,
                                databaseOptions.MaxRetryDelay,
                                null
                            );
                        }
                    }
                );

                if (databaseOptions.IgnoreExecutedCommandLogs)
                {
                    options.ConfigureWarnings(warnings =>
                    {
                        warnings.Ignore(RelationalEventId.CommandExecuted);
                    });
                }
            });
            
            services.TryAddSingleton<DatabaseTaskTracker>();
            
            services.AddSingleton<DatabaseMigrator<TContext>>();

            return services;
        }
        
        public IServiceCollection AddSteamEntityStore<TContext, TEntity>() where TContext : DbContext where TEntity : class, ISteamEntity, new()
        {
            services.AddSingleton<ISteamEntityStore<TEntity>, EfSteamEntityStore<TContext, TEntity>>();

            return services;
        }
    }
}