using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Dukate.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialtiesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessCode",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "Specialty",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "AccessCode",
                table: "Administrators");

            migrationBuilder.AlterColumn<string>(
                name: "LastNameMaternal",
                table: "Specialists",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "SpecialtyId",
                table: "Specialists",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "LastNameMaternal",
                table: "Patients",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LastNameMaternal",
                table: "Administrators",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "Specialties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeOfSpecialty = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Specialists_SpecialtyId",
                table: "Specialists",
                column: "SpecialtyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Specialists_Specialties_SpecialtyId",
                table: "Specialists",
                column: "SpecialtyId",
                principalTable: "Specialties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Specialists_Specialties_SpecialtyId",
                table: "Specialists");

            migrationBuilder.DropTable(
                name: "Specialties");

            migrationBuilder.DropIndex(
                name: "IX_Specialists_SpecialtyId",
                table: "Specialists");

            migrationBuilder.DropColumn(
                name: "SpecialtyId",
                table: "Specialists");

            migrationBuilder.AlterColumn<string>(
                name: "LastNameMaternal",
                table: "Specialists",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessCode",
                table: "Specialists",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Specialty",
                table: "Specialists",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "LastNameMaternal",
                table: "Patients",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastNameMaternal",
                table: "Administrators",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessCode",
                table: "Administrators",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
