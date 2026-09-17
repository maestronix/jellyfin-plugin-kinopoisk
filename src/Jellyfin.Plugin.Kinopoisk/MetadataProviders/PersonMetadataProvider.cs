using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.ProviderIdResolvers;
using KinopoiskUnofficialInfo.ApiClient;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.MetadataProviders
{
    public class PersonMetadataProvider : BaseMetadataProvider, IRemoteMetadataProvider<Person, PersonLookupInfo>
    {
        private readonly IKinopoiskApiClient _apiClient;
        private readonly IProviderIdResolver<PersonLookupInfo> _providerIdResolver;
        private readonly ILogger<PersonMetadataProvider> _logger;

        public PersonMetadataProvider(IKinopoiskApiClient apiClient, IProviderIdResolver<PersonLookupInfo> providerIdResolver, ILogger<PersonMetadataProvider> logger, IHttpClientFactory httpClientFactory)
            : base(httpClientFactory)
        {
            _apiClient = apiClient ?? throw new System.ArgumentNullException(nameof(apiClient));
            _providerIdResolver = providerIdResolver ?? throw new System.ArgumentNullException(nameof(providerIdResolver));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public string Name => Constants.ProviderName;

        public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Person>
            {
                QueriedById = true,
                Provider = Constants.ProviderName,
                ResultLanguage = Constants.ProviderMetadataLanguage
            };

            var (resolveResult, kinopoiskId) = await _providerIdResolver.TryResolve(info, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("[GetMetadata] Person provider ID resolved: {Resolved}, KinopoiskId: {KinopoiskId}, Name: {Name}", resolveResult, kinopoiskId, info.Name);

            if (!resolveResult)
            {
                return result;
            }

            var person = await _apiClient.GetPerson(kinopoiskId, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            result.Item = person.ToPerson();
            if (result.Item != null)
            {
                result.HasMetadata = true;
            }

            return result;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
            => Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
    }
}
