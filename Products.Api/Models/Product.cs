using System.ComponentModel.DataAnnotations;
namespace Products.Api.Models;
public sealed class Product
{
 public int Id { get; set; }
 public string Name { get; set; } = string.Empty;
 public decimal Price { get; set; }
 public int Stock { get; set; }
}
public sealed class ProductRequest : IValidatableObject
{
 [Required, StringLength(200)] public string? Name { get; init; }
 [Required, Range(typeof(decimal), "0.01", "9999999999.99", ParseLimitsInInvariantCulture = true)] public decimal? Price { get; init; }
 [Required, Range(0, int.MaxValue)] public int? Stock { get; init; }
 public IEnumerable<ValidationResult> Validate(ValidationContext context)
 {
  if (string.IsNullOrWhiteSpace(Name)) yield return new ValidationResult("Name must not be blank.", [nameof(Name)]);
  if (Price is decimal price && decimal.Round(price, 2) != price) yield return new ValidationResult("Price must have at most two decimal places.", [nameof(Price)]);
 }
}

