using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Products.Infrastructure.Data;

namespace Products.Infrastructure.Migrations;

[DbContext(typeof(ProductDbContext))]
public sealed class ProductDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0").HasAnnotation("Relational:MaxIdentifierLength", 128);
        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);
        modelBuilder.HasSequence<int>("ProductIdSequence", "dbo").StartsAt(100003).IncrementsBy(1).HasMin(100000).HasMax(999999).IsCyclic(false);
        modelBuilder.Entity("Products.Domain.Entities.Product", product =>
        {
            product.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int").HasDefaultValueSql("NEXT VALUE FOR [dbo].[ProductIdSequence]");
            product.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            product.Property<decimal>("Price").HasPrecision(12, 2).HasColumnType("decimal(12,2)");
            product.Property<int>("Stock").HasColumnType("int");
            product.HasKey("Id");
            product.ToTable("Products", table =>
            {
                table.HasCheckConstraint("CK_Products_Id", "[Id] BETWEEN 100000 AND 999999");
                table.HasCheckConstraint("CK_Products_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
                table.HasCheckConstraint("CK_Products_Price", "[Price] > 0");
                table.HasCheckConstraint("CK_Products_Stock", "[Stock] >= 0");
            });
            product.HasData(
                new { Id = 100000, Name = "Optical lens", Price = 89.90m, Stock = 25 },
                new { Id = 100001, Name = "Lens cleaner", Price = 12.50m, Stock = 50 },
                new { Id = 100002, Name = "Frame", Price = 125m, Stock = 10 });
        });
    }
}

