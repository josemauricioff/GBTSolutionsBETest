using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Products.Infrastructure.Data;

namespace Products.Infrastructure.Migrations;

[DbContext(typeof(ProductDbContext))]
[Migration("20261027000100_Initial")]
public sealed partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migration)
    {
        migration.CreateSequence<int>(name: "ProductIdSequence", schema: "dbo", startValue: 100003, minValue: 100000, maxValue: 999999);
        migration.CreateTable(name: "Products", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR [dbo].[ProductIdSequence]"),
            Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            Price = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
            Stock = table.Column<int>(type: "int", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Products", product => product.Id);
            table.CheckConstraint("CK_Products_Id", "[Id] BETWEEN 100000 AND 999999");
            table.CheckConstraint("CK_Products_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
            table.CheckConstraint("CK_Products_Price", "[Price] > 0");
            table.CheckConstraint("CK_Products_Stock", "[Stock] >= 0");
        });
        migration.InsertData(table: "Products", columns: new[] { "Id", "Name", "Price", "Stock" }, columnTypes: new[] { "int", "nvarchar(200)", "decimal(12,2)", "int" }, values: new object[,]
        {
            { 100000, "Optical lens", 89.90m, 25 },
            { 100001, "Lens cleaner", 12.50m, 50 },
            { 100002, "Frame", 125.00m, 10 }
        });
    }

    protected override void Down(MigrationBuilder migration)
    {
        migration.DropTable("Products");
        migration.DropSequence(name: "ProductIdSequence", schema: "dbo");
    }
}



