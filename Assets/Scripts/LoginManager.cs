using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using System;
using System.Collections.Generic;

using Facebook.Unity;

public class LoginManager : MonoBehaviour
{
    
    private async void Awake()
    {
        InitializeFacebook();

        //
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

    #region Facebook
        
    
    private void InitializeFacebook()
    {
        if (!FB.IsInitialized)
        {
            // Initialize the Facebook SDK
            FB.Init(InitCallback, OnHideUnity);
        }
        else
        {
            // Already initialized, signal an app activation App Event
            FB.ActivateApp();
        }
    }

    private void InitCallback()
    {
        if (FB.IsInitialized)
        {
            // Signal an app activation App Event
            FB.ActivateApp();
            // Continue with Facebook SDK
        }
        else
        {
            Debug.Log("Failed to Initialize the Facebook SDK");
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
        {
            // Pause the game - we will need to hide
            Time.timeScale = 0;
        }
        else
        {
            // Resume the game - we're getting focus again
            Time.timeScale = 1;
        }
    }


    public void StartFacebookSignIn()
    {
        // Define the permissions
        var perms = new List<string>() { "public_profile", "email" };

        FB.LogInWithReadPermissions(perms, async result =>
        {
            if (FB.IsLoggedIn)
            {              
                //Token = AccessToken.CurrentAccessToken.TokenString;
                //Debug.Log($"Facebook Login token: {Token}");


                // AccessToken class will have session detals
                var facebookAccessToken = Facebook.Unity.AccessToken.CurrentAccessToken.TokenString;

                // if the player is not signed in with Unity authentication, they're a new player, 
                // so they're signing into authentication using their Facebook identity
                if(!AuthenticationService.Instance.IsSignedIn)
                {
                    await SignInWithFacebookAsync(facebookAccessToken);
                }
                // If they've already signed in, they might have signed in anonymously as a guest
                // or they could have used another identity platform to gain authentication
                else
                {
                    await LinkWithFacebookAsync(facebookAccessToken);
                }

            }
            else
            {
                //Error = "User cancelled login";
                Debug.Log("[Facebook Login] User cancelled login");

                
            }
        });
    }

    private async Task SignInWithFacebookAsync(string accessToken)
    {
        try
        {
            // Facebook access token and sends it to Unity authentication.
            // it uses await b/c this takes time to happen over the internet 
            // and if success ful the player is now signed in
            await AuthenticationService.Instance.SignInWithFacebookAsync(accessToken);
            Debug.Log("Signed in with Facebook");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }
    }

    private async Task LinkWithFacebookAsync(string accessToken)
    {
        try
        {            
            await AuthenticationService.Instance.LinkWithFacebookAsync(accessToken);
            Debug.Log("Linked with Facebook");
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            Debug.LogException(ex);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    


    #endregion 


}
