using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PizzaShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBuyXGetYRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BuyXGetY_BuyQuantity",
                table: "Promotions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuyXGetY_GetQuantity",
                table: "Promotions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyXGetY_RewardDiscountPercentage",
                table: "Promotions",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BuyXGetY_RewardMenuItemId",
                table: "Promotions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BuyXGetY_TriggerMenuItemId",
                table: "Promotions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyXGetY_BuyQuantity",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "BuyXGetY_GetQuantity",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "BuyXGetY_RewardDiscountPercentage",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "BuyXGetY_RewardMenuItemId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "BuyXGetY_TriggerMenuItemId",
                table: "Promotions");
        }
    }
}
