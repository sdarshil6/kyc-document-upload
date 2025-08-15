using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KYCDocumentAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5a61a814-34cb-49fb-973f-9b38cf9b06bf"));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "City", "CreatedAt", "CreatedBy", "DateOfBirth", "Email", "FirstName", "IsActive", "LastName", "PhoneNumber", "PinCode", "State", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), null, "Mumbai", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(1990, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "john.doe@example.com", "John", true, "Doe", "9876543210", "400001", "Maharashtra", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "City", "CreatedAt", "CreatedBy", "DateOfBirth", "Email", "FirstName", "IsActive", "LastName", "PhoneNumber", "PinCode", "State", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("5a61a814-34cb-49fb-973f-9b38cf9b06bf"), null, "Mumbai", new DateTime(2025, 8, 14, 20, 7, 49, 819, DateTimeKind.Utc).AddTicks(9358), null, new DateTime(1990, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "john.doe@example.com", "John", true, "Doe", "9876543210", "400001", "Maharashtra", new DateTime(2025, 8, 14, 20, 7, 49, 819, DateTimeKind.Utc).AddTicks(9494), null });
        }
    }
}
