using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Advertisement.Core.Database.Migrations;

[DbContext(typeof(AdvertisementDbContext))]
[Migration("20260827130000_AddAdvertisementDeliveryRules")]
internal sealed class AddAdvertisementDeliveryRules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE advertisement.messages
                ADD COLUMN dispatch_mode VARCHAR(16) NOT NULL DEFAULT 'periodic',
                ADD COLUMN daily_times JSONB NOT NULL DEFAULT '[]'::jsonb,
                ADD COLUMN daily_start_time TIME WITHOUT TIME ZONE NULL,
                ADD COLUMN daily_end_time TIME WITHOUT TIME ZONE NULL,
                ADD COLUMN audience_type VARCHAR(16) NOT NULL DEFAULT 'all',
                ADD COLUMN audience_group VARCHAR(64) NULL;

            ALTER TABLE advertisement.messages
                ADD CONSTRAINT messages_dispatch_mode_valid
                    CHECK (dispatch_mode IN ('periodic', 'daily', 'manual')),
                ADD CONSTRAINT messages_daily_times_array
                    CHECK (jsonb_typeof(daily_times) = 'array'),
                ADD CONSTRAINT messages_audience_valid
                    CHECK (
                        (audience_type = 'all' AND audience_group IS NULL)
                        OR
                        (
                            audience_type = 'admin_group'
                            AND audience_group IS NOT NULL
                            AND btrim(audience_group) <> ''
                        )
                    );

            CREATE INDEX messages_dispatch_idx
                ON advertisement.messages (enabled, dispatch_mode);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS advertisement.messages_dispatch_idx;

            ALTER TABLE advertisement.messages
                DROP CONSTRAINT IF EXISTS messages_audience_valid,
                DROP CONSTRAINT IF EXISTS messages_daily_times_array,
                DROP CONSTRAINT IF EXISTS messages_dispatch_mode_valid,
                DROP COLUMN IF EXISTS audience_group,
                DROP COLUMN IF EXISTS audience_type,
                DROP COLUMN IF EXISTS daily_end_time,
                DROP COLUMN IF EXISTS daily_start_time,
                DROP COLUMN IF EXISTS daily_times,
                DROP COLUMN IF EXISTS dispatch_mode;
            """);
    }
}
