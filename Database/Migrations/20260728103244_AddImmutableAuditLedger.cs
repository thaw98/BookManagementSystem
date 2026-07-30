using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class AddImmutableAuditLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EntityType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<long>(type: "bigint", nullable: false),
                    EntityDisplayName = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: true),
                    ActorDisplayName = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActorEmail = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ChangeDetailsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsBackfilled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_Action",
                table: "AuditLogs",
                columns: new[] { "EntityType", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAtUtc_Id",
                table: "AuditLogs",
                columns: new[] { "OccurredAtUtc", "Id" });

            Backfill(migrationBuilder, "User", "Account",
                "COALESCE(NULLIF(source.`FullName`, ''), NULLIF(source.`Email`, ''), CONCAT('Account #', source.`Id`))");
            Backfill(migrationBuilder, "Role", "Role",
                "COALESCE(NULLIF(source.`Name`, ''), CONCAT('Role #', source.`Id`))");
            Backfill(migrationBuilder, "Authors", "Author",
                "COALESCE(NULLIF(source.`Name`, ''), CONCAT('Author #', source.`Id`))");
            Backfill(migrationBuilder, "Categories", "Category",
                "COALESCE(NULLIF(source.`Name`, ''), CONCAT('Category #', source.`Id`))");
            Backfill(migrationBuilder, "Books", "Book",
                "COALESCE(NULLIF(source.`Title`, ''), CONCAT('Book #', source.`Id`))");
            Backfill(migrationBuilder, "BookBorrowRecords", "Borrow Record",
                "CONCAT('Borrow record #', source.`Id`)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");
        }

        private static void Backfill(
            MigrationBuilder migrationBuilder,
            string table,
            string entityType,
            string displayExpression)
        {
            InsertBackfill(
                migrationBuilder, table, entityType, displayExpression,
                "Created", "CreatedBy", "CreatedAt", "1 = 1");
            InsertBackfill(
                migrationBuilder, table, entityType, displayExpression,
                "Updated", "UpdatedBy", "UpdatedAt",
                "source.`UpdatedAt` > source.`CreatedAt`");
            InsertBackfill(
                migrationBuilder, table, entityType, displayExpression,
                "Deleted", "DeletedBy", "DeletedAt",
                "source.`DeletedAt` IS NOT NULL");
        }

        private static void InsertBackfill(
            MigrationBuilder migrationBuilder,
            string table,
            string entityType,
            string displayExpression,
            string action,
            string actorColumn,
            string timestampColumn,
            string predicate)
        {
            migrationBuilder.Sql($"""
                INSERT INTO `AuditLogs`
                    (`EntityType`, `EntityId`, `EntityDisplayName`, `Action`,
                     `ActorUserId`, `ActorDisplayName`, `ActorEmail`,
                     `OccurredAtUtc`, `ChangeDetailsJson`, `IsBackfilled`)
                SELECT
                    '{entityType}',
                    source.`Id`,
                    {displayExpression},
                    '{action}',
                    source.`{actorColumn}`,
                    COALESCE(NULLIF(actor.`FullName`, ''), NULLIF(actor.`Email`, ''), 'System'),
                    actor.`Email`,
                    source.`{timestampColumn}`,
                    JSON_OBJECT(),
                    1
                FROM `{table}` source
                LEFT JOIN `User` actor ON actor.`Id` = source.`{actorColumn}`
                WHERE {predicate};
                """);
        }
    }
}
