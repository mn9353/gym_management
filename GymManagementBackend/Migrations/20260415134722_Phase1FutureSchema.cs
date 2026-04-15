using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagementBackend.Migrations
{
    /// <inheritdoc />
    public partial class Phase1FutureSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_members_gym_id\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"idx_members_gym_id\";");
            migrationBuilder.Sql("ALTER TABLE members ADD COLUMN IF NOT EXISTS amount_to_pay numeric NULL;");
            migrationBuilder.Sql("ALTER TABLE members ADD COLUMN IF NOT EXISTS email character varying(100) NULL;");
            migrationBuilder.Sql("ALTER TABLE members ADD COLUMN IF NOT EXISTS training_type character varying(20) NOT NULL DEFAULT 'GENERAL';");

            migrationBuilder.CreateTable(
                name: "attendance_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    checkin_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    checkin_end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    is_geofence_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    geofence_radius_meters = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    balance_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoices_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "login_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    device_fingerprint = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "member_body_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_date = table.Column<DateOnly>(type: "date", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric", nullable: true),
                    body_fat_percent = table.Column<decimal>(type: "numeric", nullable: true),
                    bmi = table.Column<decimal>(type: "numeric", nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_body_metrics", x => x.id);
                    table.ForeignKey(
                        name: "FK_member_body_metrics_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "member_checkins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkin_date = table.Column<DateOnly>(type: "date", nullable: false),
                    checkin_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_checkins", x => x.id);
                    table.ForeignKey(
                        name: "FK_member_checkins_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    to_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "enquiries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    interested_service_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    next_followup_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    converted_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enquiries", x => x.id);
                    table.ForeignKey(
                        name: "FK_enquiries_service_types_interested_service_type_id",
                        column: x => x.interested_service_type_id,
                        principalTable: "service_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "service_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    duration_months = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    rules_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_plans", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_plans_service_types_service_type_id",
                        column: x => x.service_type_id,
                        principalTable: "service_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "enquiry_followups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enquiry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    followup_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_followup_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enquiry_followups", x => x.id);
                    table.ForeignKey(
                        name: "FK_enquiry_followups_enquiries_enquiry_id",
                        column: x => x.enquiry_id,
                        principalTable: "enquiries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "enquiry_stage_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enquiry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    to_stage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enquiry_stage_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_enquiry_stage_history_enquiries_enquiry_id",
                        column: x => x.enquiry_id,
                        principalTable: "enquiries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoice_line_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric", nullable: false),
                    coverage_start = table.Column<DateOnly>(type: "date", nullable: true),
                    coverage_end = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_line_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoice_line_items_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoice_line_items_service_plans_service_plan_id",
                        column: x => x.service_plan_id,
                        principalTable: "service_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_invoice_line_items_service_types_service_type_id",
                        column: x => x.service_type_id,
                        principalTable: "service_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "member_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount_to_pay = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_member_subscriptions_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_member_subscriptions_service_plans_service_plan_id",
                        column: x => x.service_plan_id,
                        principalTable: "service_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_line_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_allocations", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_allocations_invoice_line_items_invoice_line_item_id",
                        column: x => x.invoice_line_item_id,
                        principalTable: "invoice_line_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payment_allocations_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_allocations_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trainer_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gym_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trainer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainer_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_trainer_assignments_member_subscriptions_member_subscriptio~",
                        column: x => x.member_subscription_id,
                        principalTable: "member_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_trainer_assignments_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trainer_assignments_users_trainer_user_id",
                        column: x => x.trainer_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_members_gym_id_training_type",
                table: "members",
                columns: new[] { "gym_id", "training_type" });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_policies_gym_id_is_active",
                table: "attendance_policies",
                columns: new[] { "gym_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_enquiries_gym_id_phone",
                table: "enquiries",
                columns: new[] { "gym_id", "phone" });

            migrationBuilder.CreateIndex(
                name: "IX_enquiries_gym_id_stage_next_followup_at",
                table: "enquiries",
                columns: new[] { "gym_id", "stage", "next_followup_at" });

            migrationBuilder.CreateIndex(
                name: "IX_enquiries_interested_service_type_id",
                table: "enquiries",
                column: "interested_service_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_enquiry_followups_enquiry_id",
                table: "enquiry_followups",
                column: "enquiry_id");

            migrationBuilder.CreateIndex(
                name: "IX_enquiry_followups_gym_id_enquiry_id_followup_at",
                table: "enquiry_followups",
                columns: new[] { "gym_id", "enquiry_id", "followup_at" });

            migrationBuilder.CreateIndex(
                name: "IX_enquiry_stage_history_enquiry_id",
                table: "enquiry_stage_history",
                column: "enquiry_id");

            migrationBuilder.CreateIndex(
                name: "IX_enquiry_stage_history_gym_id_enquiry_id_changed_at",
                table: "enquiry_stage_history",
                columns: new[] { "gym_id", "enquiry_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_items_gym_id_invoice_id",
                table: "invoice_line_items",
                columns: new[] { "gym_id", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_items_invoice_id",
                table: "invoice_line_items",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_items_service_plan_id",
                table: "invoice_line_items",
                column: "service_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_line_items_service_type_id",
                table: "invoice_line_items",
                column: "service_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_gym_id_invoice_number",
                table: "invoices",
                columns: new[] { "gym_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_gym_id_member_id_invoice_date",
                table: "invoices",
                columns: new[] { "gym_id", "member_id", "invoice_date" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_member_id",
                table: "invoices",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_login_events_email_occurred_at",
                table: "login_events",
                columns: new[] { "email", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_login_events_gym_id_user_id_occurred_at",
                table: "login_events",
                columns: new[] { "gym_id", "user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_member_body_metrics_gym_id_member_id_metric_date",
                table: "member_body_metrics",
                columns: new[] { "gym_id", "member_id", "metric_date" });

            migrationBuilder.CreateIndex(
                name: "IX_member_body_metrics_member_id",
                table: "member_body_metrics",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_member_checkins_gym_id_checkin_date",
                table: "member_checkins",
                columns: new[] { "gym_id", "checkin_date" });

            migrationBuilder.CreateIndex(
                name: "IX_member_checkins_gym_id_member_id_checkin_date",
                table: "member_checkins",
                columns: new[] { "gym_id", "member_id", "checkin_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_checkins_member_id",
                table: "member_checkins",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_member_subscriptions_gym_id_member_id_status",
                table: "member_subscriptions",
                columns: new[] { "gym_id", "member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_member_subscriptions_gym_id_start_date_end_date",
                table: "member_subscriptions",
                columns: new[] { "gym_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_member_subscriptions_member_id",
                table: "member_subscriptions",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_member_subscriptions_service_plan_id",
                table: "member_subscriptions",
                column: "service_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_gym_id_created_at",
                table: "notification_outbox",
                columns: new[] { "gym_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_idempotency_key",
                table: "notification_outbox",
                column: "idempotency_key",
                unique: true,
                filter: "\"idempotency_key\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_status_next_attempt_at",
                table: "notification_outbox",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_gym_id_invoice_id",
                table: "payment_allocations",
                columns: new[] { "gym_id", "invoice_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_gym_id_payment_id",
                table: "payment_allocations",
                columns: new[] { "gym_id", "payment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_invoice_id",
                table: "payment_allocations",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_invoice_line_item_id",
                table: "payment_allocations",
                column: "invoice_line_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_payment_id",
                table: "payment_allocations",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_plans_gym_id_service_type_id_is_active",
                table: "service_plans",
                columns: new[] { "gym_id", "service_type_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_service_plans_service_type_id",
                table: "service_plans",
                column: "service_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_types_gym_id_code",
                table: "service_types",
                columns: new[] { "gym_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_types_gym_id_is_active_sort_order",
                table: "service_types",
                columns: new[] { "gym_id", "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_trainer_assignments_gym_id_member_id_from_date",
                table: "trainer_assignments",
                columns: new[] { "gym_id", "member_id", "from_date" });

            migrationBuilder.CreateIndex(
                name: "IX_trainer_assignments_gym_id_trainer_user_id_from_date",
                table: "trainer_assignments",
                columns: new[] { "gym_id", "trainer_user_id", "from_date" });

            migrationBuilder.CreateIndex(
                name: "IX_trainer_assignments_member_id",
                table: "trainer_assignments",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_trainer_assignments_member_subscription_id",
                table: "trainer_assignments",
                column: "member_subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_trainer_assignments_trainer_user_id",
                table: "trainer_assignments",
                column: "trainer_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_policies");

            migrationBuilder.DropTable(
                name: "enquiry_followups");

            migrationBuilder.DropTable(
                name: "enquiry_stage_history");

            migrationBuilder.DropTable(
                name: "login_events");

            migrationBuilder.DropTable(
                name: "member_body_metrics");

            migrationBuilder.DropTable(
                name: "member_checkins");

            migrationBuilder.DropTable(
                name: "notification_outbox");

            migrationBuilder.DropTable(
                name: "payment_allocations");

            migrationBuilder.DropTable(
                name: "trainer_assignments");

            migrationBuilder.DropTable(
                name: "enquiries");

            migrationBuilder.DropTable(
                name: "invoice_line_items");

            migrationBuilder.DropTable(
                name: "member_subscriptions");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "service_plans");

            migrationBuilder.DropTable(
                name: "service_types");

            migrationBuilder.DropIndex(
                name: "IX_members_gym_id_training_type",
                table: "members");

            migrationBuilder.DropColumn(
                name: "training_type",
                table: "members");

            migrationBuilder.CreateIndex(
                name: "IX_members_gym_id",
                table: "members",
                column: "gym_id");
        }
    }
}
