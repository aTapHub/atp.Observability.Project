// --- TOP-LEVEL STATEMENTS START HERE ---
using Microsoft.AspNetCore.Mvc; // Added for the [FromBody] attribute, good practice
using MySqlConnector;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// The connection string is set up to connect to the 'db' service defined in docker-compose.yml
string password = File.ReadAllText("/run/secrets/db-password");
string connectionString = $"server=db;user=root;database=example;port=3306;password={password}";

// Register the connection for Dependency Injection (DI)
builder.Services.AddTransient<MySqlConnection>((_provider) => new MySqlConnection(connectionString));


// --- ADD THE CODE STARTING HERE ---
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("atp-backend")) // Add this
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://tempo:4318/v1/traces"); // Ensure /v1/traces is here
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }));
// --- END OF OPEN TELEMETRY CODE ---



var app = builder.Build();

// Prometheus setup
// capture https metrics like RequestPerSeond, Latency, Status Codes

app.UseHttpMetrics();

// --- API ENDPOINT DEFINITIONS (CRUD) ---

// READ ALL (GET /)
app.MapGet("/", async (MySqlConnection connection) =>
{
    var posts = new List<BlogPost>();
    try
    {
        await connection.OpenAsync();
        string sql = "SELECT id, title FROM blog";
        using var cmd = new MySqlCommand(sql, connection);
        using MySqlDataReader reader = await cmd.ExecuteReaderAsync();

        while (reader.Read())
        {
            posts.Add(new BlogPost(reader.GetInt32("id"), reader.GetString("title")));
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.ToString());
    }
    finally
    {
        await connection.CloseAsync();
    }
    return Results.Ok(posts);
}).WithTags("Read");

// READ SINGLE (GET /blog/{id})
app.MapGet("/blog/{id:int}", async (int id, MySqlConnection connection) =>
{
    try
    {
        await connection.OpenAsync();
        string sql = "SELECT id, title FROM blog WHERE id = @Id";
        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        using MySqlDataReader reader = await cmd.ExecuteReaderAsync();

        if (reader.Read())
        {
            var post = new BlogPost(reader.GetInt32("id"), reader.GetString("title"));
            return Results.Ok(post);
        }
        else
        {
            return Results.NotFound();
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.ToString());
    }
    finally
    {
        await connection.CloseAsync();
    }
}).WithTags("Read");

// CREATE (POST /blog)
app.MapPost("/blog", async ([FromBody] BlogPost newPost, MySqlConnection connection) =>
{
    try
    {
        await connection.OpenAsync();
        // Use a SQL transaction to ensure both insert and ID retrieval succeed
        string sql = "INSERT INTO blog (title) VALUES (@Title); SELECT LAST_INSERT_ID();";
        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Title", newPost.Title);

        // ExecuteScalar returns the ID from the LAST_INSERT_ID()
        ulong newId = (ulong)await cmd.ExecuteScalarAsync();

        return Results.Created($"/blog/{newId}", newPost with { Id = (int)newId });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.ToString());
    }
    finally
    {
        await connection.CloseAsync();
    }
}).WithTags("Write");

// UPDATE (PUT /blog/{id})
app.MapPut("/blog/{id:int}", async (int id, [FromBody] BlogPost updatedPost, MySqlConnection connection) =>
{
    try
    {
        await connection.OpenAsync();
        string sql = "UPDATE blog SET title = @Title WHERE id = @Id";
        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Title", updatedPost.Title);
        cmd.Parameters.AddWithValue("@Id", id);

        int rows = await cmd.ExecuteNonQueryAsync();

        return rows > 0 ? Results.NoContent() : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.ToString());
    }
    finally
    {
        await connection.CloseAsync();
    }
}).WithTags("Write");

// DELETE (DELETE /blog/{id})
app.MapDelete("/blog/{id:int}", async (int id, MySqlConnection connection) =>
{
    try
    {
        await connection.OpenAsync();
        string sql = "DELETE FROM blog WHERE id = @Id";
        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);

        int rows = await cmd.ExecuteNonQueryAsync();

        return rows > 0 ? Results.NoContent() : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.ToString());
    }
    finally
    {
        await connection.CloseAsync();
    }
}).WithTags("Write");


// Prometheus setup
// this creates the /metrics page that Prometheus will scrape
app.MapMetrics();


// --- APPLICATION PREPARATION & RUN ---
Prepare(connectionString);

app.Run();

// --- TYPE AND METHOD DEFINITIONS START HERE ---

// 2. Local function definition (The C# compiler allows this after the top-level statements)
void Prepare(string connectionString)
{
    using MySqlConnection connection = new MySqlConnection(connectionString);
    connection.Open();
    using var transation = connection.BeginTransaction();
    using MySqlCommand cmd1 = new MySqlCommand("DROP TABLE IF EXISTS blog", connection, transation);
    cmd1.ExecuteNonQuery();
    using MySqlCommand cmd2 = new MySqlCommand("CREATE TABLE IF NOT EXISTS blog (id int NOT NULL AUTO_INCREMENT, title varchar(255), PRIMARY KEY (id))", connection, transation);
    cmd2.ExecuteNonQuery();
    for (int i = 0; i < 5; i++)
    {
        using MySqlCommand insertCommand = new MySqlCommand($"INSERT INTO blog (title) VALUES ('Blog post #{i}');", connection, transation);
        insertCommand.ExecuteNonQuery();
    }
    transation.Commit();
    connection.Close();
}

// 3. Type definition (The C# compiler allows this after the top-level statements)
public record BlogPost(int Id, string Title);