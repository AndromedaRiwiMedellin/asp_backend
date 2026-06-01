using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace asp_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerIdToTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("permissions_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("roles_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    payload = table.Column<string>(type: "text", nullable: false),
                    last_activity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sessions_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    google_id = table.Column<string>(type: "text", nullable: true),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    profile_image = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role_id = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("employees_pkey", x => x.id);
                    table.ForeignKey(
                        name: "employees_role_id_fkey",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "employees_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    read = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_pkey", x => x.id);
                    table.ForeignKey(
                        name: "notifications_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pqrs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValueSql: "'OPEN'::character varying"),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pqrs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "pqrs_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "employee_permissions",
                columns: table => new
                {
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("employee_permissions_pkey", x => new { x.employee_id, x.permission_id });
                    table.ForeignKey(
                        name: "employee_permissions_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "employee_permissions_permission_id_fkey",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    poster_url = table.Column<string>(type: "text", nullable: true),
                    event_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    sale_start = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    sale_end = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    total_capacity = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("events_pkey", x => x.id);
                    table.ForeignKey(
                        name: "events_created_by_fkey",
                        column: x => x.created_by,
                        principalTable: "employees",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pqrs_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    pqrs_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    response = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pqrs_responses_pkey", x => x.id);
                    table.ForeignKey(
                        name: "pqrs_responses_employee_id_fkey",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "pqrs_responses_pqrs_id_fkey",
                        column: x => x.pqrs_id,
                        principalTable: "pqrs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "event_area",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("event_area_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_area_event",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    section_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("event_sections_pkey", x => x.id);
                    table.ForeignKey(
                        name: "event_sections_event_id_fkey",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "favorites",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("favorites_pkey", x => new { x.user_id, x.event_id });
                    table.ForeignKey(
                        name: "favorites_event_id_fkey",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "favorites_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    metric_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    metric_value = table.Column<decimal>(type: "numeric", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("metrics_pkey", x => x.id);
                    table.ForeignKey(
                        name: "metrics_created_by_fkey",
                        column: x => x.created_by,
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "metrics_event_id_fkey",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    qr_code = table.Column<string>(type: "text", nullable: false),
                    seat_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValueSql: "'VALID'::character varying"),
                    purchased_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tickets_pkey", x => x.id);
                    table.ForeignKey(
                        name: "tickets_event_id_fkey",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "tickets_seller_id_fkey",
                        column: x => x.seller_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "tickets_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "area_seats",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_area_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: true),
                    seat_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    row_label = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'available'::character varying"),
                    reserved_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    sold_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("area_seats_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_area_seats_area",
                        column: x => x.event_area_id,
                        principalTable: "event_area",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_area_seats_ticket",
                        column: x => x.ticket_id,
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_area_seats_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ticket_scans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scanned_by = table.Column<Guid>(type: "uuid", nullable: true),
                    scanned_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    success = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ticket_scans_pkey", x => x.id);
                    table.ForeignKey(
                        name: "ticket_scans_scanned_by_fkey",
                        column: x => x.scanned_by,
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "ticket_scans_ticket_id_fkey",
                        column: x => x.ticket_id,
                        principalTable: "tickets",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_area_seats_area",
                table: "area_seats",
                column: "event_area_id");

            migrationBuilder.CreateIndex(
                name: "idx_area_seats_status",
                table: "area_seats",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_area_seats_user",
                table: "area_seats",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_area_seats_ticket_id",
                table: "area_seats",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "uq_area_seat_number",
                table: "area_seats",
                columns: new[] { "event_area_id", "seat_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_permissions_permission_id",
                table: "employee_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_role_id",
                table: "employees",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_user_id",
                table: "employees",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_event_area_name",
                table: "event_area",
                columns: new[] { "event_id", "area_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_sections_event_id",
                table: "event_sections",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "idx_events_date",
                table: "events",
                column: "event_date");

            migrationBuilder.CreateIndex(
                name: "IX_events_created_by",
                table: "events",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_favorites_event_id",
                table: "favorites",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_metrics_created_by",
                table: "metrics",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_metrics_event_id",
                table: "metrics",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "permissions_name_key",
                table: "permissions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_pqrs_status",
                table: "pqrs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_pqrs_user_id",
                table: "pqrs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_pqrs_responses_employee_id",
                table: "pqrs_responses",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_pqrs_responses_pqrs_id",
                table: "pqrs_responses",
                column: "pqrs_id");

            migrationBuilder.CreateIndex(
                name: "roles_name_key",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ticket_scans_ticket",
                table: "ticket_scans",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_scans_scanned_by",
                table: "ticket_scans",
                column: "scanned_by");

            migrationBuilder.CreateIndex(
                name: "idx_tickets_qr",
                table: "tickets",
                column: "qr_code");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_event_id",
                table: "tickets",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_seller_id",
                table: "tickets",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_user_id",
                table: "tickets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "tickets_qr_code_key",
                table: "tickets",
                column: "qr_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "area_seats");

            migrationBuilder.DropTable(
                name: "employee_permissions");

            migrationBuilder.DropTable(
                name: "event_sections");

            migrationBuilder.DropTable(
                name: "favorites");

            migrationBuilder.DropTable(
                name: "metrics");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "pqrs_responses");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "ticket_scans");

            migrationBuilder.DropTable(
                name: "event_area");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "pqrs");

            migrationBuilder.DropTable(
                name: "tickets");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
