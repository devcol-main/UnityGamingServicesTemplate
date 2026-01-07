using Unity.Services.Analytics;
using Unity.Services.Core;

using UnityEngine.UnityConsent;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private async void Awake()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            Debug.Log("Unity Gaming Services Initalizing");
            await UnityServices.InitializeAsync();
        }

        //obsolete
        //AnalyticsService.Instance.StartDataCollection();
        EndUserConsent.SetConsentState(new ConsentState {
             AnalyticsIntent = ConsentStatus.Granted,
             AdsIntent = ConsentStatus.Denied
             });
        
    }
}
