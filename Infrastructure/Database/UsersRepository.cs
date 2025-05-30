using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Domain.Entities;
using Domain.Options;

namespace Infrastructure.Datasbase;

public class UserSerchOptions
{
    public string? Id { get; set; }

    public string? Email { get; set; }
}

public class UsersRepository
{
    private readonly IMongoCollection<UserDB> _usersCollection;

    public UsersRepository(
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
            (search.Id == null || x.Id == search.Id) &&
            (search.Email == null || x.Email == search.Email))
            .FirstOrDefaultAsync();
    }

    public async Task<UserDB?> GetByIdAsync(string id)
    {
        return await _usersCollection.Find(x =>x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(UserDB newUser) =>
        await _usersCollection.InsertOneAsync(newUser);

    public async Task UpdateAsync(string id, UserDB updatedUser) =>
        await _usersCollection.ReplaceOneAsync(x => x.Id == id, updatedUser);

    public async Task RemoveAsync(string id) =>
        await _usersCollection.DeleteOneAsync(x => x.Id == id);

    public async Task AddToken(string id, Session session) =>
        await _usersCollection.UpdateOneAsync(
            x => x.Id == id,
            Builders<UserDB>.Update.Push(x => x.Sessions, session));

    public async Task<bool> RemoveToken(string id, string token)
    {
        var action = await _usersCollection.UpdateOneAsync(
            x => x.Id == id && x.Sessions.Any(s => s.RefreshToken == token),
            Builders<UserDB>.Update.PullFilter(x => x.Sessions, s => s.RefreshToken == token));
        return action.IsAcknowledged && action.ModifiedCount != 0;
    }

    public async Task<bool> RefreshToken(string id, string oldToken,string token)
    {
        var action = await _usersCollection.UpdateOneAsync(
            x => x.Id == id && x.Sessions.Any(s => s.RefreshToken == oldToken),
            Builders<UserDB>.Update.Set("sessions.$.RefreshToken", token));

        return action.IsAcknowledged;
    }

    public async Task<bool> DeleteUser(string id)
    {
        var action = await _usersCollection.DeleteOneAsync(x => x.Id == id);
        return action.IsAcknowledged && action.DeletedCount > 0;
    }
}

