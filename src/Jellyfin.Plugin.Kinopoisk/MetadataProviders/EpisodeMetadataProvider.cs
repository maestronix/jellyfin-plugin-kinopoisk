using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    /// Provides episode metadata from the Kinopoisk series seasons endpoint.
    /// </summary>
    public sealed class EpisodeMetadataProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
    {
        private readonly IKinopoiskApiClient _apiClient;
        private readonly ILogger<EpisodeMetadataProvider> _logger;

        public EpisodeMetadataProvider(
            IKinopoiskApiClient apiClient,
            ILogger<EpisodeMetadataProvider> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Name => Constants.ProviderName;

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>
            {
                QueriedById = true,
                Provider = Constants.ProviderName,
                ResultLanguage = Constants.ProviderMetadataLanguage
            };

            if (info.ParentIndexNumber is null || info.IndexNumber is null || !TryGetSeriesId(info.SeriesProviderIds, out var seriesId))
            {
                _logger.LogDebug(
                    "[Episode] Missing season/episode number or Kinopoisk series ID for {Name}",
                    info.Name);
                return result;
            }

            var response = await _apiClient.GetSeasons(seriesId, cancellationToken).ConfigureAwait(false);
            var episode = response?.Items?
                .Where(s => s.Number == info.ParentIndexNumber.Value)
                .SelectMany(s => s.Episodes ?? Array.Empty<Episode>())
                .FirstOrDefault(e => e.SeasonNumber == info.ParentIndexNumber.Value && e.EpisodeNumber == info.IndexNumber.Value);

            if (episode is null)
            {
                _logger.LogDebug(
                    "[Episode] Episode S{Season}E{Episode} not found for Kinopoisk series {SeriesId}",
                    info.ParentIndexNumber.Value,
                    info.IndexNumber.Value,
                    seriesId);
                return result;
            }

            var name = !string.IsNullOrWhiteSpace(episode.NameRu)
                ? episode.NameRu
                : episode.NameEn;

            var item = new Episode
            {
                Name = string.IsNullOrWhiteSpace(name) ? info.Name : name,
                OriginalTitle = !string.IsNullOrWhiteSpace(episode.NameEn) && !string.Equals(episode.NameEn, name, StringComparison.OrdinalIgnoreCase)
                    ? episode.NameEn
                    : null,
                Overview = episode.Synopsis,
                ParentIndexNumber = episode.SeasonNumber,
                IndexNumber = episode.EpisodeNumber,
                PremiereDate = ParseReleaseDate(episode.ReleaseDate)
            };

            result.Item = item;
            result.HasMetadata = true;

            _logger.LogDebug(
                "[Episode] Found Kinopoisk metadata for S{Season}E{Episode}: {Name}",
                episode.SeasonNumber,
                episode.EpisodeNumber,
                item.Name);

            return result;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<RemoteSearchResult>>(Array.Empty<RemoteSearchResult>());

        private static bool TryGetSeriesId(IReadOnlyDictionary<string, string> providerIds, out int seriesId)
        {
            seriesId = 0;
            return providerIds is not null
                && providerIds.TryGetValue(Constants.ProviderId, out var value)
                && int.TryParse(value, out seriesId);
        }

        private static DateTime? ParseReleaseDate(string releaseDate)
        {
            if (string.IsNullOrWhiteSpace(releaseDate))
                return null;

            return DateTime.TryParse(
                releaseDate,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed)
                ? parsed
                : null;
        }
    }
}
