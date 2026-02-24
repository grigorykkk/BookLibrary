using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
    public DbSet<BookGenre> BookGenres => Set<BookGenre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.Property(author => author.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(author => author.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(author => author.Country)
                .HasMaxLength(100);
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.Property(genre => genre.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(genre => genre.Description)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.Property(book => book.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(book => book.ISBN)
                .HasMaxLength(13)
                .IsRequired();

            entity.Property(book => book.QuantityInStock)
                .HasDefaultValue(0);
        });

        modelBuilder.Entity<BookAuthor>(entity =>
        {
            entity.HasKey(bookAuthor => new { bookAuthor.BookId, bookAuthor.AuthorId });

            entity.HasOne(bookAuthor => bookAuthor.Book)
                .WithMany(book => book.BookAuthors)
                .HasForeignKey(bookAuthor => bookAuthor.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(bookAuthor => bookAuthor.Author)
                .WithMany(author => author.BookAuthors)
                .HasForeignKey(bookAuthor => bookAuthor.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookGenre>(entity =>
        {
            entity.HasKey(bookGenre => new { bookGenre.BookId, bookGenre.GenreId });

            entity.HasOne(bookGenre => bookGenre.Book)
                .WithMany(book => book.BookGenres)
                .HasForeignKey(bookGenre => bookGenre.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(bookGenre => bookGenre.Genre)
                .WithMany(genre => genre.BookGenres)
                .HasForeignKey(bookGenre => bookGenre.GenreId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
