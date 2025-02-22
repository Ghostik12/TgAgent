using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
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
        public DbSet<Users> Users { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseNpgsql("Host=localhost;Database=mydatabase;Username=postgres;Password=32477423");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Добавляем начальные данные
            modelBuilder.Entity<Setting>().HasData(
                new Setting { Id = 1, Key = "WelcomeMessage", Value = "Добро пожаловать!" }
            );

            modelBuilder.Entity<UserLink>().HasData(
                new UserLink { Id = 1, UserId1 = 123456789, UserId2 = 987654321, UserName1 = "unknown", UserName2 = "unknown" },
                new UserLink { Id = 2, UserId1 = 1952565657, UserId2 = 7140034389, UserName1 = "violet_myst", UserName2 = "lercky" }
            );

            modelBuilder.Entity<MessageRecord>().HasData(
                new MessageRecord
                {
                    Id = 1,
                    FromUserId = 123456789,
                    ToUserId = 987654321,
                    Text = "Привет!",
                    Timestamp = DateTime.SpecifyKind(new DateTime(2025, 02, 17, 10, 0, 0), DateTimeKind.Utc),
                    FromUsername = "pro",
                    ToUsername = "sto"// Сообщение от вчера
                },
                new MessageRecord
                {
                    Id = 2,
                    FromUserId = 987654321,
                    ToUserId = 123456789,
                    Text = "Как дела?",
                    Timestamp = DateTime.SpecifyKind(new DateTime(2023, 10, 1, 10, 0, 0), DateTimeKind.Utc),
                    FromUsername = "sto",
                    ToUsername = "pro"// Сообщение старше 7 дней
                },
                new MessageRecord
                {
                    Id = 3,
                    FromUserId = 7140034389,
                    ToUserId = 1952565657,
                    Text = "Привет!",
                    Timestamp = DateTime.SpecifyKind(new DateTime(2025, 02, 19, 10, 0, 0), DateTimeKind.Utc),
                    FromUsername = "lercky",
                    ToUsername = "violet_myst"// Сообщение от вчера
                },
                new MessageRecord
                {
                    Id = 4,
                    FromUserId = 1952565657,
                    ToUserId = 7140034389,
                    Text = "Как дела?",
                    Timestamp = DateTime.SpecifyKind(new DateTime(2025, 02, 19, 10, 1, 0), DateTimeKind.Utc),
                    FromUsername = "violet_myst",
                    ToUsername = "lercky"// Сообщение старше 7 дней
                }
            );

            modelBuilder.Entity<BlacklistWord>().HasData(
                new BlacklistWord { Id = 1, Word = "спам" }
            );

            modelBuilder.Entity<ListAdmins>().HasData(
                new ListAdmins { Id = 1, ChatId = 313064453 } // Администратор
            );

            modelBuilder.Entity<Users>().HasData(
                new Users { Id = 1, ChatId = 1952565657, Username = "violet_myst" },
                new Users { Id = 2, ChatId = 7140034389, Username = "lercky" },
                new Users { Id = 3, ChatId = 313064453, Username = "imkr1stal" }// Администратор
            );
        }
    }
}
