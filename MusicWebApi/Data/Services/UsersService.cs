using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MusicWebApi.Data;
using MusicWebApi.Data.Models;

namespace MusicWebApi.Data.Services;

public class UserSerchOptions
{
    public string? Id { get; set; }

    public string? Email { get; set; }
}

public class UsersService
{
    private readonly IMongoCollection<UserDB> _usersCollection;

    public UsersService(
        IOptions<DatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(
            databaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            databaseSettings.Value.DatabaseName);

        _usersCollection = mongoDatabase.GetCollection<UserDB>(
            databaseSettings.Value.UsersCollectionName);
    }

    public async Task<UserDB?> GetAsync(UserSerchOptions search)
    {
        if (search.Id == null && search.Email == null)
            return null;

        return await _usersCollection.Find(x => 
        search.Id == null || search.Id != null && x.Id == search.Id || 
        search.Id == null || search.Email != null && x.Email == search.Email)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(UserDB newUser) =>
        await _usersCollection.InsertOneAsync(newUser);

    public async Task UpdateAsync(string id, UserDB updatedUser) =>
        await _usersCollection.ReplaceOneAsync(x => x.Id == id, updatedUser);

    public async Task RemoveAsync(string id) =>
        await _usersCollection.DeleteOneAsync(x => x.Id == id);

    public async Task AddToken(string id, string token) =>
        await _usersCollection.UpdateOneAsync(
            x => x.Id == id,
            Builders<UserDB>.Update.Push(x => x.RefreshToken, token));
}

