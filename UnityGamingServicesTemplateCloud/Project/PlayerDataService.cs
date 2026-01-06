using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace UnityGamingServicesTemplateCloud;
public class PlayerDataService
{
    //
    public const string k_PlayerNameKey = "PLAYER_NAME";
    private const int minNameLength = 3;
    private const int maxNameLength = 16;

    //
    private readonly ILogger<PlayerDataService> _logger;

    public PlayerDataService(ILogger<PlayerDataService> logger)
    {
        _logger = logger;
    }

    // not using it b/c easy to get hacked
    //[CloudCodeFunction("SaveData")]
    private async Task SaveData(IExecutionContext context, IGameApiClient gameApiClient, string key, string value)
    {
        try
        {
            await gameApiClient.CloudSaveData.SetItemAsync(
                context, 
                context.AccessToken!, 
                context.ProjectId!,
                context.PlayerId!, 
                new SetItemBody(key, value));
                
            _logger.LogInformation("Successfully saved data for key: {Key}", key);
        }
        catch (ApiException ex)
        {
            _logger.LogError("Failed to save data for key {Key}. Error: {Error}", key, ex.Message);
            throw new Exception($"Unable to save player data: {ex.Message}");
        }
    }

    // not using it b/c easy to get hacked
    //[CloudCodeFunction("GetData")]
    private async Task<string> GetData(IExecutionContext context, IGameApiClient gameApiClient, string key)
    {
        try
        {
            var result = await gameApiClient.CloudSaveData.GetItemsAsync(
                context, 
                context.AccessToken!,
                context.ProjectId!, 
                context.PlayerId!, 
                new List<string> { key });
                
            var data = result.Data.Results.FirstOrDefault()?.Value?.ToString() ?? string.Empty;
            _logger.LogInformation("Successfully retrieved data for key: {Key}", key);
            return data;
        }
        catch (ApiException ex)
        {
            _logger.LogError("Failed to retrieve data for key {Key}. Error: {Error}", key, ex.Message);
            throw new Exception($"Unable to retrieve player data: {ex.Message}");
        }
    }    
    //

    //
    [CloudCodeFunction("SayHello")]
    public string Hello(string name)
    {
        return $"Hello, {name}!";
    }

    [CloudCodeFunction("HandleNewPlayerNameEntry")]
    public async Task<string> HandleNewPlayerNameEntry(IExecutionContext context, IGameApiClient gameApiClient, string newName)
    {
        if (IsPlayerNameValid(newName))
        {
            await SaveData(context, gameApiClient, k_PlayerNameKey, newName);
            return newName;
        }
        //throw new ArgumentException($"Name is not valid | length should be between {minNameLength} ~ {maxNameLength} characters | No Special characters");
        throw new ArgumentException($"Name is not valid");
    }

    //////
    private bool IsPlayerNameValid(string name)
    {
        if (name.Length is < minNameLength or > maxNameLength)
        {                 
            
            return false;
        }

        // Check for special characters using LINQ
        if (!name.All(c => char.IsLetterOrDigit(c)))
        {
            return false;
        }

        return true;
    }
}


