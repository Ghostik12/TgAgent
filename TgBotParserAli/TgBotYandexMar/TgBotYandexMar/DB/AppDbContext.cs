using Microsoft.EntityFrameworkCore;
using TgBotYandexMar.Models;

namespace TgBotYandexMar.DB
{
    public class AppDbContext : DbContext
    {
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Admins> Admin { get; set; }
        public DbSet<KeywordSetting> KeywordSettings { get; set; }
        public DbSet<KeywordStat> KeywordStats { get; set; }
        public DbSet<PostSettings> PostSetting { get; set; }
        public DbSet<ChannekStat> ChannelStats { get; set; }

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
            // Добавляем начальные данные
            modelBuilder.Entity<Admins>().HasData(
                new Admins { Id = 2, ChatId = 292720339, UserName = "admin" }
            );
        }
    }
}