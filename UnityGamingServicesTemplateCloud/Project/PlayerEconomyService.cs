using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

using Unity.Services.Economy.Model;

namespace UnityGamingServicesTemplateCloud;

public class PlayerEconomyService
{
    //
    public const string k_GoldCurrencyKey = "GOLD";
    public const string k_HealthPotionKey = "HEALTH_POTION";
    private const int k_StartingHealthPotions = 3;

    //
    private readonly ILogger<PlayerEconomyService> m_Logger;

    public PlayerEconomyService(ILogger<PlayerEconomyService> logger)
    {
        m_Logger = logger;
    }

    [CloudCodeFunction("GetPlayerEconomyData")]
    public async Task<PlayerEconomyData> GetPlayerEconomyData(IExecutionContext context, IGameApiClient gameApiClient)
    {
        try
        {
            //await CleanUpNullOrZeroAmountItems(context, gameApiClient, k_HealthPotionKey);

            // Create economy data object
            var economyData = new PlayerEconomyData();

            // Get player gold and add to economy data
            int goldAmount = await GetPlayerGold(context, gameApiClient);
            economyData.Currencies[k_GoldCurrencyKey] = goldAmount;

            // Add any other currencies here...

            // Get player inventory and add to economy data
            economyData.ItemInventory = await GetPlayerInventoryItemAmountMap(context, gameApiClient);

            return economyData;
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to get economy data: {PlayerId}", context.PlayerId);
            throw new Exception($"Failed to get economy: {ex.Message}", ex);
        }
    }

    public async Task<PlayerEconomyData> InitializeNewPlayerEconomy(IExecutionContext context, IGameApiClient gameApiClient)
    {
        await InitializeInventory(context, gameApiClient);
        return await GetPlayerEconomyData(context, gameApiClient);
    }

    private async Task<List<InventoryResponse>> GetPlayerInventory(
        IExecutionContext context,
        IGameApiClient gameApiClient,
        int? limit = null,
        params string[]? inventoryItemIds)
    {
        try
        {
            List<string>? ids = inventoryItemIds?.Length > 0
                ? inventoryItemIds.ToList()
                : null;

            // Call the API to get player inventory
            var playerInventory = await gameApiClient.EconomyInventory.GetPlayerInventoryAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId!,
                inventoryItemIds: ids,
                limit: limit
            );

            return playerInventory.Data.Results;
        }

        catch (ApiException ex)
        {
            m_Logger.LogError($"Failed to get inventory for player '{context.PlayerId}'. Error: {ex.Message}");
            throw new Exception($"Failed to get inventory: {ex.Message}", ex);
        }
    }

    private async Task<Dictionary<string, int>> GetPlayerInventoryItemAmountMap(
        IExecutionContext context,
        IGameApiClient gameApiClient,
        params string[]? inventoryItemIds)
    {
        var items = await GetPlayerInventory(context, gameApiClient, inventoryItemIds: inventoryItemIds);

        return items
            .Where(item => !string.IsNullOrEmpty(item.InventoryItemId))
            .ToDictionary(
                item => item.InventoryItemId!,
                item => GetInventoryItemCustomData<int?>(item, "amount") ?? 1
            );
    }

    private T? GetInventoryItemCustomData<T>(InventoryResponse item, string key)
    {
        if (item?.InstanceData == null) return default;

        try
        {
            // Convert to JObject if it isn't already
            var jObject = item.InstanceData as Newtonsoft.Json.Linq.JObject
                ?? Newtonsoft.Json.Linq.JObject.Parse(item.InstanceData?.ToString() ?? "{}");

            // Get value using indexer syntax
            var token = jObject[key];
            if (token != null)
            {
                // Convert to requested type
                return token.ToObject<T>();
            }
        }
        catch (Exception ex)
        {
            m_Logger.LogWarning($"Failed to get '{key}' from item '{item.InventoryItemId}': {ex.Message}");
        }

        return default;
    }



    private async Task<int> GetCurrencyAmount(IExecutionContext context, IGameApiClient gameApiClient, string key)
    {
        try
        {
            var playerCurrenciesData = await gameApiClient.EconomyCurrencies.GetPlayerCurrenciesAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId!
                );

            // Find the currency with the matching key
            CurrencyBalanceResponse? targetCurrency =
                playerCurrenciesData.Data.Results.FirstOrDefault(currency => currency.CurrencyId == key);

            if (targetCurrency != null)
            {
                return (int)targetCurrency.Balance;
            }
            else
            {
                throw new Exception($"Currency '{key}' not found");
            }
        }
        catch (ApiException ex)
        {
            throw new Exception($"Failed to get currency '{key}' for player '{context.PlayerId}'. Error: {ex.Message}");
        }
    }

    

    private async Task<int> GetInventoryItemAmount(IExecutionContext context, IGameApiClient gameApiClient, string key)
    {
        try
        {
            var inventoryResponse = await gameApiClient.EconomyInventory.GetPlayerInventoryAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId!,
                inventoryItemIds: new List<string> { key }
                );

            InventoryResponse? item = inventoryResponse.Data.Results.FirstOrDefault();

            if (null == item)
            {
                // This could happen if the item hasn't been added to inventory yet
                m_Logger.LogInformation($"Inventory item {key} not found for player '{context.PlayerId}'");
                return 0; // Return 0 when item doesn't exist instead of throwing
            }

            if (!TryParseInventoryItemAmount(item, out int amount))
            {
                // TryParseInventoryItemAmount already logs the error
                return 0; // Return 0 when parsing fails instead of throwing
            }

            return amount;
        }
        catch (ApiException ex)
        {
            m_Logger.LogError(ex, $"Failed to get inventory item data for player '{context.PlayerId}'");
            throw new Exception($"Failed to get inventory item data for player '{context.PlayerId}'. Error: {ex.Message}");
        }
    }
    private bool TryParseInventoryItemAmount(InventoryResponse itemResponse, out int amount)
    {
        amount = 0; // Default value if parsing fails

        if (null == itemResponse.InstanceData)
        {
            m_Logger.LogWarning($"Item '{itemResponse.InventoryItemId}' instance data is null");
            return false;
        }

        try
        {
            string json = $"{itemResponse.InstanceData}";
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (null != data && data.TryGetValue("amount", out var amountObj))
            {
                if (int.TryParse(amountObj.ToString(), out amount))
                {
                    return true;
                }

                m_Logger.LogWarning($"Amount value '{amountObj}' for '{itemResponse.InventoryItemId}' is not a valid integer");
                return false;

            }

            m_Logger.LogWarning($"Instance data for '{itemResponse.InventoryItemId}' doesn't contain an 'amount' property");
            return false;

        }
        catch (Exception ex)
        {
            m_Logger.LogWarning($"Failed to parse inventory item amount for '{itemResponse.InventoryItemId}': {ex.Message}");
            return false;
        }
    }

    public async Task AddNewInventoryItem(
        IExecutionContext context,
        IGameApiClient gameApiClient,
        string itemId,
        int amount)
    {
        var instanceData = new Dictionary<string, object>
        {
            { "amount", amount }
        };

        await AddNewInventoryItem(context, gameApiClient, itemId, instanceData);
    }

    public async Task AddNewInventoryItem(
        IExecutionContext context,
        IGameApiClient gameApiClient,
        string itemId,
        Dictionary<string, object> instanceData)
    {

        var inventoryRequest = new AddInventoryRequest(itemId, instanceData: instanceData);

        try
        {
            await gameApiClient.EconomyInventory.AddInventoryItemAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId ?? throw new InvalidOperationException("PlayerId is null"),
                inventoryRequest
            );
        }
        catch (ApiException ex)
        {
            m_Logger.LogError(
                $"Failed to add inventory item '{itemId}' for player '{context.PlayerId}'. Error: {ex.Message}");
            throw new Exception($"Failed to add inventory item '{itemId}': {ex.Message}", ex);
        }
    }  


    private async Task InitializeInventory(IExecutionContext context, IGameApiClient gameApiClient)
    {
        var startingItems = new Dictionary<string, int>
        {
            { k_HealthPotionKey, k_StartingHealthPotions },
        };

        foreach (var item in startingItems)
        {
            try
            {
                await AddNewInventoryItem(context, gameApiClient, item.Key, item.Value);
            }

            catch (Exception ex)
            {
                m_Logger.LogError(ex, $"Failed to grant initial inventory item {item.Key}");
            }
        }
    }
    
    private async Task<int> GetPlayerGold(IExecutionContext context, IGameApiClient gameApiClient)
    {
        return await GetCurrencyAmount(context, gameApiClient, k_GoldCurrencyKey);
    }

    
    private async Task<int> GetHealthPotionAmount(IExecutionContext context, IGameApiClient gameApiClient)
    {
        return await GetInventoryItemAmount(context, gameApiClient, k_HealthPotionKey);
    }

    
    //==========
    /*
    // client can call when a new player joins the game
    [CloudCodeFunction("InitializeInventory")]
    public async Task InitializeInventory(IExecutionContext context, IGameApiClient gameApiClient)
    {
        var startingItems = new Dictionary<string, int>
        {
            { k_HealthPotionKey, k_StartingHealthPotions },
        };

        foreach (var item in startingItems)
        {
            try
            {
                await AddNewInventoryItem(context, gameApiClient, item.Key, item.Value);
            }

            catch (Exception ex)
            {
                m_Logger.LogError(ex, $"Failed to grant initial inventory item {item.Key}");
            }
        }
    }

    // Convenience method
    [CloudCodeFunction("GetPlayerGold")]
    public async Task<int> GetPlayerGold(IExecutionContext context, IGameApiClient gameApiClient)
    {
        return await GetCurrencyAmount(context, gameApiClient, k_GoldCurrencyKey);
    }

    // Convenience method for getting health potions
    [CloudCodeFunction("GetHealthPotionAmount")]
    public async Task<int> GetHealthPotionAmount(IExecutionContext context, IGameApiClient gameApiClient)
    {
        return await GetInventoryItemAmount(context, gameApiClient, k_HealthPotionKey);
    }
    */

}

