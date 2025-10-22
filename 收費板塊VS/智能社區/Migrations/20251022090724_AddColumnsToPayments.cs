using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCommunity.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnsToPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayMethod",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "PayDate",
                table: "Payments",
                newName: "PaymentDate");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "PaymentDate",
                table: "Payments",
                newName: "PayDate");

            migrationBuilder.AddColumn<decimal>(
                name: "PayAmount",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PayMethod",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
