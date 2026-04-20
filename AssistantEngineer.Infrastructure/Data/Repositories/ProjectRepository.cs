using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Data.Repositories;

internal sealed class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context) => _context = context;

    public async Task<Project?> GetByIdAsync(
        int id,
        bool includeBuildings = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Project> query = _context.Projects;

        if (includeBuildings)
            query = query.Include(project => project.Buildings);

        return await query.FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.Projects
            .OrderBy(project => project.Id)
            .ToListAsync(cancellationToken);

    public void Add(Project project) => _context.Projects.Add(project);
}
