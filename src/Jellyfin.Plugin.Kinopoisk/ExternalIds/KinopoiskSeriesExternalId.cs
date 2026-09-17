using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Kinopoisk.ExternalIds
{
    /// <summary>
    /// Kinopoisk external id for series.
    /// </summary>
    public sealed class KinopoiskSeriesExternalId : IExternalId
    {
        public string ProviderName => Constants.ProviderName;

        public string Key => Constants.ProviderId;

        public ExternalIdMediaType? Type => ExternalIdMediaType.Series;

        public bool Supports(IHasProviderIds item) => item is Series;
    }
}
