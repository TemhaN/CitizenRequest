using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Reflection.Emit;
using CitizenRequest.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CitizenRequest.Data
{
    public class CitizenRequestsContext : DbContext
    {
        public CitizenRequestsContext(DbContextOptions<CitizenRequestsContext> options)
            : base(options) { }

        public DbSet<Citizen> Citizens { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CitizenApplication> CitizenApplications { get; set; }
        public DbSet<Response> Responses { get; set; }
        public DbSet<CitizenSession> CitizenSessions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var statusConverter = new ValueConverter<ApplicationStatus, string>(
                v => v == ApplicationStatus.New
                        ? "Новое"
                        : v == ApplicationStatus.InProgress
                            ? "В обработке"
                            : v == ApplicationStatus.Resolved
                                ? "Рассмотрено"
                                : v == ApplicationStatus.Rejected
                                    ? "Отклонено"
                                    : "Unsupported",
                v => v == "Новое"
                        ? ApplicationStatus.New
                        : v == "В обработке"
                            ? ApplicationStatus.InProgress
                            : v == "Рассмотрено"
                                ? ApplicationStatus.Resolved
                                : v == "Отклонено"
                                    ? ApplicationStatus.Rejected
                                    : ApplicationStatus.New);

            modelBuilder.Entity<CitizenApplication>()
                .Property(a => a.Status)
                .HasConversion(statusConverter);

            base.OnModelCreating(modelBuilder);
        }

    }
}
