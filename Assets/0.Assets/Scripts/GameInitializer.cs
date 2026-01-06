using Unity.Services.Analytics;
using Unity.Services.Core;

using UnityEngine.UnityConsent;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        //obsolete
        //AnalyticsService.Instance.StartDataCollection();
        EndUserConsent.SetConsentState(new ConsentState {
             AnalyticsIntent = ConsentStatus.Granted,
             AdsIntent = ConsentStatus.Denied
             });
        
    }
}
