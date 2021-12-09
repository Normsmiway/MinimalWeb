using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MinimalWeb.Entities
{
    public class Book
    {
        [Key]
        public int Id { get; set; }
        public string? Title { get; set; }
        public int Year { get; set; }
        public long ISBN { get; set; }
        public DateTime? PublishedDate { get; set; }
        public short Price { get; set; }
        public int AuthorId { get; set; }

    }


    public class BookDb : DbContext
    {
        public BookDb(DbContextOptions<BookDb> options) : base(options) { }
        public DbSet<Book> Books { get; set;}
    }
}
