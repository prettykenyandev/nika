using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Domain.Expenses;

namespace NikaFitness.Application.Expenses.Commands;

public sealed record CreateExpenseCategoryCommand(string Name, string? Description)
    : IRequest<Guid>;

public sealed class CreateExpenseCategoryCommandValidator
    : AbstractValidator<CreateExpenseCategoryCommand>
{
    public CreateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public sealed class CreateExpenseCategoryCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateExpenseCategoryCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateExpenseCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = new ExpenseCategory(request.Name, request.Description);

        var exists = await db.ExpenseCategories
            .AnyAsync(c => c.Slug == category.Slug, cancellationToken);
        if (exists)
            throw new Domain.Common.DomainException(
                $"An expense category named '{category.Name}' already exists.");

        db.ExpenseCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
