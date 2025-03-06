using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TwoFactorAuthProj.Migrations
{
    /// <inheritdoc />
    public partial class AddedCOUNTERinOtpRecordTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Purpose",
                table: "OtpRecords",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<long>(
                name: "Counter",
                table: "OtpRecords",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Counter",
                table: "OtpRecords");

            migrationBuilder.AlterColumn<int>(
                name: "Purpose",
                table: "OtpRecords",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
