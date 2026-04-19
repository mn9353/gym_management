using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagementBackend.Migrations
{
    /// <inheritdoc />
    public partial class FixUsersRoleCheckForTrainer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE users DROP CONSTRAINT IF EXISTS users_role_check;
                ALTER TABLE users
                    ADD CONSTRAINT users_role_check
                    CHECK (UPPER(role) IN ('ADMIN', 'OWNER', 'STAFF', 'TRAINER', 'MEMBER'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE users DROP CONSTRAINT IF EXISTS users_role_check;
                ALTER TABLE users
                    ADD CONSTRAINT users_role_check
                    CHECK (UPPER(role) IN ('ADMIN', 'OWNER', 'STAFF'));
                """);
        }
    }
}

