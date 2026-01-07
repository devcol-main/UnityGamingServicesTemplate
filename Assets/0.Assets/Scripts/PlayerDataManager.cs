using UnityEngine;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using System;
using System.Linq;
using GooglePlayGames.BasicApi;
using Newtonsoft.Json;
using Unity.Services.CloudCode.GeneratedBindings.UnityGamingServicesTemplateCloud;

public class PlayerDataManager : MonoBehaviour
{
    // Bindings
    private PlayerDataServiceBindings m_Bindings;
    //private PlayerEconomyServiceBindings m_EconomyServiceBindings;

    //
    public LoginManager LoginManager;
    public string PlayerName;
    //public PlayerDataManager PlayerDataLocal;
    public PlayerData PlayerDataLocal;

    //public event Action<PlayerDataManager> PlayerDataUpdated;
    public event Action<PlayerData> PlayerDataUpdated;

    //
    private const int minNameLength = 3;
    private const int maxNameLength = 16;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoginManager.PlayerSignedIn += InitializePlayer;

        m_Bindings = new PlayerDataServiceBindings(CloudCodeService.Instance);
        //m_EconomyServiceBindings = new PlayerEconomyServiceBindings(CloudCodeService.Instance);
    }

    private async void InitializePlayer()
    {
        try
        {

            // var resultFromCloud = await m_Bindings.SayHello(PlayerName);            
            // //
            // var gold = await m_EconomyServiceBindings.GetPlayerGold();
            
            // Debug.Log($"{resultFromCloud} has {gold} gold");
            
            // //
            // await m_EconomyServiceBindings.InitializeInventory();
            // var potionAmount = await m_EconomyServiceBindings.GetHealthPotionAmount();
            // Debug.Log($"{PlayerName} has {potionAmount} health potions");

            var playerDataResponse = await m_Bindings.HandlePlayerSignIn();
            

            PlayerDataLocal = playerDataResponse.PlayerData;
            PlayerDataUpdated?.Invoke(PlayerDataLocal);
            LogResponse(playerDataResponse);

        }

        catch (CloudCodeException ex)
        {
            Debug.LogException(ex);
        }
    }


    private void LogResponse(PlayerDataResponse response)
    {
        string economyJson = JsonConvert.SerializeObject(response.EconomyData, Formatting.Indented);

        Debug.Log(
            $"===== Player Sign-In Response =====\n" +
            $"Name: {response.PlayerData.DisplayName}\n" +
            $"New Player: {response.IsNewPlayer}\n" +
            $"XP: {response.PlayerData.Experience}\n" +
            $"Economy: {economyJson}\n" +
            $"===================================="
        );
    }

    public async void SaveNewPlayerName()
    {
        if (!IsPlayerNameValid(PlayerName))
        {
            string nameLength = ($"name length should be between {minNameLength} ~ {maxNameLength} characters");
            string onlyLetNumWarning = ("Only letters and numbers | No Special characters");

            Debug.LogWarning($"Name is not valid | {nameLength} | {onlyLetNumWarning} ");
            return;
        }

        try
        {
            PlayerName = await m_Bindings.HandleNewPlayerNameEntry(PlayerName);
            Debug.Log($"Saved new player name in the cloud: {PlayerName}");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

    }

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

    private void Osable()
    {
        LoginManager.PlayerSignedIn -= InitializePlayer;      
    }
}
