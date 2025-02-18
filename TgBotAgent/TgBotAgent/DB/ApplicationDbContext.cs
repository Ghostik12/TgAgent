using Microsoft.EntityFrameworkCore;
using TgBotAgent.Models;

namespace TgBotAgent.DB
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Setting> Settings { get; set; }
        public DbSet<UserLink> UserLinks { get; set; }
        public DbSet<MessageRecord> Messages { get; set; }
        public DbSet<BlacklistWord> BlacklistWords { get; set; }
        public DbSet<ListAdmins> Admins { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Подключение к PostgreSQL
            optionsBuilder.UseNpgsql("Host=localhost;Database=tgagent;Username=postgres;Password=12345Ob@");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Добавляем начальные данные
            modelBuilder.Entity<Setting>().HasData(
                new Setting { Id = 1, Key = "WelcomeMessage", Value = "Добро пожаловать!" }
            );

            modelBuilder.Entity<UserLink>().HasData(
                new UserLink { Id = 1, UserId1 = 123456789, UserId2 = 987654321 }
            );

            modelBuilder.Entity<MessageRecord>().HasData(
                new MessageRecord
                {
                    Id = 1,
                    FromUserId = 123456789,
                    ToUserId = 987654321,
                    Text = "Привет!",
                    Timestamp = DateTime.SpecifyKind(new DateTime(2025, 02, 17, 10, 0, 0), DateTimeKind.Utc) // Сообщение от вчера
                },
                new MessageRecord
                {
                    Id = 2,
                    FromUserId = 987654321,
                    ToUserId = 123456789,
                    Text = "Как дела?",
                    Timestamp = DateTime.SpecifyKind(new DateTime(2023, 10, 1, 10, 0, 0), DateTimeKind.Utc) // Сообщение старше 7 дней
                }
            );

            modelBuilder.Entity<BlacklistWord>().HasData(
                new BlacklistWord { Id = 1, Word = "спам" }
            );

            modelBuilder.Entity<ListAdmins>().HasData(
                new ListAdmins { Id = 1, ChatId = 1451999567 } // Администратор
            );
        }
    }
}
