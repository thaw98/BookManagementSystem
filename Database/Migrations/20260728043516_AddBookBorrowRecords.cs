using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBookBorrowRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookBorrowRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BookId = table.Column<long>(type: "bigint", nullable: false),
                    BorrowedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookBorrowRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookBorrowRecords_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookBorrowRecords_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BookBorrowRecords_BookId",
                table: "BookBorrowRecords",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookBorrowRecords_BorrowedAt",
                table: "BookBorrowRecords",
                column: "BorrowedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookBorrowRecords_ReturnedAt",
                table: "BookBorrowRecords",
                column: "ReturnedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookBorrowRecords_UserId",
                table: "BookBorrowRecords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookBorrowRecords");
        }
    }
}
