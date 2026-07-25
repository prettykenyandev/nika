using FluentValidation.Results;
using NikaFitness.Application.Carts.Commands;
using NikaFitness.Application.Catalog.Commands;
using NikaFitness.Application.Orders.Commands;
using NikaFitness.Application.Pos.Commands;
using NikaFitness.Domain.Payments;

namespace NikaFitness.Application.Tests;

public class CartValidatorTests
{
    private readonly AddToCartCommandValidator _add = new();
    private readonly UpdateCartItemCommandValidator _update = new();

    [Fact]
    public void AddToCart_Accepts_Valid_Command()
    {
        var result = _add.Validate(new AddToCartCommand("cart-1", Guid.NewGuid(), 2));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AddToCart_Rejects_Blank_Cart_Empty_Variant_And_NonPositive_Quantity()
    {
        var result = _add.Validate(new AddToCartCommand("", Guid.Empty, 0));

        Assert.False(result.IsValid);
        AssertHasError(result, nameof(AddToCartCommand.CartId));
        AssertHasError(result, nameof(AddToCartCommand.ProductVariantId));
        AssertHasError(result, nameof(AddToCartCommand.Quantity));
    }

    [Fact]
    public void AddToCart_Rejects_Quantity_Above_Hundred()
    {
        var result = _add.Validate(new AddToCartCommand("cart-1", Guid.NewGuid(), 101));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateCartItem_Allows_Zero_Quantity_For_Removal()
    {
        var result = _update.Validate(new UpdateCartItemCommand("cart-1", Guid.NewGuid(), 0));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateCartItem_Rejects_Negative_And_Over_Hundred()
    {
        Assert.False(_update.Validate(new UpdateCartItemCommand("cart-1", Guid.NewGuid(), -1)).IsValid);
        Assert.False(_update.Validate(new UpdateCartItemCommand("cart-1", Guid.NewGuid(), 101)).IsValid);
    }

    private static void AssertHasError(ValidationResult result, string propertyName)
        => Assert.Contains(result.Errors, e => e.PropertyName == propertyName);
}

public class CheckoutValidatorTests
{
    private readonly CheckoutCommandValidator _validator = new();

    private static CheckoutCommand Valid() => new(
        CartId: "cart-1",
        Email: "jane@example.com",
        FullName: "Jane Doe",
        Line1: "123 Ngong Rd",
        City: "Nairobi",
        Country: "Kenya",
        PhoneNumber: "254712345678");

    [Fact]
    public void Accepts_Valid_Checkout()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void Rejects_Bad_Email(string email)
    {
        Assert.False(_validator.Validate(Valid() with { Email = email }).IsValid);
    }

    [Theory]
    [InlineData("0712345678")]
    [InlineData("254812345678")]
    [InlineData("")]
    public void Rejects_Bad_Phone(string phone)
    {
        var result = _validator.Validate(Valid() with { PhoneNumber = phone });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Rejects_Overlong_FullName()
    {
        Assert.False(_validator.Validate(Valid() with { FullName = new string('x', 151) }).IsValid);
    }
}

public class PosSaleValidatorTests
{
    private readonly CreatePosSaleCommandValidator _validator = new();

    private static CreatePosSaleCommand CardSale() => new(
        Items: new[] { new PosLineInput(Guid.NewGuid(), 1) },
        Method: PaymentMethod.Card);

    [Fact]
    public void Accepts_Valid_Card_Sale_Without_Phone()
    {
        Assert.True(_validator.Validate(CardSale()).IsValid);
    }

    [Fact]
    public void Rejects_Empty_Basket()
    {
        var result = _validator.Validate(CardSale() with { Items = Array.Empty<PosLineInput>() });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Add at least one item to the sale.");
    }

    [Fact]
    public void Rejects_NonPositive_Line_Quantity()
    {
        var result = _validator.Validate(
            CardSale() with { Items = new[] { new PosLineInput(Guid.NewGuid(), 0) } });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Rejects_Cash_Payment_Method()
    {
        var result = _validator.Validate(CardSale() with { Method = PaymentMethod.Cash });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "The till only accepts Card or M-Pesa payments.");
    }

    [Fact]
    public void Mpesa_Requires_A_Valid_Phone()
    {
        var missing = _validator.Validate(CardSale() with { Method = PaymentMethod.Mpesa });
        Assert.False(missing.IsValid);

        var valid = _validator.Validate(
            CardSale() with { Method = PaymentMethod.Mpesa, CustomerPhone = "254712345678" });
        Assert.True(valid.IsValid);
    }

    [Fact]
    public void Card_Sale_Ignores_Phone_Rule()
    {
        // The M-Pesa phone rule is scoped with .When(Method == Mpesa), so a card
        // sale with a bogus phone is still valid.
        var result = _validator.Validate(CardSale() with { CustomerPhone = "nonsense" });
        Assert.True(result.IsValid);
    }
}

public class CatalogValidatorTests
{
    private readonly CreateProductCommandValidator _product = new();
    private readonly CreateCategoryCommandValidator _category = new();

    private static CreateProductCommand ValidProduct() => new(
        Name: "Training Tee",
        Description: "Comfy tee",
        CategoryId: Guid.NewGuid(),
        ImageUrls: Array.Empty<string>(),
        Variants: new[] { new CreateProductVariantInput("TEE-M", "Medium", 1500m, 10) },
        Publish: false);

    [Fact]
    public void Accepts_Valid_Product()
    {
        Assert.True(_product.Validate(ValidProduct()).IsValid);
    }

    [Fact]
    public void Rejects_Product_With_No_Variants()
    {
        var result = _product.Validate(ValidProduct() with { Variants = Array.Empty<CreateProductVariantInput>() });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "A product needs at least one variant.");
    }

    [Fact]
    public void Rejects_Variant_With_NonPositive_Price_Or_Negative_Stock()
    {
        Assert.False(_product.Validate(ValidProduct() with
        {
            Variants = new[] { new CreateProductVariantInput("TEE-M", "Medium", 0m, 10) },
        }).IsValid);

        Assert.False(_product.Validate(ValidProduct() with
        {
            Variants = new[] { new CreateProductVariantInput("TEE-M", "Medium", 10m, -1) },
        }).IsValid);
    }

    [Fact]
    public void Rejects_Blank_Product_Name()
    {
        Assert.False(_product.Validate(ValidProduct() with { Name = "" }).IsValid);
    }

    [Fact]
    public void Category_Accepts_Name_And_Rejects_Blank_Or_Overlong()
    {
        Assert.True(_category.Validate(new CreateCategoryCommand("Apparel", null)).IsValid);
        Assert.False(_category.Validate(new CreateCategoryCommand("", null)).IsValid);
        Assert.False(_category.Validate(new CreateCategoryCommand(new string('x', 121), null)).IsValid);
    }
}
