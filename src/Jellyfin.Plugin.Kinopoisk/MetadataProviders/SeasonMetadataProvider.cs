using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using KinopoiskUnofficialInfo.ApiClient;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.MetadataProviders
{
    /// <summary>
    /// Provides season metadata from the Kinopoisk series seasons endpoint.
    /// </summary>
    public sealed class SeasonMetadataProvider : BaseMetadataProvider, IRemoteMetadataProvider<MediaBrowser.Controller.Entities.TV.Season, SeasonInfo>
    {
        private readonly IKinopoiskApiClient _apiClient;
        private readonly ILogger<SeasonMetadataProvider> _logger;

        public SeasonMetadataProvider(
            IKinopoiskApiClient apiClient,
            ILogger<SeasonMetadataProvider> logger,
            IHttpClientFactory httpClientFactory)
            : base(httpClientFactory)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MetadataResult<MediaBrowser.Controller.Entities.TV.Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MediaBrowser.Controller.Entities.TV.Season>
            {
                QueriedById = true,
                Provider = Constants.ProviderName,
                ResultLanguage = Constants.ProviderMetadataLanguage
            };

            if (info.IndexNumber is null || !TryGetSeriesId(info.SeriesProviderIds, out var seriesId))
            {
                _logger.LogDebug("[Season] Missing season number or Kinopoisk series ID for {Name}", info.Name);
                return result;
            }

            var response = await _apiClient.GetSeasons(seriesId, cancellationToken).ConfigureAwait(false);
            var season = response?.Items?.FirstOrDefault(s => s.Number == info.IndexNumber.Value);
            if (season is null)
            {
                _logger.LogDebug("[Season] Season {SeasonNumber} not found for Kinopoisk series {SeriesId}", info.IndexNumber.Value, seriesId);
                return result;
            }

            result.Item = new MediaBrowser.Controller.Entities.TV.Season
            {
                Name = info.Name,
                IndexNumber = season.Number
            };
            result.HasMetadata = true;

            _logger.LogDebug("[Season] Found Kinopoisk season {SeasonNumber} for series {SeriesId}", season.Number, seriesId);
            return result;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo searchInfo, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<RemoteSearchResult>>(Array.Empty<RemoteSearchResult>());

        private static bool TryGetSeriesId(IReadOnlyDictionary<string, string> providerIds, out int seriesId)
        {
            seriesId = 0;
            return providerIds is not null
                && providerIds.TryGetValue(Constants.ProviderId, out var value)
                && int.TryParse(value, out seriesId);
        }
    }
}
