using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add HttpClient for making external API calls
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Root endpoint - simple health check
app.MapGet("/", () => Results.Ok(new
{
    Message = "Traffic Capture Demo API",
    Timestamp = DateTime.UtcNow
}));

// Endpoint that calls an external API
app.MapGet("/api/user/{id}", async (int id, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();

    try
    {
        // Call JSONPlaceholder API (free public test API)
        var userResponse = await client.GetStringAsync(
            $"https://jsonplaceholder.typicode.com/users/{id}");

        var user = JsonSerializer.Deserialize<JsonElement>(userResponse);

        // Call another endpoint to get user's posts
        var postsResponse = await client.GetStringAsync(
            $"https://jsonplaceholder.typicode.com/posts?userId={id}");

        var posts = JsonSerializer.Deserialize<JsonElement>(postsResponse);

        return Results.Ok(new
        {
            Message = "Data retrieved successfully",
            User = user,
            Posts = posts,
            Timestamp = DateTime.UtcNow
        });
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem($"Error calling external API: {ex.Message}");
    }
});

// Endpoint that demonstrates POST to external API
app.MapPost("/api/create-post", async (CreatePostRequest request, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient();

    try
    {
        var postData = new
        {
            title = request.Title,
            body = request.Body,
            userId = request.UserId
        };

        var jsonContent = JsonContent.Create(postData);

        var response = await client.PostAsync(
            "https://jsonplaceholder.typicode.com/posts",
            jsonContent);

        var responseContent = await response.Content.ReadAsStringAsync();

        return Results.Ok(new
        {
            Message = "Post created successfully",
            StatusCode = (int)response.StatusCode,
            Response = JsonSerializer.Deserialize<JsonElement>(responseContent)
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}");
    }
});

app.Run("https://localhost:5001");

// Request model for POST endpoint
record CreatePostRequest(string Title, string Body, int UserId);