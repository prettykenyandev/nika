using FluentValidation;
using MediatR;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Catalog;

namespace NikaFitness.Application.Catalog.Commands;

public sealed record CreateCategoryCommand(string Name, string? Description) : IRequest<Guid>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}

public sealed class CreateCategoryCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category(request.Name, request.Description);
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
