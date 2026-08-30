using Microsoft.EntityFrameworkCore;
using Economy.Core.Database.Entities;

namespace Economy.Core.Database;

public sealed class EconomyDbContext(DbContextOptions<EconomyDbContext> options) : DbContext(options)
{
    public const string SchemaName = "economy";

    internal DbSet<AccountEntity> Accounts => Set<AccountEntity>();

    internal DbSet<EconomySettingsEntity> Settings => Set<EconomySettingsEntity>();

    internal DbSet<EconomyRoleRuleEntity> RoleRules => Set<EconomyRoleRuleEntity>();

    internal DbSet<EconomyWeaponRuleEntity> WeaponRules => Set<EconomyWeaponRuleEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<EconomySettingsEntity>()
            .Property(entity => entity.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<EconomySettingsEntity>()
            .HasCheckConstraint("ck_economy_settings_singleton", "id = 1")
            .HasCheckConstraint("ck_economy_settings_limits", "absolute_max_money >= 0 AND default_max_money BETWEEN 0 AND absolute_max_money AND start_money BETWEEN 0 AND default_max_money")
            .HasCheckConstraint("ck_economy_settings_rewards", "money_per_damage >= 0 AND money_for_infection >= 0 AND money_for_zombie_kill >= 0 AND money_for_human_kill >= 0")
            .HasCheckConstraint("ck_economy_settings_intervals", "periodic_save_interval_seconds BETWEEN 10 AND 86400 AND settings_refresh_interval_seconds BETWEEN 10 AND 3600");

        modelBuilder.Entity<EconomyRoleRuleEntity>()
            .HasCheckConstraint("ck_economy_role_rules_values", "max_money >= 0 AND reward_bonus_percent BETWEEN 0 AND 10000");

        modelBuilder.Entity<EconomyWeaponRuleEntity>()
            .HasCheckConstraint("ck_economy_weapon_reward_rules_values", "damage_bonus_percent BETWEEN 0 AND 10000");
    }
}
