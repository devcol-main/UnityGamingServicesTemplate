
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.Economy.Model;

namespace UnityGamingServicesTemplateCloud;

// Handling virtual and real money purchases
public class StoreService
{
    private const string k_HealthPotionPurchaseId = "HEALTH_POTION_VIRTUAL_PURCHASE";

    private readonly ILogger<StoreService> m_Logger;
    private PlayerEconomyService m_PlayerEconomyService;

    public StoreService(ILogger<StoreService> logger, PlayerEconomyService playerEconomy)
    {
        m_Logger = logger;  
        m_PlayerEconomyService = playerEconomy;
    }

    [CloudCodeFunction("VirtualPurchaseHealthPotion")]
    public async Task<PlayerEconomyData> VirtualPurchaseHealthPotion(IExecutionContext context, IGameApiClient gameApiClient)
    {
        try
        {
            await ProcessVirtualPurchase(context, gameApiClient, k_HealthPotionPurchaseId);

            await m_PlayerEconomyService.CleanUpNullOrZeroAmountItems(
                context, gameApiClient, PlayerEconomyService.k_HealthPotionKey);

            await m_PlayerEconomyService.AddOrUpdateInventoryItemAmount(
                context, gameApiClient, PlayerEconomyService.k_HealthPotionKey, 1);
                

            return await m_PlayerEconomyService.GetPlayerEconomyData(context, gameApiClient);
        }
        catch (ApiException ex)
        {
            m_Logger.LogError(ex, $"Failed to purchase potion: {context.PlayerId}");
            throw new System.Exception($"Failed to purchase potion: {ex.Message}", ex);
        }
    }

    private async Task ProcessVirtualPurchase(IExecutionContext context, IGameApiClient gameApiClient, string virtualPurchaseID)
    {
        try
        {
            var purchaseRequest = new PlayerPurchaseVirtualRequest(virtualPurchaseID);

            var purchaseResponse = await gameApiClient.EconomyPurchases.MakeVirtualPurchaseAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId ?? throw new InvalidOperationException("PlayerID is null"),
                purchaseRequest
                );
            
            if (null == purchaseResponse || null == purchaseResponse.Data || null == purchaseResponse.Data.Rewards)
            {
                m_Logger.LogWarning($"Invalid purchase response structure for {virtualPurchaseID}");
                return;
            }
        }
        catch (ApiException ex)
        {
            m_Logger.LogError(ex, $"Failed to process potion purchase: {context.PlayerId}");
            throw; // Rethrow to be handled by the calling method
        }
    }   


}