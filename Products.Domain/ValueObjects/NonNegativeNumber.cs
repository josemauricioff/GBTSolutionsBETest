using System.Numerics;
using Products.Domain.Exceptions;

namespace Products.Domain.ValueObjects;

public record NonNegativeNumber<T> where T : INumber<T>
{
    public T Value { get; }

    public NonNegativeNumber(T value)
    {
        if (!T.IsFinite(value) || value < T.Zero)
            throw new DomainValidationException(
                "Value must be finite and greater than or equal to zero.");

        Value = value;
    }
}