using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using System;

namespace DiscordMafia.Services
{
    public class DIContractResolver : DefaultContractResolver
    {
        private readonly IServiceProvider _provider;

        public DIContractResolver(IServiceProvider provider)
        {
            _provider = provider;
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            JsonObjectContract contract = base.CreateObjectContract(objectType);
            contract.DefaultCreator = (() => ActivatorUtilities.CreateInstance(_provider, objectType));
            return contract;
        }
    }
}
