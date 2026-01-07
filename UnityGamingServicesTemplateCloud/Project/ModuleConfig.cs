using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Microsoft.Extensions.DependencyInjection;


using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace UnityGamingServicesTemplateCloud
{
    public class ModuleConfig : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config)
        {
            config.Dependencies.AddSingleton(GameApiClient.Create());

            config.Dependencies.AddSingleton<PlayerEconomyService>();
        }
    }
}