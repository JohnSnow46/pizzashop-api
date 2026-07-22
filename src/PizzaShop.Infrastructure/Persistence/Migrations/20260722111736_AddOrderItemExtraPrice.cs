using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzaShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemExtraPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "OrderItemExtras",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "OrderItemExtras");
        }
    }
}
