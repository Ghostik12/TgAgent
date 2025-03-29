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
        public DbSet<KeywordSetting> KeywordSettings { get; set; }
        public DbSet<KeywordStat> KeywordStats { get; set; }
        public DbSet<PostSettings> PostSetting { get; set; }
        public DbSet<Token> Tokens { get; set; }

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

            modelBuilder.Entity<Token>().HasData(
                new Token
                {
                    Id = 1,
                    AccessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJ0b2tlbl90eXBlIjoiYWNjZXNzX3Rva2VuIiwiZXhwIjoxNzQyNTAwNDE3LCJ1c2VyX2lkIjo2OTQzNzksInVzZXJfcm9sZSI6InVzZXIiLCJjbGllbnRfcGxhdGZvcm0iOiJ3ZWIiLCJjbGllbnRfaXAiOiI5MS4yMzcuMTc5LjE4MSIsImNsaWVudF9pZCI6IjVDQmhqWTBQbXdxRzRFc2M5THlsSE9OdlNKdVVUN2lnIiwiY2hlY2tfaXAiOmZhbHNlLCJ0b2tlbiI6IjQ4NzFlZTcyZDhhNGU1OThlN2U1NDBkZWM0OTJhNjQwYWQyZmU0ZTIiLCJzY29wZSI6InVzZXJfaXNzdWVkX3Rva2VuIn0.mPDj2SyGctiRI1JVXE3w6cqm-EW4A2Cd-w-J8EabAlNExGAbNfGIquKtOiP-W_kQJMBKuCnIq3SUfMALBsjo9A",
                    RefreshToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJ0b2tlbl90eXBlIjoicmVmcmVzaF90b2tlbiIsImV4cCI6MTc0MzcxMDAxNywidG9rZW4iOiI3MjU5MDdkYjVhNTlhOWUwMmZiNzdkYWE1ZTgyZTk3M2E0MWI5NmM4IiwidXNlcl9pZCI6Njk0Mzc5LCJjbGllbnRfaXAiOiI5MS4yMzcuMTc5LjE4MSIsImNoZWNrX2lwIjpmYWxzZSwic2NvcGUiOiJ1c2VyX2lzc3VlZF90b2tlbiJ9.RxHuycPFS_oSZibp4TRdajaRH_8hrI43PyCHB-Qvpgnhn6BVWhrbE5q1w60UYsfzJQzW3QYtrJ1eYS4Iat8fjQ",
                    ExpiresAt = new DateTime(2025, 03, 20, 22, 53, 37, DateTimeKind.Utc)
                });
        }
    }

}
