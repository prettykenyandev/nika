using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Exceptions;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Catalog;
using NikaFitness.Domain.ValueObjects;

namespace NikaFitness.Application.Catalog.Commands;

public sealed record CreateProductVariantInput(string Sku, string Name, decimal Price, int StockQuantity);

public sealed record CreateProductCommand(
    string Name,
    string Description,
    Guid CategoryId,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<CreateProductVariantInput> Variants,
    bool Publish) : IRequest<Guid>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Variants).NotEmpty().WithMessage("A product needs at least one variant.");
        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(i => i.Sku).NotEmpty().MaximumLength(64);
            v.RuleFor(i => i.Name).NotEmpty().MaximumLength(120);
            v.RuleFor(i => i.Price).GreaterThan(0);
            v.RuleFor(i => i.StockQuantity).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CreateProductCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await db.Categories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
            throw new NotFoundException("Category", request.CategoryId);

        var product = new Product(request.Name, request.Description, request.CategoryId);

        foreach (var image in request.ImageUrls)
            product.AddImage(image);

        foreach (var v in request.Variants)
            product.AddVariant(v.Sku, v.Name, new Money(v.Price), v.StockQuantity);

        if (request.Publish)
            product.Publish();

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
