using LudoServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LudoServer.Data
{
    public class LudoDbContext : DbContext
    {
        public LudoDbContext(DbContextOptions<LudoDbContext> options) : base(options) { }
        public DbSet<PlayerWallet> PlayerWallet { get; set; }
        public DbSet<WalletTransaction> WalletTransaction { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<MultiPlayer> MultiPlayers { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<TournamentChallenger> TournamentChallengers { get; set; }
        public DbSet<DailyBonus> DailyBonus { get; set; }
        public DbSet<FriendRequest> FriendsRequests { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<PlayerWalletKey> PlayerWalletKey { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tournament>()
                .HasMany(t => t.Games)
                .WithOne(g => g.Tournament)
                .HasForeignKey(g => g.TournamentId);

            modelBuilder.Entity<Tournament>()
                .HasMany(t => t.TournamentChallengers)
                .WithOne(tc => tc.Tournament)
                .HasForeignKey(tc => tc.TournamentId);

            modelBuilder.Entity<Player>()
                .HasMany(p => p.TournamentChallengers)
                .WithOne(tc => tc.Player)
                .HasForeignKey(tc => tc.PlayerId);

            modelBuilder.Entity<Player>()
                .HasMany(p => p.DailyBonus)
                .WithOne(a => a.Player)
                .HasForeignKey(a => a.PlayerId);

      

            // Self-referencing many-to-many relationship for Friend Requests for Sender
            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany(p => p.SentFriendRequests)
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents cascading deletes

            // Self-referencing many-to-many relationship for Friend Requests for Receiver
            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany(p => p.ReceivedFriendRequests)
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents cascading deletes
          
            modelBuilder.Entity<WalletTransaction>()
                .Property(b => b.CreatedDate)
                .HasDefaultValueSql("getutcdate()");
           
            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(t => t.OperationId)
                .IsUnique();

            modelBuilder.Entity<PlayerWallet>()
                .Property(b => b.CreatedDate)
                .HasDefaultValueSql("getutcdate()");

            modelBuilder.Entity<MultiPlayer>()
                .HasKey(mp => mp.MultiPlayerId); // Ensure it's the primary key

            modelBuilder.Entity<MultiPlayer>()
                .Property(mp => mp.MultiPlayerId)
                .ValueGeneratedOnAdd(); // Tells EF Core to use auto-increment
            
            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(t => new { t.PlayerId, t.CreatedDate });

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(t => t.Status);

            // Configure ChatMessageEntity (table name is set via [Table("ChatMessages")] on the model)
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Index);
                entity.Property(e => e.SenderName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ReceiverName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.CreatedDate).IsRequired();
                // (PlayerPicture and ReceiverPicture are optional strings)
            });

            //modelBuilder.Entity<MultiPlayer>()
            //    .HasKey(m => m.Id); // Set Id as primary key

            //modelBuilder.Entity<MultiPlayer>()
            //    .Property(m => m.Id)
            //    .ValueGeneratedOnAdd(); // Ensures auto-increment

            modelBuilder.Entity<PlayerWalletKey>(entity =>
            {
                entity.HasKey(e => new { e.PlayerId, e.PublicKey });

                entity.Property(e => e.PublicKey)
                      .IsRequired();

                entity.Property(e => e.EncryptedPrivateKey)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("getutcdate()");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
