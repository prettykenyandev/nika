using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Tests;

public class SlugTests
{
    [Theory]
    [InlineData("Men's Training Tee", "men-s-training-tee")]
    [InlineData("  Hello World  ", "hello-world")]
    [InlineData("Already-Slugged", "already-slugged")]
    [InlineData("Multiple   spaces", "multiple-spaces")]
    [InlineData("Trailing!!!", "trailing")]
    public void Create_Produces_Url_Friendly_Value(string input, string expected)
    {
        Assert.Equal(expected, Slug.Create(input).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Blank_Source(string input)
    {
        Assert.Throws<DomainException>(() => Slug.Create(input));
    }

    [Fact]
    public void Create_Rejects_Source_With_No_Alphanumerics()
    {
        Assert.Throws<DomainException>(() => Slug.Create("!!!"));
    }

    [Fact]
    public void FromExisting_Preserves_Value_Verbatim()
    {
        Assert.Equal("Raw Value", Slug.FromExisting("Raw Value").Value);
    }

    [Fact]
    public void ToString_Returns_Value()
    {
        Assert.Equal("men-s-training-tee", Slug.Create("Men's Training Tee").ToString());
    }
}
