using TMPro;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField]
    PlayerEconomyManager m_PlayerEconomyManager;

    [Header("Text")]
    [SerializeField]
    private TMP_Text goldText;

    [SerializeField]
    private TMP_Text healthPotionText;


    private int goldAmount;
    private int healthPotionAmount;

    private void Onnable()
    {
        m_PlayerEconomyManager.EconomyConfigSynced += UpdateVitualPurchase;        
    }

    private void UpdateVitualPurchase()
    {
        Debug.Log("UpdateVitualPurchase from " + this.gameObject.name);

        try
        {
            //LogVirtualPurchasesFromConfig();
            //InitializePurchaseCosts();
            goldText.text = "Gold: " +  m_PlayerEconomyManager.Gold.ToString();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to sync Economy configuration : {ex.Message}");
        }
    }




    private void OnDisable()
    {
        m_PlayerEconomyManager.EconomyConfigSynced -= UpdateVitualPurchase;
    }
    
}
