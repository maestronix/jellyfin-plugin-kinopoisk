using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Kinopoisk.ExternalIds
{
    /// <summary>
    /// Kinopoisk external id for people.
    /// </summary>
    public sealed class KinopoiskPersonExternalId : IExternalId
    {
        public string ProviderName => Constants.ProviderName;

        public string Key => Constants.ProviderId;

        public ExternalIdMediaType? Type => ExternalIdMediaType.Person;

        public bool Supports(IHasProviderIds item) => item is Person;
    }
}
