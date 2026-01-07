using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace UnityGamingServicesTemplateCloud;

public class PlayerDataService
{
    // used in Player Data part (for saving player name)
    /*
    public const string k_PlayerNameKey = "PLAYER_NAME";
    

    //
    private readonly ILogger<PlayerDataService> _logger;

    public PlayerDataService(ILogger<PlayerDataService> logger)
    {
        _logger = logger;
    }
*/

    // new in Player Economy
    public const string k_PlayerDataKey = "PLAYER_DATA";
    public const string k_PlayerNameKey = "PLAYER_NAME";

    //
    private const int minNameLength = 3;
    private const int maxNameLength = 16;

    private PlayerEconomyService m_PlayerEconomyService;

    private static ILogger<PlayerDataService> m_Logger;

    public PlayerDataService(ILogger<PlayerDataService> logger, PlayerEconomyService playerEconomy)
    {
        m_Logger = logger;
        m_PlayerEconomyService = playerEconomy;
    }

    [CloudCodeFunction("HandlePlayerSignIn")]
    public async Task<PlayerDataResponse> HandlePlayerSignIn(IExecutionContext context, IGameApiClient gameApiClient)
    {
        var (playerExists, playerData) = await TryGetPlayerData(context, gameApiClient);

        if (!playerExists || playerData == null)
        {
            // New player!
            return await InitializeNewPlayer(context, gameApiClient);
        }

        // Returning player
        PlayerEconomyData economyData = await m_PlayerEconomyService.GetPlayerEconomyData(context, gameApiClient);

        return new PlayerDataResponse
        {
            PlayerData = playerData,
            EconomyData = economyData,
            IsNewPlayer = false,
        };
    }

    // Use Try methods for operations that might fail in expected, normal ways
    private async Task<(bool playerExists, PlayerData? playerData)> TryGetPlayerData(IExecutionContext context, IGameApiClient gameApiClient)
    {
        try
        {
            // OLD - throws exception on missing data
            // var playerDataObject = await GetData(context, gameApiClient, k_PlayerDataKey);

            // NEW - gracefully handles missing data  
            var (success, playerDataJson) = await TryGetData(context, gameApiClient, k_PlayerDataKey);

            if (playerDataJson == null)
            {
                return (false, null);
            }

            var playerData = JsonConvert.DeserializeObject<PlayerData>($"{playerDataJson}");
            return (playerData != null, playerData);
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, $"Error deserializing player data for player: {context.PlayerId}");
            return (false, null);
        }
    }

    // public async Task<object> GetData(IExecutionContext context, IGameApiClient gameApiClient, string key)
    // {
    //     try
    //     {
    //         var result = await gameApiClient.CloudSaveData.GetItemsAsync(
    //             context, 
    //             context.AccessToken,
    //             context.ProjectId, 
    //             context.PlayerId ?? throw new InvalidOperationException("PlayerId is null"), 
    //             new List<string> { key });

    //         // Possible fix for no data:
    //         // if (result.Data.Results.Count == 0) return null;

    //         //return result.Data.Results.First().Value;
    //         return result.Data.Results.FirstOrDefault().Value;
    //     }
    //     catch (ApiException ex)
    //     {
    //         m_Logger.LogError("Failed to get data. Error: {Error}", ex.Message);
    //         throw new Exception($"Failed to get data for playerId '{context.PlayerId}'. Error: {ex.Message}");
    //     }
    // }

    // not using it b/c easy to get hacked
    //[CloudCodeFunction("SaveData")]

    private async Task<PlayerDataResponse> InitializeNewPlayer(IExecutionContext context, IGameApiClient gameApiClient)
    {
        PlayerData newPlayerData = new PlayerData
        {
            DisplayName = "New Player",
            Experience = 0,
        };

        PlayerEconomyData newEconomyData;

        try
        {
            // Save new player data
            await SaveData(context, gameApiClient, k_PlayerDataKey, newPlayerData);

            // Initialize new player inventory
            newEconomyData = await m_PlayerEconomyService.InitializeNewPlayerEconomy(context, gameApiClient);

            m_Logger.LogInformation($"New player initialized: {context.PlayerId}");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, $"Failed to initialize new player: {context.PlayerId}");
            throw new Exception("Failed to initialize new player", ex);
        }

        return new PlayerDataResponse
        {
            PlayerData = newPlayerData,
            EconomyData = newEconomyData,
            IsNewPlayer = true
        };
    }

    public async Task<(bool success, string? value)> TryGetData(IExecutionContext context, IGameApiClient gameApiClient, string key)
    {
        try
        {
            var response = await gameApiClient.CloudSaveData.GetItemsAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId ?? throw new InvalidOperationException("PlayerId is null"),
                new List<string> { key });

            var retrievedItem = response.Data.Results.FirstOrDefault();
            if (retrievedItem != null)
            {
                return (true, Convert.ToString(retrievedItem.Value));
            }

            return (false, null);
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error retrieving data from CloudSave for player {PlayerId}", context.PlayerId);
            return (false, null);
        }
    }

    private async Task SaveData(IExecutionContext context, IGameApiClient gameApiClient, string key, object value)
    {
        try
        {
            await gameApiClient.CloudSaveData.SetItemAsync(
                context,
                context.AccessToken!,
                context.ProjectId!,
                context.PlayerId!,
                new SetItemBody(key, value));

            //_logger.LogInformation("Successfully saved data for key: {Key}", key);
            m_Logger.LogInformation("Successfully saved data for key: {Key}", key);
        }
        catch (ApiException ex)
        {
            //_logger.LogError("Failed to save data for key {Key}. Error: {Error}", key, ex.Message);
            m_Logger.LogError("Failed to save data for key {Key}. Error: {Error}", key, ex.Message);
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
            //_logger.LogInformation("Successfully retrieved data for key: {Key}", key);
            m_Logger.LogInformation("Successfully retrieved data for key: {Key}", key);
            return data;
        }
        catch (ApiException ex)
        {
            //_logger.LogError("Failed to retrieve data for key {Key}. Error: {Error}", key, ex.Message);
            m_Logger.LogError("Failed to retrieve data for key {Key}. Error: {Error}", key, ex.Message);
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
            PlayerData? playerData = await TryGetPlayerData(context, gameApiClient);
 
            if (null == playerData)
            {
                throw new InvalidOperationException($"Player data not found for player: {context.PlayerId}");
            }

            playerData.DisplayName = newName;

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


