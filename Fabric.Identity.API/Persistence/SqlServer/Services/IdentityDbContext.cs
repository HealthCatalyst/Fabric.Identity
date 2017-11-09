using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.EntityModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Fabric.Identity.API.Persistence.SqlServer.Services
{
    public class IdentityDbContext : DbContext, IIdentityDbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
            : base(options)
        {                    
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<ApiResource> ApiResources { get; set; }
        public DbSet<IdentityResource> IdentityResources { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PersistedGrant> PersistedGrants { get; set; }

        public Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }

        private void OnSaveChanges()
        {
            var entities = base.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entityEntry in entities)
            {
                var trackableEntity = entityEntry.Entity as ITrackable;
                if(trackableEntity == null)
                {
                    continue;
                }

                if (entityEntry.State == EntityState.Added)
                {
                    trackableEntity.CreatedDateTimeUtc = DateTime.UtcNow;
                    trackableEntity.CreatedBy = "placeholder";
                }
                if (entityEntry.State == EntityState.Modified)
                {
                    trackableEntity.ModifiedDateTimeUtc = DateTime.UtcNow;
                }
            }
        }

        public override int SaveChanges()
        {
            OnSaveChanges();

            return base.SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApiClaim>(entity =>
            {
                entity.ToTable("ApiClaims");

                entity.HasIndex(e => e.ApiResourceId)
                    .HasName("IX_ApiClaims_ApiResourceId");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(d => d.ApiResource)
                    .WithMany(p => p.ApiClaims)
                    .HasForeignKey(d => d.ApiResourceId);
            });

            modelBuilder.Entity<ApiResource>(entity =>
            {
                entity.ToTable("ApiResources");

                entity.HasIndex(e => e.Name)
                    .HasName("IX_ApiResources_Name")
                    .IsUnique();

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedDateTimeUtc).HasColumnType("datetime");

                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.Property(e => e.DisplayName).HasMaxLength(200);

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                entity.Property(e => e.ModifiedDateTimeUtc).HasColumnType("datetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<ApiScopeClaim>(entity =>
            {
                entity.ToTable("ApiScopeClaims");

                entity.HasIndex(e => e.ApiScopeId)
                    .HasName("IX_ApiScopeClaims_ApiScopeId");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(d => d.ApiScope)
                    .WithMany(p => p.ApiScopeClaims)
                    .HasForeignKey(d => d.ApiScopeId);
            });

            modelBuilder.Entity<ApiScope>(entity =>
            {
                entity.ToTable("ApiScopes");

                entity.HasIndex(e => e.ApiResourceId)
                    .HasName("IX_ApiScopes_ApiResourceId");

                entity.HasIndex(e => e.Name)
                    .HasName("IX_ApiScopes_Name")
                    .IsUnique();

                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.Property(e => e.DisplayName).HasMaxLength(200);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(d => d.ApiResource)
                    .WithMany(p => p.ApiScopes)
                    .HasForeignKey(d => d.ApiResourceId);
            });

            modelBuilder.Entity<ApiSecret>(entity =>
            {
                entity.ToTable("ApiSecrets");

                entity.HasIndex(e => e.ApiResourceId)
                    .HasName("IX_ApiSecrets_ApiResourceId");

                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.Property(e => e.Type).HasMaxLength(250);

                entity.Property(e => e.Value).HasMaxLength(2000);

                entity.HasOne(d => d.ApiResource)
                    .WithMany(p => p.ApiSecrets)
                    .HasForeignKey(d => d.ApiResourceId);
            });

            modelBuilder.Entity<ClientClaim>(entity =>
            {
                entity.ToTable("ClientClaims");

                entity.HasIndex(e => e.ClientId)
                    .HasName("IX_ClientClaims_ClientId");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.HasOne(d => d.Client)
                    .WithMany(p => p.ClientClaims)
                    .HasForeignKey(d => d.ClientId);
            });

            modelBuilder.Entity<ClientCorsOrigin>(entity =>
            {
                entity.ToTable("ClientCorsOrigins");

                entity.HasIndex(e => e.ClientId)
                    .HasName("IX_ClientCorsOrigins_ClientId");

                entity.Property(e => e.Origin)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasOne(d => d.Client)
                    .WithMany(p => p.ClientCorsOrigins)
                    .HasForeignKey(d => d.ClientId);
            });

            modelBuilder.Entity<ClientGrantType>(entity =>
            {
                entity.ToTable("ClientGrantTypes");

                entity.HasIndex(e => e.ClientId)
                    .HasName("IX_ClientGrantTypes_ClientId");

                entity.Property(e => e.GrantType)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.HasOne(d => d.Client)
                    .WithMany(p => p.ClientGrantTypes)
                    .HasForeignKey(d => d.ClientId);
            });

            modelBuilder.Entity<ClientIdpRestriction>(entity =>
            {
                entity.ToTable("ClientIdPRestrictions");

                entity.HasIndex(e => e.ClientId)
                    .HasName("IX_ClientIdPRestrictions_ClientId");

                entity.Property(e => e.Provider)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(d => d.Client)
                    .WithMany(p => p.ClientIdpRestrictions)
                    .HasForeignKey(d => d.ClientId);
            });

            modelBuilder.Entity<ClientPostLogoutRedirectUri>(entity =>
            {
                entity.ToTable("ClientPostLogoutRedirectUris");

                entity.HasIndex(e => e.ClientId)
                    .HasName("IX_ClientPostLogoutRedirectUris_ClientId");

                entity.Property(e => e.PostLogoutRedirectUri)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.HasOne(d => d.Client)
                    .WithMany(p => p.ClientPostLogoutRedirectUris)
                    .HasForeignKey(d => d.ClientId);
            });

            modelBuilder.Entity<ClientRedirectUri>(entity =>
            {
                entity.ToTable("ClientRedirectUris");

                entity.HasIndex(e => e.ClientId)
                    .HasName("IX_ClientRedirectUris_ClientId");

                entity.Property(e => e.RedirectUri)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.HasOne(d => d.Client)
                    .WithMany(p => p.ClientRedirectUris)
                    .HasForeignKey(d => d.ClientId);
            });

            modelBuilder.Entity<ClientScope>(entity =>
            {
                entity.ToTable("ClientScopes");

                entity.HasIndex(e => e.ClientId)
                    .HasName("IX_ClientScopes_ClientId");

                entity.Property(e => e.Scope)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(d => d.Client)
                    .WithMany(p => p.ClientScopes)
                    .HasForeignKey(d => d.ClientId);
            });

            modelBuilder.Entity<ClientSecret>(entity =>
            {
                entity.ToTable("ClientSecrets");

                entity.HasIndex(e => e.ClientId)
                    .HasName("IX_ClientSecrets_ClientId");

                entity.Property(e => e.Description).HasMaxLength(2000);

                entity.Property(e => e.Type).HasMaxLength(250);

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.HasOne(d => d.Client)
                    .WithMany(p => p.ClientSecrets)
                    .HasForeignKey(d => d.ClientId);
            });

            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clients");

                entity.HasIndex(e => e.ClientId)
                    .HasName("IX_Clients_ClientId")
                    .IsUnique();

                entity.Property(e => e.ClientId)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.ClientName).HasMaxLength(200);

                entity.Property(e => e.ClientUri).HasMaxLength(2000);

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedDateTimeUtc).HasColumnType("datetime");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.Property(e => e.LogoUri).HasMaxLength(2000);

                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                entity.Property(e => e.ModifiedDateTimeUtc).HasColumnType("datetime");

                entity.Property(e => e.ProtocolType)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<IdentityClaim>(entity =>
            {
                entity.ToTable("IdentityClaimss");

                entity.HasIndex(e => e.IdentityResourceId)
                    .HasName("IX_IdentityClaims_IdentityResourceId");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(d => d.IdentityResource)
                    .WithMany(p => p.IdentityClaims)
                    .HasForeignKey(d => d.IdentityResourceId);
            });

            modelBuilder.Entity<IdentityResource>(entity =>
            {
                entity.ToTable("IdentityResources");

                entity.HasIndex(e => e.Name)
                    .HasName("IX_IdentityResources_Name")
                    .IsUnique();

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedDateTimeUtc).HasColumnType("datetime");

                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.Property(e => e.DisplayName).HasMaxLength(200);

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                entity.Property(e => e.ModifiedDateTimeUtc).HasColumnType("datetime");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<UserLogin>(entity =>
            {
                entity.ToTable("UserLogins");

                entity.Property(e => e.LoginDate).HasColumnType("datetime");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserLogins)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_UserLogins_Users_Id");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedDateTimeUtc).HasColumnType("datetime");

                entity.Property(e => e.FirstName).HasMaxLength(200);

                entity.Property(e => e.LastName).HasMaxLength(200);

                entity.Property(e => e.MiddleName).HasMaxLength(200);

                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                entity.Property(e => e.ModifiedDateTimeUtc).HasColumnType("datetime");

                entity.Property(e => e.ProviderName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.SubjectId)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(200);
            });

            modelBuilder.Entity<PersistedGrant>(grant =>
            {
                grant.ToTable("PersistedGrants");

                grant.Property(x => x.Key).HasMaxLength(200).ValueGeneratedNever();
                grant.Property(x => x.Type).HasMaxLength(50).IsRequired();
                grant.Property(x => x.SubjectId).HasMaxLength(200);
                grant.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
                grant.Property(x => x.CreationTime).IsRequired();
                grant.Property(x => x.Data).HasMaxLength(50000).IsRequired();

                grant.HasKey(x => x.Key);

                grant.HasIndex(x => new { x.SubjectId, x.ClientId, x.Type });
            });
        }       
    }
}
