using Microsoft.EntityFrameworkCore;
using patient_api.Models;

namespace patient_api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<VitalSign> VitalSigns => Set<VitalSign>();
    public DbSet<PatientAuthState> PatientAuthStates => Set<PatientAuthState>();
    public DbSet<PatientTaskLog> PatientTaskLogs => Set<PatientTaskLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // These tables already exist in the legacy CPS schema and are owned by that
        // codebase's LINQ-to-SQL model — never let EF Core migrations try to create/alter them.
        // They also carry legacy triggers, which SQL Server forbids combining with the default
        // "UPDATE ... OUTPUT ..." EF Core issues, hence UseSqlOutputClause(false) on each.
        modelBuilder.Entity<Patient>().ToTable(tb =>
        {
            tb.ExcludeFromMigrations();
            tb.UseSqlOutputClause(false);
        });
        modelBuilder.Entity<Visit>().ToTable(tb =>
        {
            tb.ExcludeFromMigrations();
            tb.UseSqlOutputClause(false);
        });
        modelBuilder.Entity<VitalSign>().ToTable(tb =>
        {
            tb.ExcludeFromMigrations();
            tb.UseSqlOutputClause(false);
        });

        // Precision must mirror the legacy dbo.VitalSign / dbo.Patient column definitions
        // (library/DBKMedNet.designer.cs) exactly, or EF Core's default (18,2) silently
        // truncates values like a 10-digit mobile_no or a whole-number spo2 reading.
        modelBuilder.Entity<VitalSign>().Property(v => v.Height).HasPrecision(22, 2);
        modelBuilder.Entity<VitalSign>().Property(v => v.Temperature).HasPrecision(22, 2);
        modelBuilder.Entity<VitalSign>().Property(v => v.Spo2).HasPrecision(18, 0);
        modelBuilder.Entity<Patient>().Property(p => p.MobileNo).HasPrecision(18, 0);

        modelBuilder.Entity<PatientAuthState>()
            .HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientTaskLog>()
            .HasOne(t => t.Patient)
            .WithMany()
            .HasForeignKey(t => t.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PatientTaskLog>()
            .HasIndex(t => new { t.PatientId, t.LogDate, t.TaskKey })
            .IsUnique();
    }
}
