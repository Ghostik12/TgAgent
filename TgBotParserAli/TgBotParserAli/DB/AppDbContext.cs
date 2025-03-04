using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TgBotParserAli.Models;

namespace TgBotParserAli.DB
{
    public class AppDbContext : DbContext
    {
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Admins> Admin { get; set; }

        public AppDbContext()
        {
            Database.EnsureDeleted();  // удаляем бд со старой схемой
            Database.EnsureCreated();
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseNpgsql("Host=localhost;Database=epnbot1;Username=postgres;Password=12345Ob@");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Добавляем начальные данные
            modelBuilder.Entity<Admins>().HasData(
                new Admins { Id = 1, ChatId = 1451999567, UserName = "faust_harric" }
            );

            modelBuilder.Entity<Channel>()
                .Property(c => c.ParseFrequency)
                .HasConversion(
                    v => v.ToString(), // Преобразуем TimeSpan в строку
                    v => TimeSpan.Parse(v) // Преобразуем строку в TimeSpan
                );

            modelBuilder.Entity<Channel>()
                .Property(c => c.PostFrequency)
                .HasConversion(
                    v => v.ToString(), // Преобразуем TimeSpan в строку
                    v => TimeSpan.Parse(v) // Преобразуем строку в TimeSpan
                );
        }
    }

}
