using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagementBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionMonthServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscription_month_services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    month_index = table.Column<int>(type: "integer", nullable: false),
                    trainer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_to_pay = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_month_services", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscription_month_services_member_subscriptions_member_sub~",
                        column: x => x.member_subscription_id,
                        principalTable: "member_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_subscription_month_services_service_types_service_type_id",
                        column: x => x.service_type_id,
                        principalTable: "service_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_month_services_gym_id_member_subscription_id_m~",
                table: "subscription_month_services",
                columns: new[] { "gym_id", "member_subscription_id", "month_index" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_month_services_gym_id_service_type_id_month_in~",
                table: "subscription_month_services",
                columns: new[] { "gym_id", "service_type_id", "month_index" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_month_services_member_subscription_id_month_in~",
                table: "subscription_month_services",
                columns: new[] { "member_subscription_id", "month_index", "service_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_month_services_service_type_id",
                table: "subscription_month_services",
                column: "service_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscription_month_services");
        }
    }
}
