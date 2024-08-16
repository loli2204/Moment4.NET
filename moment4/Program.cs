using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Lägg till SongDb som singleton
builder.Services.AddSingleton<SongDb>();

var app = builder.Build();

// Endpoint för att visa välkomstmeddelande
app.MapGet("/", () => "Welcome to the Song API!");

// Endpoint för att hämta alla låtar
app.MapGet("/songs", async (SongDb db) =>
{
    var songs = await db.GetSongs();
    return songs;
});

// Endpoint för att lägga till en ny låt
app.MapPost("/songs", async (Song song, SongDb db) =>
{
    await db.AddSong(song);
    return Results.Created($"/songs/{song.Id}", song);
});

// Endpoint för att uppdatera en befintlig låt
app.MapPut("/songs/{id}", async (string id, Song updatedSong, SongDb db) =>
{
    var result = await db.UpdateSong(id, updatedSong);
    if (result == null)
    {
        return Results.NotFound($"Song with id {id} not found.");
    }

    return Results.Ok(result);
});

// Endpoint för att ta bort en låt
app.MapDelete("/songs/{id}", async (string id, SongDb db) =>
{
    var result = await db.DeleteSong(id);
    if (!result)
    {
        return Results.NotFound($"Song with id {id} not found.");
    }

    return Results.Ok();
});

app.Run();

// Klass för Song
public class Song
{
    public ObjectId Id { get; set; }
    public string Artist { get; set; }
    public string Title { get; set; }
    public int LengthInSeconds { get; set; }
    public string Category { get; set; }
}

public class SongDb
{
    private readonly IMongoCollection<Song> _songs;

    public SongDb()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("SongDb");
        _songs = database.GetCollection<Song>("Songs");
    }

    public async Task<List<Song>> GetSongs()
    {
        return await _songs.Find(_ => true).ToListAsync();
    }

    public async Task AddSong(Song song)
    {
        await _songs.InsertOneAsync(song);
    }

    public async Task<Song> UpdateSong(string id, Song updatedSong)
    {
        var objectId = ObjectId.Parse(id);
        var filter = Builders<Song>.Filter.Eq(s => s.Id, objectId);
        var update = Builders<Song>.Update
            .Set(s => s.Artist, updatedSong.Artist)
            .Set(s => s.Title, updatedSong.Title)
            .Set(s => s.LengthInSeconds, updatedSong.LengthInSeconds)
            .Set(s => s.Category, updatedSong.Category);

        var options = new FindOneAndUpdateOptions<Song>
        {
            ReturnDocument = ReturnDocument.After
        };

        return await _songs.FindOneAndUpdateAsync(filter, update, options);
    }

    public async Task<bool> DeleteSong(string id)
    {
        var objectId = ObjectId.Parse(id);
        var result = await _songs.DeleteOneAsync(s => s.Id == objectId);
        return result.DeletedCount > 0;
    }
}
