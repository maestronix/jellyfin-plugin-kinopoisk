using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.ProviderIdResolvers;
using KinopoiskUnofficialInfo.ApiClient;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.MetadataProviders
{
    public abstract class BaseVideoMetadataProvider<TItemType, TLookupInfoType> : BaseMetadataProvider, IRemoteMetadataProvider<TItemType, TLookupInfoType>
        where TItemType : BaseItem, IHasLookupInfo<TLookupInfoType>
        where TLookupInfoType : ItemLookupInfo, new()
    {
        private readonly ILogger _logger;
        private readonly IKinopoiskApiClient _apiClient;
        private readonly IProviderIdResolver<TLookupInfoType> _providerIdResolver;

        protected BaseVideoMetadataProvider(
            IKinopoiskApiClient kinopoiskApiClient,
            IProviderIdResolver<TLookupInfoType> providerIdResolver,
            ILogger logger,
            IHttpClientFactory httpClientFactory)
            : base(httpClientFactory)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _apiClient = kinopoiskApiClient ?? throw new System.ArgumentNullException(nameof(kinopoiskApiClient));
            _providerIdResolver = providerIdResolver ?? throw new System.ArgumentNullException(nameof(providerIdResolver));
        }

        public string Name => Constants.ProviderName;

        protected abstract TItemType ConvertResponseToItem(Film apiResponse);

        public async Task<MetadataResult<TItemType>> GetMetadata(TLookupInfoType info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<TItemType>
            {
                QueriedById = true,
                Provider = Constants.ProviderName,
                ResultLanguage = Constants.ProviderMetadataLanguage
            };

            var (resolveResult, kinopoiskId) = await _providerIdResolver.TryResolve(info, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("[GetMetadata] ProviderId resolved: {Resolved}, KinopoiskId: {KinopoiskId}, Name: {Name}", resolveResult, kinopoiskId, info.Name);

            if (!resolveResult)
            {
                return result;
            }

            var film = await _apiClient.GetSingleFilm(kinopoiskId, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            result.Item = ConvertResponseToItem(film);
            if (result.Item != null)
            {
                result.HasMetadata = true;
            }

            var staff = await _apiClient.GetStaff(kinopoiskId, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var sanitizedPersons = await SanitizeEmptyImagePersonInfos(staff.ToPersonInfos()).ConfigureAwait(false);
            foreach (var item in sanitizedPersons)
            {
                result.AddPerson(item);
            }

            var trailers = await _apiClient.GetTrailers(kinopoiskId, cancellationToken).ConfigureAwait(false);
            var remoteTrailers = trailers.ToMediaUrls();
            if (remoteTrailers is not null && result.Item is not null)
            {
                result.Item.RemoteTrailers = remoteTrailers;
            }

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(TLookupInfoType searchInfo, CancellationToken cancellationToken)
        {
            _logger.LogDebug(
                "[GetSearchResults] Starting. Name={Name}, ProviderIds={@ProviderIds}, Path={Path}",
                searchInfo.Name,
                searchInfo.ProviderIds,
                searchInfo.Path);

            if (searchInfo.TryGetProviderId(Constants.ProviderId, out var kinopoiskIdStr)
                && int.TryParse(kinopoiskIdStr, out var kinopoiskId))
            {
                _logger.LogDebug("[GetSearchResults] Looking up Kinopoisk ID {KinopoiskId}", kinopoiskId);
                var singleResult = (await _apiClient.GetSingleFilm(kinopoiskId, cancellationToken).ConfigureAwait(false)).ToRemoteSearchResult();
                return singleResult is null
                    ? Enumerable.Empty<RemoteSearchResult>()
                    : Enumerable.Repeat(singleResult, 1);
            }

            if (string.IsNullOrWhiteSpace(searchInfo.Name))
            {
                _logger.LogDebug("[GetSearchResults] No Kinopoisk ID or name supplied");
                return Enumerable.Empty<RemoteSearchResult>();
            }

            _logger.LogDebug("[GetSearchResults] Searching Kinopoisk by name {Name}", searchInfo.Name);
            return (await _apiClient.SearchByKeyword(searchInfo.Name, cancellationToken: cancellationToken).ConfigureAwait(false))
                .ToRemoteSearchResults(_logger);
        }

        protected async Task<IEnumerable<PersonInfo>> SanitizeEmptyImagePersonInfos(IEnumerable<PersonInfo> images)
        {
            using var httpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = false }, true);
            var sanitizer = new RemoteImageUrlSanitizer(httpClient);
            var res = await Task.WhenAll(images.Select(async p =>
            {
                p.ImageUrl = await sanitizer.SanitizeRemoteImageUrl(p.ImageUrl).ConfigureAwait(false);
                return p;
            })).ConfigureAwait(false);

            return res.Where(i => i != null).ToArray();
        }
    }
}
