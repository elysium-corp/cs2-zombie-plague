using Admin.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Database;

public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public const string SchemaName = "admin";

    internal DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();

    internal DbSet<PrivilegeEntity> Privileges => Set<PrivilegeEntity>();

    internal DbSet<PrivilegePermissionEntity> PrivilegePermissions => Set<PrivilegePermissionEntity>();

    internal DbSet<PlayerPrivilegeEntity> PlayerPrivileges => Set<PlayerPrivilegeEntity>();

    private const string PostgreSqlCurrentTimestamp = "CURRENT_TIMESTAMP";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        ConfigurePermissions(modelBuilder);
        ConfigurePrivileges(modelBuilder);
        ConfigurePrivilegePermissions(modelBuilder);
        ConfigurePlayerPrivileges(modelBuilder);
    }

    private static void ConfigurePermissions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PermissionEntity>()
            .ToTable(
                "permissions",
                SchemaName,
                table => table.HasCheckConstraint(
                    "ck_permissions_key_lowercase",
                    "\"key\" = lower(\"key\")"
                )
            );

        modelBuilder.Entity<PermissionEntity>()
            .Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql(PostgreSqlCurrentTimestamp);

        modelBuilder.Entity<PermissionEntity>()
            .Property(x => x.UpdatedAtUtc)
            .HasDefaultValueSql(PostgreSqlCurrentTimestamp);
    }

    private static void ConfigurePrivileges(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrivilegeEntity>()
            .ToTable(
                "privileges",
                SchemaName,
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_privileges_group_lowercase",
                        "\"group_name\" = lower(\"group_name\")"
                    );

                    table.HasCheckConstraint(
                        "ck_privileges_code_lowercase",
                        "\"code\" = lower(\"code\")"
                    );
                }
            );

        modelBuilder.Entity<PrivilegeEntity>()
            .Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql(PostgreSqlCurrentTimestamp);

        modelBuilder.Entity<PrivilegeEntity>()
            .Property(x => x.UpdatedAtUtc)
            .HasDefaultValueSql(PostgreSqlCurrentTimestamp);
    }

    private static void ConfigurePrivilegePermissions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrivilegePermissionEntity>()
            .HasOne(x => x.Privilege)
            .WithMany(x => x.PrivilegePermissions)
            .HasForeignKey(x => x.PrivilegeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PrivilegePermissionEntity>()
            .HasOne(x => x.Permission)
            .WithMany(x => x.PrivilegePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePlayerPrivileges(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerPrivilegeEntity>()
            .Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql(PostgreSqlCurrentTimestamp);

        modelBuilder.Entity<PlayerPrivilegeEntity>()
            .Property(x => x.UpdatedAtUtc)
            .HasDefaultValueSql(PostgreSqlCurrentTimestamp);

        modelBuilder.Entity<PlayerPrivilegeEntity>()
            .HasOne(x => x.Privilege)
            .WithMany(x => x.PlayerPrivileges)
            .HasForeignKey(x => x.PrivilegeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}