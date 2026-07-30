using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletionAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ExecuteIfColumnMissing(migrationBuilder, "User", "DeletedAt", "ALTER TABLE `User` ADD `DeletedAt` datetime(6) NULL;");
            ExecuteIfColumnMissing(migrationBuilder, "User", "DeletedBy", "ALTER TABLE `User` ADD `DeletedBy` bigint NULL;");
            ExecuteIfColumnMissing(migrationBuilder, "Role", "DeletedAt", "ALTER TABLE `Role` ADD `DeletedAt` datetime(6) NULL;");
            ExecuteIfColumnMissing(migrationBuilder, "Role", "DeletedBy", "ALTER TABLE `Role` ADD `DeletedBy` bigint NULL;");
            ExecuteIfColumnMissing(migrationBuilder, "Categories", "DeletedAt", "ALTER TABLE `Categories` ADD `DeletedAt` datetime(6) NULL;");
            ExecuteIfColumnMissing(migrationBuilder, "Categories", "DeletedBy", "ALTER TABLE `Categories` ADD `DeletedBy` bigint NULL;");
            ExecuteIfColumnMissing(migrationBuilder, "Books", "DeletedAt", "ALTER TABLE `Books` ADD `DeletedAt` datetime(6) NULL;");
            ExecuteIfColumnMissing(migrationBuilder, "Books", "DeletedBy", "ALTER TABLE `Books` ADD `DeletedBy` bigint NULL;");
            ExecuteIfColumnMissing(migrationBuilder, "BookBorrowRecords", "DeletedAt", "ALTER TABLE `BookBorrowRecords` ADD `DeletedAt` datetime(6) NULL;");
            ExecuteIfColumnMissing(migrationBuilder, "BookBorrowRecords", "DeletedBy", "ALTER TABLE `BookBorrowRecords` ADD `DeletedBy` bigint NULL;");
            ExecuteIfColumnMissing(migrationBuilder, "Authors", "DeletedAt", "ALTER TABLE `Authors` ADD `DeletedAt` datetime(6) NULL;");
            ExecuteIfColumnMissing(migrationBuilder, "Authors", "DeletedBy", "ALTER TABLE `Authors` ADD `DeletedBy` bigint NULL;");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "DeletedAt", "DeletedBy" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "DeletedAt", "DeletedBy" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "DeletedAt", "DeletedBy" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ExecuteIfColumnExists(migrationBuilder, "User", "DeletedAt", "ALTER TABLE `User` DROP COLUMN `DeletedAt`;");
            ExecuteIfColumnExists(migrationBuilder, "User", "DeletedBy", "ALTER TABLE `User` DROP COLUMN `DeletedBy`;");
            ExecuteIfColumnExists(migrationBuilder, "Role", "DeletedAt", "ALTER TABLE `Role` DROP COLUMN `DeletedAt`;");
            ExecuteIfColumnExists(migrationBuilder, "Role", "DeletedBy", "ALTER TABLE `Role` DROP COLUMN `DeletedBy`;");
            ExecuteIfColumnExists(migrationBuilder, "Categories", "DeletedAt", "ALTER TABLE `Categories` DROP COLUMN `DeletedAt`;");
            ExecuteIfColumnExists(migrationBuilder, "Categories", "DeletedBy", "ALTER TABLE `Categories` DROP COLUMN `DeletedBy`;");
            ExecuteIfColumnExists(migrationBuilder, "Books", "DeletedAt", "ALTER TABLE `Books` DROP COLUMN `DeletedAt`;");
            ExecuteIfColumnExists(migrationBuilder, "Books", "DeletedBy", "ALTER TABLE `Books` DROP COLUMN `DeletedBy`;");
            ExecuteIfColumnExists(migrationBuilder, "BookBorrowRecords", "DeletedAt", "ALTER TABLE `BookBorrowRecords` DROP COLUMN `DeletedAt`;");
            ExecuteIfColumnExists(migrationBuilder, "BookBorrowRecords", "DeletedBy", "ALTER TABLE `BookBorrowRecords` DROP COLUMN `DeletedBy`;");
            ExecuteIfColumnExists(migrationBuilder, "Authors", "DeletedAt", "ALTER TABLE `Authors` DROP COLUMN `DeletedAt`;");
            ExecuteIfColumnExists(migrationBuilder, "Authors", "DeletedBy", "ALTER TABLE `Authors` DROP COLUMN `DeletedBy`;");
        }

        private static void ExecuteIfColumnMissing(
            MigrationBuilder migrationBuilder,
            string tableName,
            string columnName,
            string sql)
        {
            ExecuteConditionalSql(migrationBuilder, tableName, columnName, sql, shouldExist: false);
        }

        private static void ExecuteIfColumnExists(
            MigrationBuilder migrationBuilder,
            string tableName,
            string columnName,
            string sql)
        {
            ExecuteConditionalSql(migrationBuilder, tableName, columnName, sql, shouldExist: true);
        }

        private static void ExecuteConditionalSql(
            MigrationBuilder migrationBuilder,
            string tableName,
            string columnName,
            string sql,
            bool shouldExist)
        {
            var existenceCheck = shouldExist ? "> 0" : "= 0";

            migrationBuilder.Sql($"""
                SET @column_exists = (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = '{tableName}'
                      AND COLUMN_NAME = '{columnName}'
                );
                SET @migration_sql = IF(@column_exists {existenceCheck}, '{sql}', 'SELECT 1');
                PREPARE migration_stmt FROM @migration_sql;
                EXECUTE migration_stmt;
                DEALLOCATE PREPARE migration_stmt;
                """);
        }
    }
}
