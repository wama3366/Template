using LibraryTemplate.Domain;
using LibraryTemplate.Domain.Aggregates.Books;
using LibraryTemplate.Domain.Aggregates.Branches;
using LibraryTemplate.Domain.Aggregates.Loans;
using LibraryTemplate.Domain.Aggregates.Users;
using LibraryTemplate.Persistence.Rows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LibraryTemplate.Persistence;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    private static readonly ValueConverter<AuthorId, Guid> AuthorIdConverter = new(id => id.Value, value => new AuthorId(value));
    private static readonly ValueConverter<BookId, Guid> BookIdConverter = new(id => id.Value, value => new BookId(value));
    private static readonly ValueConverter<BranchId, Guid> BranchIdConverter = new(id => id.Value, value => new BranchId(value));
    private static readonly ValueConverter<LoanId, Guid> LoanIdConverter = new(id => id.Value, value => new LoanId(value));
    private static readonly ValueConverter<LoanLineId, Guid> LoanLineIdConverter = new(id => id.Value, value => new LoanLineId(value));
    private static readonly ValueConverter<UserId, Guid> UserIdConverter = new(id => id.Value, value => new UserId(value));

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<BookAuthorRow> BookAuthors => Set<BookAuthorRow>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanLine> LoanLines => Set<LoanLine>();
    public DbSet<User> Users => Set<User>();
    public DbSet<BorrowerProfile> BorrowerProfiles => Set<BorrowerProfile>();
    public DbSet<RoleRow> Roles => Set<RoleRow>();
    public DbSet<RoleAssignmentRow> RoleAssignments => Set<RoleAssignmentRow>();
    public DbSet<UserBranchRow> UserBranches => Set<UserBranchRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureBranches(modelBuilder);
        ConfigureBooks(modelBuilder);
        ConfigureLoans(modelBuilder);
        ConfigureSecurity(modelBuilder);
    }

    private static void ConfigureBranches(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(branch =>
        {
            branch.ToTable("branches");
            branch.HasKey(entity => entity.Id);
            branch.Property(entity => entity.Id).HasConversion(BranchIdConverter).ValueGeneratedNever();
            branch.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
            branch.Property(entity => entity.IsDeleted).HasDefaultValue(false);
            branch.Property(entity => entity.DeletedReason).HasMaxLength(500);
            branch.Ignore(entity => entity.DomainEvents);
            branch.HasIndex(entity => entity.Name).IsUnique();
            branch.HasIndex(entity => entity.IsDeleted);
        });
    }

    private static void ConfigureBooks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(book =>
        {
            book.ToTable("books");
            book.HasKey(entity => entity.Id);
            book.Property(entity => entity.Id).HasConversion(BookIdConverter).ValueGeneratedNever();
            book.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
            book.Property(entity => entity.Isbn)
                .HasConversion(isbn => isbn.Value, value => Isbn.Create(value))
                .HasColumnName("isbn")
                .HasMaxLength(13)
                .IsRequired();
            book.Property(entity => entity.AgeRestriction)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            book.Property(entity => entity.IsBlocked).HasDefaultValue(false);
            book.Property(entity => entity.BlockReason).HasMaxLength(500);
            book.Ignore(entity => entity.DomainEvents);
            book.HasIndex(entity => entity.Isbn).IsUnique();
        });

        modelBuilder.Entity<Author>(author =>
        {
            author.ToTable("authors");
            author.HasKey(entity => entity.Id);
            author.Property(entity => entity.Id).HasConversion(AuthorIdConverter).ValueGeneratedNever();
            author.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
            author.Ignore(entity => entity.DomainEvents);
            author.HasIndex(entity => entity.Name).IsUnique();
        });

        modelBuilder.Entity<BookAuthorRow>(bookAuthor =>
        {
            bookAuthor.ToTable("book_authors");
            bookAuthor.HasKey(entity => new { entity.BookId, entity.AuthorId });
            bookAuthor.Property(entity => entity.BookId).HasConversion(BookIdConverter);
            bookAuthor.Property(entity => entity.AuthorId).HasConversion(AuthorIdConverter);
            bookAuthor.HasOne<Book>()
                .WithMany()
                .HasForeignKey(entity => entity.BookId);
            bookAuthor.HasOne<Author>()
                .WithMany()
                .HasForeignKey(entity => entity.AuthorId);
        });
    }

    private static void ConfigureLoans(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Loan>(loan =>
        {
            loan.ToTable("loans");
            loan.HasKey(entity => entity.Id);
            loan.Property(entity => entity.Id).HasConversion(LoanIdConverter).ValueGeneratedNever();
            loan.Property(entity => entity.BranchId).HasConversion(BranchIdConverter).IsRequired();
            loan.Property(entity => entity.BorrowerId).HasConversion(UserIdConverter).IsRequired();
            loan.Property(entity => entity.LoanNumber)
                .HasConversion(loanNumber => loanNumber.Value, value => LoanNumber.Create(value))
                .HasColumnName("loan_number")
                .HasMaxLength(64)
                .IsRequired();
            loan.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            loan.Ignore(entity => entity.DomainEvents);
            loan.HasIndex(entity => entity.BranchId);
            loan.HasIndex(entity => entity.BorrowerId);
            loan.HasIndex(entity => entity.LoanNumber).IsUnique();
            loan.HasMany(entity => entity.Lines)
                .WithOne()
                .HasForeignKey(entity => entity.LoanId)
                .OnDelete(DeleteBehavior.Cascade);
            loan.Navigation(entity => entity.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<LoanLine>(line =>
        {
            line.ToTable("loan_lines");
            line.HasKey(entity => entity.Id);
            line.Property(entity => entity.Id).HasConversion(LoanLineIdConverter).ValueGeneratedNever();
            line.Property(entity => entity.LoanId).HasConversion(LoanIdConverter).IsRequired();
            line.Property(entity => entity.BookId).HasConversion(BookIdConverter).IsRequired();
            line.Property(entity => entity.BookTitle).HasMaxLength(200).IsRequired();
            line.HasIndex(entity => entity.BookId);
            line.HasIndex(entity => entity.DueOn);
        });
    }

    private static void ConfigureSecurity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("app_users");
            user.HasKey(entity => entity.Id);
            user.Property(entity => entity.Id).HasConversion(UserIdConverter).ValueGeneratedNever();
            user.Property(entity => entity.ExternalUserId)
                .HasColumnName("Auth0Subject")
                .HasMaxLength(200)
                .IsRequired();
            user.Property(entity => entity.DisplayName).HasMaxLength(200).IsRequired();
            user.Property(entity => entity.Email).HasMaxLength(320).IsRequired();
            user.Ignore(entity => entity.DomainEvents);
            user.Ignore(entity => entity.Roles);
            user.HasIndex(entity => entity.ExternalUserId)
                .HasDatabaseName("IX_app_users_Auth0Subject")
                .IsUnique();
        });

        modelBuilder.Entity<BorrowerProfile>(profile =>
        {
            profile.ToTable("borrower_profiles");
            profile.HasKey(entity => entity.UserId);
            profile.Property(entity => entity.UserId)
                .HasColumnName("AppUserId")
                .HasConversion(UserIdConverter)
                .ValueGeneratedNever();
            profile.Property(entity => entity.Category)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            profile.HasOne<User>()
                .WithOne()
                .HasForeignKey<BorrowerProfile>(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleRow>(role =>
        {
            role.ToTable("roles");
            role.HasKey(entity => entity.Name);
            role.Property(entity => entity.Name)
                .HasConversion<string>()
                .HasMaxLength(100);
            role.Property(entity => entity.Description).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<RoleAssignmentRow>(assignment =>
        {
            assignment.ToTable("role_assignments");
            assignment.HasKey(entity => entity.Id);
            assignment.Property(entity => entity.Id).ValueGeneratedNever();
            assignment.Property(entity => entity.UserId).HasConversion(UserIdConverter).IsRequired();
            assignment.Property(entity => entity.Role)
                .HasColumnName("RoleName")
                .HasConversion<string>()
                .HasMaxLength(100)
                .IsRequired();
            assignment.HasIndex(entity => entity.Role)
                .HasDatabaseName("IX_role_assignments_RoleName");
            assignment.HasIndex(entity => new { entity.UserId, entity.Role })
                .HasDatabaseName("IX_role_assignments_UserId_RoleName")
                .IsUnique();
            assignment.HasOne<User>()
                .WithMany()
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            assignment.HasOne<RoleRow>()
                .WithMany()
                .HasForeignKey(entity => entity.Role)
                .HasConstraintName("FK_role_assignments_roles_RoleName");
        });

        modelBuilder.Entity<UserBranchRow>(userBranch =>
        {
            userBranch.ToTable("user_branches");
            userBranch.HasKey(entity => new { entity.UserId, entity.BranchId });
            userBranch.Property(entity => entity.UserId).HasConversion(UserIdConverter);
            userBranch.Property(entity => entity.BranchId).HasConversion(BranchIdConverter);
            userBranch.HasIndex(entity => entity.BranchId);
            userBranch.HasOne<User>()
                .WithMany()
                .HasForeignKey(entity => entity.UserId);
            userBranch.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(entity => entity.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

}
