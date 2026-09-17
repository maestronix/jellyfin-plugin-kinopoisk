using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Kinopoisk.ExternalIds
{
    /// <summary>
    /// Kinopoisk external id for movies.
    /// </summary>
    public sealed class KinopoiskMovieExternalId : IExternalId
    {
        public string ProviderName => Constants.ProviderName;

        public string Key => Constants.ProviderId;

        public ExternalIdMediaType? Type => ExternalIdMediaType.Movie;

        public bool Supports(IHasProviderIds item) => item is Movie;
    }
}
