using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KYCDocumentAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    PinCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KYCVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalDocuments = table.Column<int>(type: "integer", nullable: false),
                    VerifiedDocuments = table.Column<int>(type: "integer", nullable: false),
                    RejectedDocuments = table.Column<int>(type: "integer", nullable: false),
                    OverallScore = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KYCVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KYCVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AadhaarNumber = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    PANNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PassportNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PinCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RawExtractedData = table.Column<string>(type: "text", nullable: true),
                    ExtractionConfidence = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentData_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    KYCVerificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AuthenticityScore = table.Column<double>(type: "double precision", nullable: false),
                    QualityScore = table.Column<double>(type: "double precision", nullable: false),
                    ConsistencyScore = table.Column<double>(type: "double precision", nullable: false),
                    FraudScore = table.Column<double>(type: "double precision", nullable: false),
                    IsFormatValid = table.Column<bool>(type: "boolean", nullable: false),
                    IsDataConsistent = table.Column<bool>(type: "boolean", nullable: false),
                    IsImageClear = table.Column<bool>(type: "boolean", nullable: false),
                    IsTampered = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReasons = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AIInsights = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationResults_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VerificationResults_KYCVerifications_KYCVerificationId",
                        column: x => x.KYCVerificationId,
                        principalTable: "KYCVerifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "City", "CreatedAt", "CreatedBy", "DateOfBirth", "Email", "FirstName", "IsActive", "LastName", "PhoneNumber", "PinCode", "State", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("5a61a814-34cb-49fb-973f-9b38cf9b06bf"), null, "Mumbai", new DateTime(2025, 8, 14, 20, 7, 49, 819, DateTimeKind.Utc).AddTicks(9358), null, new DateTime(1990, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "john.doe@example.com", "John", true, "Doe", "9876543210", "400001", "Maharashtra", new DateTime(2025, 8, 14, 20, 7, 49, 819, DateTimeKind.Utc).AddTicks(9494), null });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentData_DocumentId",
                table: "DocumentData",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UserId",
                table: "Documents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_KYCVerifications_UserId",
                table: "KYCVerifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationResults_DocumentId",
                table: "VerificationResults",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationResults_KYCVerificationId",
                table: "VerificationResults",
                column: "KYCVerificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentData");

            migrationBuilder.DropTable(
                name: "VerificationResults");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "KYCVerifications");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
