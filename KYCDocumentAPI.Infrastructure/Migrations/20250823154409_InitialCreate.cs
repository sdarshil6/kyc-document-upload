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
            migrationBuilder.DropTable(
                name: "VerificationResults");

            migrationBuilder.DropTable(
                name: "KYCVerifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KYCVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OverallScore = table.Column<double>(type: "double precision", nullable: false),
                    RejectedDocuments = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalDocuments = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    VerifiedDocuments = table.Column<int>(type: "integer", nullable: false)
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
                name: "VerificationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    KYCVerificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    AIInsights = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AuthenticityScore = table.Column<double>(type: "double precision", nullable: false),
                    ConsistencyScore = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    FailureReasons = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FraudScore = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDataConsistent = table.Column<bool>(type: "boolean", nullable: false),
                    IsFormatValid = table.Column<bool>(type: "boolean", nullable: false),
                    IsImageClear = table.Column<bool>(type: "boolean", nullable: false),
                    IsTampered = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    QualityScore = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_KYCVerifications_UserId",
                table: "KYCVerifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationResults_DocumentId",
                table: "VerificationResults",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationResults_KYCVerificationId",
                table: "VerificationResults",
                column: "KYCVerificationId");
        }
    }
}
