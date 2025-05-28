using AvionRelay.External.Hub.Services;
using Microsoft.AspNetCore.Identity;

namespace AvionRelay.External.Hub.Components.Account.Shared.Models;

public class AvionRelayRoleStore : IRoleStore<ApplicationRole>
{
    private readonly SqliteDatabaseService _sqliteDatabase;
    
    public AvionRelayRoleStore(SqliteDatabaseService sqliteDatabase)
    {
        _sqliteDatabase = sqliteDatabase;
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        return await _sqliteDatabase.InsertAsync(role) ? IdentityResult.Success : IdentityResult.Failed();
    }

    /// <inheritdoc />
    public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        return await _sqliteDatabase.UpdateAsync(role) ? IdentityResult.Success : IdentityResult.Failed();
    }

    /// <inheritdoc />
    public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
         return await _sqliteDatabase.DeleteAsync(role) ? IdentityResult.Success : IdentityResult.Failed();
    }

    /// <inheritdoc />
    public async Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        return (await _sqliteDatabase.GetItemsWhereAsync<ApplicationRole>(x => x.Id == role.Id)).FirstOrDefault()?.Id.ToString() ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        return (await _sqliteDatabase.GetItemsWhereAsync<ApplicationRole>(x => x.Id == role.Id)).FirstOrDefault()?.Name;
    }


    /// <inheritdoc />
    public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        return (await _sqliteDatabase.GetItemsWhereAsync<ApplicationRole>(x => x.Id.ToString() == roleId)).FirstOrDefault();
    }


    /// <inheritdoc />
    public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        return (await _sqliteDatabase.GetItemsWhereAsync<ApplicationRole>(x => x.Name == normalizedRoleName)).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        return (await _sqliteDatabase.GetItemsWhereAsync<ApplicationRole>(x => x.Id == role.Id)).FirstOrDefault()?.Name;
    }

    /// <inheritdoc />
    public async Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
    {
        if (roleName is null)
        {
            return;
        }
        role.Name = roleName;
        await _sqliteDatabase.UpdateAsync(role);
    }
    
    /// <inheritdoc />
    public async Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        if (normalizedName is null)
        {
            return;
        }
        role.Name = normalizedName;
        await _sqliteDatabase.UpdateAsync(role);
    }

    
}