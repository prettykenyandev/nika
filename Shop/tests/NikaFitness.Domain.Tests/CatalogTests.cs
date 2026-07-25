using NikaFitness.Domain.Catalog;
using NikaFitness.Domain.Common;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Domain.Tests;

public class ProductVariantTests
{
    private static Product NewProduct() => new("Training Tee", "A comfy tee", Guid.NewGuid());

    [Fact]
    public void AddVariant_Normalises_Sku_And_Name()
    {
        var product = NewProduct();

        var variant = product.AddVariant(" tee-m ", "  Medium ", new Money(1500m), 10);

        Assert.Equal("TEE-M", variant.Sku);
        Assert.Equal("Medium", variant.Name);
        Assert.Equal(10, variant.StockQuantity);
    }

    [Fact]
    public void AddVariant_Rejects_Duplicate_Sku_Case_Insensitively()
    {
        var product = NewProduct();
        product.AddVariant("TEE-M", "Medium", new Money(1500m), 10);

        Assert.Throws<DomainException>(
            () => product.AddVariant("tee-m", "Medium again", new Money(1500m), 5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_Blank_Sku(string sku)
    {
        var product = NewProduct();
        Assert.Throws<DomainException>(() => product.AddVariant(sku, "Medium", new Money(1m), 1));
    }

    [Fact]
    public void Rejects_Negative_Stock()
    {
        var product = NewProduct();
        Assert.Throws<DomainException>(() => product.AddVariant("TEE-M", "Medium", new Money(1m), -1));
    }

    [Fact]
    public void HasStock_Reflects_Available_Quantity()
    {
        var variant = NewProduct().AddVariant("TEE-M", "Medium", new Money(1m), 3);

        Assert.True(variant.HasStock(3));
        Assert.False(variant.HasStock(4));
    }

    [Fact]
    public void Reserve_Reduces_Stock_And_Guards_Availability()
    {
        var variant = NewProduct().AddVariant("TEE-M", "Medium", new Money(1m), 3);

        variant.Reserve(2);
        Assert.Equal(1, variant.StockQuantity);

        Assert.Throws<DomainException>(() => variant.Reserve(2));
        Assert.Throws<DomainException>(() => variant.Reserve(0));
    }

    [Fact]
    public void Restock_Increases_Stock_And_Rejects_NonPositive()
    {
        var variant = NewProduct().AddVariant("TEE-M", "Medium", new Money(1m), 3);

        variant.Restock(5);
        Assert.Equal(8, variant.StockQuantity);

        Assert.Throws<DomainException>(() => variant.Restock(0));
    }
}

public class ProductTests
{
    [Fact]
    public void Constructor_Sets_Slug_From_Name()
    {
        var product = new Product("Men's Training Tee", "Comfy", Guid.NewGuid());
        Assert.Equal("men-s-training-tee", product.Slug.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_Blank_Description(string description)
    {
        Assert.Throws<DomainException>(() => new Product("Tee", description, Guid.NewGuid()));
    }

    [Fact]
    public void Rejects_Empty_Category()
    {
        Assert.Throws<DomainException>(() => new Product("Tee", "Comfy", Guid.Empty));
    }

    [Fact]
    public void Rename_Updates_Name_And_Slug()
    {
        var product = new Product("Old Name", "Comfy", Guid.NewGuid());
        product.Rename("  New Name  ");

        Assert.Equal("New Name", product.Name);
        Assert.Equal("new-name", product.Slug.Value);
    }

    [Fact]
    public void GetVariant_Throws_When_Missing()
    {
        var product = new Product("Tee", "Comfy", Guid.NewGuid());
        Assert.Throws<DomainException>(() => product.GetVariant(Guid.NewGuid()));
    }

    [Fact]
    public void Publish_Requires_At_Least_One_Variant()
    {
        var product = new Product("Tee", "Comfy", Guid.NewGuid());
        Assert.Throws<DomainException>(product.Publish);

        product.AddVariant("TEE-M", "Medium", new Money(1m), 1);
        product.Publish();
        Assert.True(product.IsPublished);

        product.Unpublish();
        Assert.False(product.IsPublished);
    }

    [Fact]
    public void AddImage_Rejects_Blank_Url()
    {
        var product = new Product("Tee", "Comfy", Guid.NewGuid());
        Assert.Throws<DomainException>(() => product.AddImage(" "));
    }
}
