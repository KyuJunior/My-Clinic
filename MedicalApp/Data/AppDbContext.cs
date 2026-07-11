using Microsoft.EntityFrameworkCore;
using MedicalApp.Models;

namespace MedicalApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Visit> Visits => Set<Visit>();
        public DbSet<QueueEntry> QueueEntries => Set<QueueEntry>();
        public DbSet<Drug> Drugs => Set<Drug>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<DoctorSetting> DoctorSettings => Set<DoctorSetting>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Patient -> Visits (One-to-Many, cascade delete)
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.Visits)
                .WithOne(v => v.Patient)
                .HasForeignKey(v => v.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Patient -> QueueEntries (One-to-Many, cascade delete)
            modelBuilder.Entity<QueueEntry>()
                .HasOne(q => q.Patient)
                .WithMany()
                .HasForeignKey(q => q.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
