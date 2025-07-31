using Microsoft.AspNetCore.Http;

namespace Aevatar.Workshop.Client.Services
{
    /// <summary>
    /// Interface for localization service to support multi-language functionality
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// Get current language from request context
        /// </summary>
        string GetCurrentLanguage(HttpContext context);
        
        /// <summary>
        /// Get localized text by key
        /// </summary>
        string GetText(string key, string language = null);
        
        /// <summary>
        /// Get localized text with parameters
        /// </summary>
        string GetText(string key, object[] parameters, string language = null);
        
        /// <summary>
        /// Set current language for the request
        /// </summary>
        void SetCurrentLanguage(string language);
    }
} 