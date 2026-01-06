using UnityEngine;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using System;
using System.Linq;

public class PlayerDataManager : MonoBehaviour
{
    private PlayerDataServiceBindings m_Bindings;
    public LoginManager LoginManager;
    public string PlayerName;

    //
    private const int minNameLength = 3;
    private const int maxNameLength = 16;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoginManager.PlayerSignedIn += InitializePlayer;
        m_Bindings = new PlayerDataServiceBindings(CloudCodeService.Instance);
    }

    private async void InitializePlayer()
    {
        try
        {
            var resultFromCloud = await m_Bindings.SayHello(PlayerName);

            //PlayerName = await MyModuleBindings.HandleNewPlayerNameEntry(PlayerName);
            Debug.Log($"Saved new player name in the cloud: {PlayerName}");
            Debug.Log($"{resultFromCloud}");
        }

        catch (CloudCodeException ex)
        {
            Debug.LogException(ex);
        }
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
