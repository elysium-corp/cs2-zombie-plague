using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Admin.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminKeyNormalizationConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_privileges_code_lowercase",
                schema: "admin",
                table: "privileges",
                sql: "\"code\" = lower(\"code\")");

            migrationBuilder.AddCheckConstraint(
                name: "ck_privileges_group_lowercase",
                schema: "admin",
                table: "privileges",
                sql: "\"group_name\" = lower(\"group_name\")");

            migrationBuilder.AddCheckConstraint(
                name: "ck_permissions_key_lowercase",
                schema: "admin",
                table: "permissions",
                sql: "\"key\" = lower(\"key\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_privileges_code_lowercase",
                schema: "admin",
                table: "privileges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_privileges_group_lowercase",
                schema: "admin",
                table: "privileges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_permissions_key_lowercase",
                schema: "admin",
                table: "permissions");
        }
    }
}
