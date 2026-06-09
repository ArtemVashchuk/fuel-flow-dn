using FuelFlow.Features.Auth.SharedModels;
using FuelFlow.Features.Vouchers;
using FuelFlow.Features.Vouchers.Import;
using Microsoft.EntityFrameworkCore;

namespace FuelFlow.Persistence;

public sealed class ApplicationDbContext : DbContext, IImportVouchersDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<FuelVoucher> FuelVouchers => Set<FuelVoucher>();
    public DbSet<VoucherImport> VoucherImports => Set<VoucherImport>();
    public DbSet<VoucherImportError> VoucherImportErrors => Set<VoucherImportError>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FuelVoucher>(entity =>
        {
            entity.ToTable("fuel_vouchers");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Provider)
                .HasColumnName("provider")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.FuelType)
                .HasColumnName("fuel_type")
                .HasMaxLength(50)
                .HasConversion<string>()
                .IsRequired();

            entity.Property(e => e.Liters)
                .HasColumnName("liters")
                .HasColumnType("numeric(10,2)")
                .IsRequired();

            entity.Property(e => e.ExpirationDate)
                .HasColumnName("expiration_date")
                .IsRequired();

            entity.Property(e => e.VoucherNumber)
                .HasColumnName("voucher_number")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.QrPayload)
                .HasColumnName("qr_payload")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            entity.HasIndex(e => e.VoucherNumber)
                .IsUnique();

            entity.HasIndex(e => e.QrPayload)
                .IsUnique();

            entity.HasIndex(e => e.ExpirationDate);
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.FuelType);
        });

        modelBuilder.Entity<VoucherImport>(entity =>
        {
            entity.ToTable("voucher_imports");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.FileName)
                .HasColumnName("file_name")
                .IsRequired();

            entity.Property(e => e.PageCount)
                .HasColumnName("page_count")
                .IsRequired();

            entity.Property(e => e.StartedAtUtc)
                .HasColumnName("started_at_utc")
                .IsRequired();

            entity.Property(e => e.CompletedAtUtc)
                .HasColumnName("completed_at_utc");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .IsRequired();

            entity.Property(e => e.ImportedCount)
                .HasColumnName("imported_count")
                .IsRequired();

            entity.Property(e => e.DuplicateCount)
                .HasColumnName("duplicate_count")
                .IsRequired();

            entity.Property(e => e.FailedCount)
                .HasColumnName("failed_count")
                .IsRequired();
        });

        modelBuilder.Entity<VoucherImportError>(entity =>
        {
            entity.ToTable("voucher_import_errors");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.ImportId)
                .HasColumnName("import_id")
                .IsRequired();

            entity.Property(e => e.PageNumber)
                .HasColumnName("page_number")
                .IsRequired();

            entity.Property(e => e.VoucherNumber)
                .HasColumnName("voucher_number")
                .HasMaxLength(100);

            entity.Property(e => e.ErrorMessage)
                .HasColumnName("error_message")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.RawText)
                .HasColumnName("raw_text")
                .HasColumnType("text");

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            entity.HasOne<VoucherImport>()
                .WithMany()
                .HasForeignKey(d => d.ImportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.PhoneNumber)
                .HasColumnName("phone_number")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.RoleId)
                .HasColumnName("role_id");

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            entity.Property(e => e.LastLoginAtUtc)
                .HasColumnName("last_login_at_utc");

            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.PhoneNumber)
                .IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(e => e.Token)
                .HasColumnName("token")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.ExpiresAtUtc)
                .HasColumnName("expires_at_utc")
                .IsRequired();

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            entity.Property(e => e.IsRevoked)
                .HasColumnName("is_revoked")
                .IsRequired();

            entity.Property(e => e.RevokedAtUtc)
                .HasColumnName("revoked_at_utc");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token)
                .IsUnique();

            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<VerificationCode>(entity =>
        {
            entity.ToTable("verification_codes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.PhoneNumber)
                .HasColumnName("phone_number")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Code)
                .HasColumnName("code")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.ExpiresAtUtc)
                .HasColumnName("expires_at_utc")
                .IsRequired();

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            entity.Property(e => e.IsUsed)
                .HasColumnName("is_used")
                .IsRequired();

            entity.Property(e => e.UsedAtUtc)
                .HasColumnName("used_at_utc");

            entity.HasIndex(e => e.PhoneNumber);
            entity.HasIndex(e => e.ExpiresAtUtc);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
