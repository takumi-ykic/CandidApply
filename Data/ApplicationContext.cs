using CandidApply.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CandidApply.Data
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        public DbSet<Application> applications { get; set; }
        public DbSet<ApplicationStatus> status { get; set; }
        public DbSet<ApplicationFile> files { get; set; }
        public DbSet<Interview> interviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Application>().ToTable("Application");
            modelBuilder.Entity<ApplicationStatus>().ToTable("ApplicationStatus");
            modelBuilder.Entity<ApplicationFile>().ToTable("ApplicationFile");
            modelBuilder.Entity<Interview>().ToTable("Interview");

            modelBuilder.Entity<Application>()
                .HasKey(a => a.applicationId);

            modelBuilder.Entity<Application>()
                .Property(a => a.applicationId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Application>()
                .HasIndex(a => a.applicationId)
                .IsUnique(true);

            modelBuilder.Entity<Application>()
                .HasOne(a => a.ApplicationStatus)
                .WithMany()
                .HasForeignKey(a => a.status)
                .IsRequired();

            modelBuilder.Entity<ApplicationStatus>()
                .HasKey(ast => ast.statusId);

            modelBuilder.Entity<Application>()
                .HasOne(a => a.ApplicationFile)
                .WithOne(af => af.Application)
                .HasForeignKey<ApplicationFile>(af => af.applicationId);

            modelBuilder.Entity<ApplicationFile>()
                .HasKey(af => af.fileId);

            modelBuilder.Entity<ApplicationFile>()
                .Property(af => af.fileId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Interview>()
                .HasKey(i => i.interviewId);

            modelBuilder.Entity<Interview>()
                .Property(i => i.interviewId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Application>()
                .HasOne(a => a.Interview)
                .WithOne(i => i.Application)
                .HasForeignKey<Interview>(i => i.applicationId);
        }
    }
}
