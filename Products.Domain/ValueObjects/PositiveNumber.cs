using System.Numerics;
using Products.Domain.Exceptions;

namespace Products.Domain.ValueObjects;

public sealed record PositiveNumber<T> : NonNegativeNumber<T>
    where T : INumber<T>
{
    public PositiveNumber(T value) : base(value)
    {
        if (value <= T.Zero)
            throw new DomainValidationException(
                "Value must be greater than zero.");
    }
}