using MediatR;
using Microsoft.EntityFrameworkCore;
using NikaFitness.Application.Catalog.Dtos;
using NikaFitness.Application.Common.Interfaces;

namespace NikaFitness.Application.Catalog.Queries;

public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

public sealed class GetCategoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        return await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug.Value, c.Description))
            .ToListAsync(cancellationToken);
    }
}
