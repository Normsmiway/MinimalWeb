using System.Text;
using MinimalWeb.Dto;
using MinimalWeb.Entities;
using MinimalWeb.Services;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<TokenService>(new TokenService(builder.Configuration));
builder.Services.AddSingleton<IUserRepositoryService>(new UserRepositoryService());

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
builder.Services.AddSwaggerGen(c =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter JWT Bearer token **_only_**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer", // must be lower case
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
{securityScheme, Array.Empty<string>()} });
});
builder.Services.AddDbContext<BookDb>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MinimalContext"));
});

//build app
var app = builder.Build();

app.MapGet("/", () => Handler.Greet()).ExcludeFromDescription();

//Get all books
app.MapGet("/books", async (BookDb db) => await db.Books.ToListAsync())
    .Produces<List<Book>>(StatusCodes.Status200OK).WithName("GetAllBooks").WithTags("Queries");

//create new book
app.MapPost("/books",
async ([FromBody] Book addbook, [FromServices] BookDb db, HttpResponse response) =>
{
    db.Books.Add(addbook);
    await db.SaveChangesAsync();
    response.StatusCode = 200;
    response.Headers.Location = $"books/{addbook.Id}";
})
.Accepts<Book>("application/json")
.Produces<Book>(StatusCodes.Status201Created)
.WithName("AddNewBook").WithTags("Commands");

//update book
app.MapPut("/books",
[AllowAnonymous] async (int id, string title, short price, long
 isbn, int year, [FromServices] BookDb db, HttpResponse response) =>
{
    var mybook = db.Books.SingleOrDefault(s => s.Id == id);
    if (mybook == null) return Results.NotFound();
    mybook.Title = title;
    mybook.Price = price;
    mybook.ISBN = isbn;
    mybook.Year = year;

    await db.SaveChangesAsync();
    return Results.Created("/books", mybook);
})
    .Produces<Book>(StatusCodes.Status201Created).Produces(StatusCodes.Status404NotFound)
    .WithName("UpdateBook").WithTags("Commands");

//get book by Id
app.MapGet("/books/{id}", async (BookDb db, int id) =>
await db.Books.SingleOrDefaultAsync(s => s.Id == id) is Book mybook ? Results.Ok(mybook) : Results.NotFound())
    .Produces<Book>(StatusCodes.Status200OK)
    .WithName("GetBookbyId").WithTags("Queries");

//search book by title
app.MapGet("/books/search/{query}",
(string query, BookDb db) =>
{
    var _selectedBooks = db.Books.Where(x => x.Title.ToLower().Contains(query.ToLower())).ToList();
    return _selectedBooks.Count > 0 ? Results.Ok(_selectedBooks) : Results.NotFound(Array.Empty<Book>());
})
    .Produces<List<Book>>(StatusCodes.Status200OK)
    .WithName("Search").WithTags("Queries");

//get paginated book list

app.MapGet("/books/bypage", async (int pageNumber, int pageSize, BookDb db) =>
await db.Books
.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync())
    .Produces<List<Book>>(StatusCodes.Status200OK)
    .WithName("GetBooksByPage").WithTags("Queries");

//generate auth token
app.MapPost("/auth/token", [AllowAnonymous] async ([FromBodyAttribute] UserModel userModel,
TokenService tokenService, IUserRepositoryService userRepositoryService, HttpResponse response) =>
{
    var userDto = userRepositoryService.GetUser(userModel);
    if (userDto == null)
    {
        response.StatusCode = 401;
        return;
    }
    var token = tokenService.BuildToken(userDto);
    await response.WriteAsJsonAsync(new { token = token });
    return;
})
    .Produces(StatusCodes.Status200OK)
    .WithName("Login").WithTags("Accounts");

//authenticated resource
app.MapGet("/AuthorizedResource", (Func<string>)(
[Authorize] () => "Action Succeeded"))
    .Produces(StatusCodes.Status200OK)
    .WithName("Authorized").WithTags("Accounts").RequireAuthorization();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.UseAuthentication();

app.Run();



class Handler
{
    public static string Greet(string greetings = "Hello from Minimal App, .Net 6.0")
    {
        return greetings;
    }
}
