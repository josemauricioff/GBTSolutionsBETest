using Products.Domain.Exceptions;
using Products.Domain.ValueObjects;

namespace Products.Tests;

public sealed class NumberValueObjectTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void NonNegativeIntegerAcceptsZeroAndPositiveValues(int value) =>
        Assert.Equal(value, new NonNegativeNumber<int>(value).Value);

    [Theory]
    [InlineData("0")]
    [InlineData("0.01")]
    [InlineData("25.50")]
    public void NonNegativeDecimalPreservesValue(string text)
    {
        var value = decimal.Parse(
            text,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(
            value,
            new NonNegativeNumber<decimal>(value).Value);
    }

    [Fact]
    public void NonNegativeNumbersRejectNegativeValues()
    {
        Assert.Throws<DomainValidationException>(
            () => new NonNegativeNumber<int>(-1));

        Assert.Throws<DomainValidationException>(
            () => new NonNegativeNumber<decimal>(-0.01m));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("0")]
    public void PositiveDecimalRejectsZeroAndNegativeValues(string text)
    {
        var value = decimal.Parse(
            text,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<DomainValidationException>(
            () => new PositiveNumber<decimal>(value));
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("25.50")]
    public void PositiveDecimalPreservesPositiveValue(string text)
    {
        var value = decimal.Parse(
            text,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(
            value,
            new PositiveNumber<decimal>(value).Value);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NumberTypesRejectNonFiniteValues(double value)
    {
        Assert.Throws<DomainValidationException>(
            () => new NonNegativeNumber<double>(value));

        Assert.Throws<DomainValidationException>(
            () => new PositiveNumber<double>(value));
    }

    [Fact]
    public void PositiveNumberCanBeUsedAsNonNegativeNumber()
    {
        NonNegativeNumber<int> number = new PositiveNumber<int>(5);

        Assert.Equal(5, number.Value);
    }

    [Fact]
    public void EqualValuesOfTheSameTypeHaveValueEquality()
    {
        Assert.Equal(
            new NonNegativeNumber<int>(5),
            new NonNegativeNumber<int>(5));

        Assert.Equal(
            new PositiveNumber<decimal>(2.50m),
            new PositiveNumber<decimal>(2.50m));

        Assert.Equal(
            new PositiveNumber<int>(5).GetHashCode(),
            new PositiveNumber<int>(5).GetHashCode());

        Assert.NotEqual(
            new NonNegativeNumber<int>(5),
            new NonNegativeNumber<int>(6));
    }

    [Fact]
    public void DifferentNumberTypesHaveDistinctValueEquality()
    {
        NonNegativeNumber<int> nonNegative =
            new NonNegativeNumber<int>(5);

        NonNegativeNumber<int> positive =
            new PositiveNumber<int>(5);

        Assert.NotEqual(nonNegative, positive);
    }
}