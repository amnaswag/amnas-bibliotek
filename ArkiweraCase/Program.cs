using System.Xml.Linq;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// gör att index.html visas som startsida
app.UseDefaultFiles();
app.UseStaticFiles();

// skapar en endpoint för att hämta och filtrera böcker
app.MapGet("/api/books", (HttpContext context) =>
{
    var query = context.Request.Query;
    
    // hämtar sökfilter och gör om till gemener
    var title = query["title"].ToString().ToLower();
    var author = query["author"].ToString().ToLower();
    var genre = query["genre"].ToString().ToLower();
    var date = query["date"].ToString();
    var minP = query["minPrice"].ToString();
    var maxP = query["maxPrice"].ToString();

    // laddar xml-filen från projektets rotmapp
    var path = Path.Combine(app.Environment.ContentRootPath, "books.xml");
    if (!File.Exists(path)) return Results.NotFound("xml-filen saknas");
    
    var xDoc = XDocument.Load(path);

    // mappar om xml-data till ett läsbart objekt
    var books = xDoc.Descendants("book").Select(b => new
    {
        id = b.Attribute("id")?.Value,
        author = b.Element("author")?.Value,
        title = b.Element("title")?.Value,
        genre = b.Element("genre")?.Value,
        price = b.Element("price")?.Value,
        publishDate = b.Element("publish_date")?.Value,
        description = b.Element("description")?.Value
    });

    // filtrerar resultatet baserat på sökningen
    var filtered = books.Where(b =>
    {
        var matches = true;
        if (!string.IsNullOrEmpty(title) && !(b.title ?? "").ToLower().Contains(title)) matches = false;
        if (!string.IsNullOrEmpty(author) && !(b.author ?? "").ToLower().Contains(author)) matches = false;
        if (!string.IsNullOrEmpty(genre) && !(b.genre ?? "").ToLower().Contains(genre)) matches = false;
        if (!string.IsNullOrEmpty(date) && !(b.publishDate ?? "").ToLower().Contains(date)) matches = false;

        // hanterar priskonvertering och jämförelse endast om filter finns
        if (decimal.TryParse(minP, CultureInfo.InvariantCulture, out var min) || 
            decimal.TryParse(maxP, CultureInfo.InvariantCulture, out var max))
        {
            decimal.TryParse(b.price, CultureInfo.InvariantCulture, out var p);
            if (!string.IsNullOrEmpty(minP) && decimal.TryParse(minP, CultureInfo.InvariantCulture, out var m) && p < m) matches = false;
            if (!string.IsNullOrEmpty(maxP) && decimal.TryParse(maxP, CultureInfo.InvariantCulture, out var mx) && p > mx) matches = false;
        }

        return matches;
    });

    // skickar tillbaka listan som json
    return Results.Ok(filtered);
});

app.Run();