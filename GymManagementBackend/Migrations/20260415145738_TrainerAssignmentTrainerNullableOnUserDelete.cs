using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagementBackend.Migrations
{
    /// <inheritdoc />
    public partial class TrainerAssignmentTrainerNullableOnUserDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_trainer_assignments_users_trainer_user_id",
                table: "trainer_assignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "trainer_user_id",
                table: "trainer_assignments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_trainer_assignments_users_trainer_user_id",
                table: "trainer_assignments",
                column: "trainer_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_trainer_assignments_users_trainer_user_id",
                table: "trainer_assignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "trainer_user_id",
                table: "trainer_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_trainer_assignments_users_trainer_user_id",
                table: "trainer_assignments",
                column: "trainer_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
