using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Services.Repositories;

namespace Data.Repositories;

public class GradeRepository : IGradeRepository
{
    private readonly NemeBookDbContext _dbContext;

    public GradeRepository(NemeBookDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Grade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Grades
            .Include(g => g.Student)
            .ThenInclude(student => student.Class)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Grade>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Grades
            .Include(g => g.Student)
            .ThenInclude(student => student.Class)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Grade grade, CancellationToken cancellationToken = default)
    {
        await _dbContext.Grades.AddAsync(grade, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateRangeAsync(IEnumerable<Grade> grades, CancellationToken cancellationToken = default)
    {
        await _dbContext.Grades.AddRangeAsync(grades, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Grade grade, CancellationToken cancellationToken = default)
    {
        _dbContext.Grades.Update(grade);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var grade = await GetByIdAsync(id, cancellationToken);
        if (grade is null) return;

        _dbContext.Grades.Remove(grade);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task<IReadOnlyList<Grade>> GetGradesByStudentIdAsync(
        Guid studentId,
        GradeFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Grades
            .Where(g => g.StudentId == studentId);

        query = ApplyFilter(query, filter);

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Grade>> GetGradesByClassSubjectIdAsync(
        Guid classSubjectId,
        GradeFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Grades
            .Where(g => g.ClassSubjectId == classSubjectId);

        query = ApplyFilter(query, filter);

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Grade>> GetGradesByStudentAndSubjectIdAsync(
        Guid studentId,
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        var classSubjectId = await _dbContext.ClassSubjects
            .Where(cs => cs.SubjectId == subjectId)
            .Select(cs => cs.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (classSubjectId == Guid.Empty)
            return new List<Grade>();

        return await _dbContext.Grades
            .Where(g => g.StudentId == studentId && g.ClassSubjectId == classSubjectId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, decimal>> GetAveragesByClassSubjectIdAsync(
        Guid classSubjectId,
        CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.Grades
            .Where(g => g.ClassSubjectId == classSubjectId)
            .GroupBy(g => g.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                Average = g.Average(grade => grade.Value)
            })
            .ToDictionaryAsync(
                x => x.StudentId,
                x => Math.Round(x.Average, 2),
                cancellationToken);

        return result;
    }

    public async Task<GradeStatisticsDto> GetStatisticsByClassSubjectIdAsync(
        Guid classSubjectId,
        CancellationToken cancellationToken = default)
    {
        var grades = await _dbContext.Grades
            .Where(g => g.ClassSubjectId == classSubjectId)
            .Select(g => g.Value)
            .ToListAsync(cancellationToken);

        var result = new GradeStatisticsDto
        {
            TotalGrades = grades.Count
        };

        if (grades.Any())
        {
            result.Average = Math.Round(grades.Average(), 2);
            result.MinGrade = grades.Min();
            result.MaxGrade = grades.Max();

            var distribution = new Dictionary<int, int>();
            foreach (var val in new[] { 2, 3, 4, 5, 6 })
            {
                distribution[val] = grades.Count(g => (int)Math.Floor(g) == val);
            }
            result.GradeDistribution = distribution;
        }

        return result;
    }

    private static IQueryable<Grade> ApplyFilter(IQueryable<Grade> query, GradeFilter? filter)
    {
        if (filter is null)
            return query;

        if (filter.FromDate.HasValue)
            query = query.Where(g => g.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(g => g.CreatedAt <= filter.ToDate.Value);

        if (filter.Type.HasValue)
            query = query.Where(g => g.Type == filter.Type.Value);

        return query;
    }
}
