using AvionRelay.External;
using Microsoft.AspNetCore.Identity;

namespace AvionRelay.Examples.External.Hub.Components.Account.Shared.Models;

public class AvionRelayUserStore : IUserStore<ApplicationUser>
{
    private readonly SqliteDatabaseService _sqliteDatabase;
    
    public AvionRelayUserStore(SqliteDatabaseService sqliteDatabase)
    {
        _sqliteDatabase = sqliteDatabase;
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public async Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return (await _sqliteDatabase.GetItemsWhereAsync<ApplicationUser>(x => x.Id == user.Id)).FirstOrDefault()?.Id.ToString() ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return (await _sqliteDatabase.GetItemsWhereAsync<ApplicationUser>(x => x.Id == user.Id)).FirstOrDefault()?.UserName;
    }

    /// <inheritdoc />
    public async Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        if (userName is null)
        {
            return;
        }
        user.UserName = userName;
        await _sqliteDatabase.UpdateAsync(user);
    }

    /// <inheritdoc />
    public async Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return (await _sqliteDatabase.GetItemsWhereAsync<ApplicationUser>(x => x.Id == user.Id)).FirstOrDefault()?.NormalizedUserName;
    }

    /// <inheritdoc />
    public async Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        if (normalizedName is null)
        {
            return;
        }
        user.NormalizedUserName = normalizedName;
        await _sqliteDatabase.UpdateAsync(user);
    }

    /// <inheritdoc />
    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        //check if any user with the same username exists
        var existingUser = (await _sqliteDatabase.GetItemsWhereAsync<ApplicationUser>(x => x.UserName == user.UserName)).FirstOrDefault();
        if (existingUser is not null)
        {
            return IdentityResult.Failed();
        }
        return await _sqliteDatabase.InsertAsync(user) ? IdentityResult.Success : IdentityResult.Failed();
        
    }

    /// <inheritdoc />
    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return await _sqliteDatabase.UpdateAsync(user) ? IdentityResult.Success : IdentityResult.Failed();
    }

    /// <inheritdoc />
    public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return await _sqliteDatabase.DeleteAsync(user) ? IdentityResult.Success : IdentityResult.Failed();
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _sqliteDatabase.GetItemWhereAsync<ApplicationUser>(x => x.Id == userId);
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return await _sqliteDatabase.GetItemWhereAsync<ApplicationUser>(x => x.NormalizedUserName == normalizedUserName);
    }
}