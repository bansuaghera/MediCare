using MediCare.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<OPDSchedule> OPDSchedules { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<PrescriptionTemplate> PrescriptionTemplates { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Appointment)
                .WithMany()
                .HasForeignKey(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
