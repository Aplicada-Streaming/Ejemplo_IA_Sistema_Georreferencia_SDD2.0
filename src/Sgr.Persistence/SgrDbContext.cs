using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Audit;
using Sgr.Domain.Identity;
using Sgr.Domain.Photos;
using Sgr.Domain.Points;
using Sgr.Domain.Surveys;
using Sgr.Domain.Templates;

namespace Sgr.Persistence;

public class SgrDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Point> Points => Set<Point>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public SgrDbContext(DbContextOptions<SgrDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SgrDbContext).Assembly);
    }
}
