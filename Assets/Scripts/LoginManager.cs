using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using System;



public class LoginManager : MonoBehaviour
{

    private async void Awake()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            Debug.Log("Unity Gaming Services Initalizing");
            await UnityServices.InitializeAsync();
        }

        PlayerAccountService.Instance.SignedIn += SignInOrLinkWithUnity;


    }

    private async Task Start()
    {
        /*
        // Automatic Anonymous Sign in
        
        if (!AuthenticationService.Instance.SessionTokenExists)
        {
            Debug.Log("Session Token not found");
            return;
        }

        Debug.Log("Returning player signing in");
        await SignInAnonymouslyAsync();
        */
    }

    // for button
    public async void StartAnonymusSignIn()
    {
        await SignInAnonymouslyAsync();
    }

    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            // if wants to use it with button, need to created func -> StartAnonymusSignIn()
            // b/c Unity's button requires a void return type
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log("Sign in anonymously succeeded!");

            // Shows how to get the playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");

        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    public async void StartUnitySignInAsync()
    {
        if (PlayerAccountService.Instance.IsSignedIn)
        {
            SignInOrLinkWithUnity();
            return;
        }

        try
        {
            await PlayerAccountService.Instance.StartSignInAsync();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
            //SetException(ex);
        }
    }

    private async void SignInOrLinkWithUnity()
    {
        try
        {
            // 1. Player is not yet autehnticated, signing up with Unity
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Signing up with Unity Player Account");
                await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
                Debug.Log("Successfully signed up with Unity Player Account");
                return;

                //UpdateUI();
            }

            // 2. Player is autehnticated but does not yet have a Unity ID linked, link
            if (!HasUnityID())
            {
                Debug.Log("Linking Anonymous account to Unity");
                await LinkWithUnityAsync(PlayerAccountService.Instance.AccessToken);
                Debug.Log("Successfully linked Anonymous account");
                return;
            }

            // 3. Player has authentication and a Unity ID
            Debug.Log("Player is already signed in to their Unity Player Account");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
            //SetException(ex);
        }
    }
    private bool HasUnityID()
    {
        return AuthenticationService.Instance.PlayerInfo.GetUnityId() != null;
    }


    private async Task LinkWithUnityAsync(string accessToken)
    {
        try
        {
            await AuthenticationService.Instance.LinkWithUnityAsync(accessToken);
            Debug.Log("Link is successful.");
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // Prompt the player with an error message.
            Debug.LogError("This user is already linked with another account. Log in instead.");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    // Once unlinked, if their account isn't linked to any additional identity, it transitions to an anonymous account.
    private async Task UnlinkUnityAsync()
    {
        try
        {
            Debug.Log("Unlinking Unity Player Account.");
            await AuthenticationService.Instance.UnlinkUnityAsync();
            Debug.Log("Unlink is successful.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        /*
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        */
    }

    public void SignOut(bool clearSessionToken = false)
    {
        Debug.Log("SignOut");
        // Sign out of Unity Authentication, with the option to clear the session token
        // AuthenticationService.Instance.SignOut(clearSessionToken);
        AuthenticationService.Instance.SignOut(clearSessionToken);

        // Sign out of Unity Player Accounts
        PlayerAccountService.Instance.SignOut();
    }

    public void ClearSessionToken()
    {
        Debug.Log("Clearing session token");
        AuthenticationService.Instance.ClearSessionToken();

    }

    public void DeleteAccount()
    {
        Debug.Log("Deleting account");
        AuthenticationService.Instance.DeleteAccountAsync();
    }



}
