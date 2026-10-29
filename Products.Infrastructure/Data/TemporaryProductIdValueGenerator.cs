using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Products.Domain.Entities;
using Products.Domain.ValueObjects;

namespace Products.Infrastructure.Data;

public sealed class TemporaryProductIdValueGenerator
    : ValueGenerator<PositiveNumber<int>>
{
    private static long nextValue = Product.MaximumIdentifier;

    public override bool GeneratesTemporaryValues => true;

    public override PositiveNumber<int> Next(EntityEntry entry)
    {
        var value = Interlocked.Increment(ref nextValue);

        if (value > int.MaxValue)
        {
            throw new InvalidOperationException(
                "Temporary product identifier range has been exhausted.");
        }

        // These keys exist only in EF tracking,
        // outside the persisted six-digit range.
        return new PositiveNumber<int>((int)value);
    }
}