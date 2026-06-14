using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Application.Expenses.Dtos;

namespace NikaFitness.Application.Expenses.Queries;

public sealed record GetExpenseCategoriesQuery : IRequest<IReadOnlyList<ExpenseCategoryDto>>;

public sealed class GetExpenseCategoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetExpenseCategoriesQuery, IReadOnlyList<ExpenseCategoryDto>>
{
    public async Task<IReadOnlyList<ExpenseCategoryDto>> Handle(
        GetExpenseCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        return await db.ExpenseCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ExpenseCategoryDto(c.Id, c.Name, c.Slug.Value, c.Description))
            .ToListAsync(cancellationToken);
    }
}
