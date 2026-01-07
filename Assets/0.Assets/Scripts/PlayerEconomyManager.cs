using System;
using System.Collections.Generic;

using UnityEngine;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using Unity.Services.CloudCode.GeneratedBindings.UnityGamingServicesTemplateCloud;

using Unity.Services.Economy;
using Unity.Services.Authentication;


public class PlayerEconomyManager : MonoBehaviour
{
    public PlayerEconomyData EconomyDataLocal {get; private set;} = new PlayerEconomyData
    {
        Currencies = new Dictionary<string, int>(),
        ItemInventory = new Dictionary<string, int>()
    };

    // Currency constants
    public const string k_GoldCurrencyKey = "GOLD";

    public int Gold
    {
        get => GetCurrencyAmount(k_GoldCurrencyKey);
    }

    public event Action<PlayerEconomyData> PlayerEconomyUpdated;
    public event Action EconomyConfigSynced; 

    

    private void Start()
    {
        AuthenticationService.Instance.SignedIn += SyncEconomyConfig;
    }

    private async void SyncEconomyConfig()
    {
        try
        {
            await EconomyService.Instance.Configuration.SyncConfigurationAsync();
            Debug.Log("Economy configuration synced");
            EconomyConfigSynced?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    /// <summary>
    /// Method for other systems to update economy data from server.
    /// Centralizes subscription for economy updates (e.g. for UI updates).
    /// </summary>
    /// <param name="economyData"></param>
    public void HandleEconomyUpdate(PlayerEconomyData economyData)
    {
        EconomyDataLocal = economyData;
        PlayerEconomyUpdated?.Invoke(EconomyDataLocal);

    }

    // Safely get a currency amount, returning 0 if it doesn't exist
    public int GetCurrencyAmount(string currencyKey)
    {
        if (EconomyDataLocal.Currencies.TryGetValue(currencyKey, out int amount))
        {
            return amount;
        }
        return 0;
    }

     
}
