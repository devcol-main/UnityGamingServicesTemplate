
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityGamingServicesTemplateCloud
{
    public class PlayerData
    {
        // JsonProperties provide a mapping 
        // between our C# property names and the JSON field names during serialization and deserialization
        // this ensure our data gets correctly converted when sending it between the client and server
        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }
        [JsonProperty("experience")]
        public int Experience { get; set; }

        public static implicit operator PlayerData((bool playerExists, PlayerData? playerData) v)
        {
            throw new NotImplementedException();
        }
    }
    public class PlayerEconomyData
    {
        [JsonProperty("currencies")]
        public Dictionary<string, int> Currencies { get; set; } = new Dictionary<string, int>();
        [JsonProperty("itemInventory")]
        public Dictionary<string, int> ItemInventory { get; set; } = new Dictionary<string, int>();
    }

    // DTO, data transfer object, common pattern in client server architectures
    public class PlayerDataResponse
    {
        [JsonProperty("playerData")]
        public PlayerData PlayerData { get; set; } = new PlayerData();

        [JsonProperty("economyData")]
        public PlayerEconomyData EconomyData { get; set; } = new PlayerEconomyData();

        [JsonProperty("isNewPlayer")]
        public bool IsNewPlayer { get; set; }
    }
}
