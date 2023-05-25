using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;
using System.Linq;
using Poushec.UpdateCatalog.Models;
using Poushec.UpdateCatalog.Exceptions;
using static System.Web.HttpUtility;

namespace Poushec.UpdateCatalog
{
    /// <summary>
    /// Class that handles all communications with catalog.update.microsoft.com
    /// </summary>
    public class CatalogClient
    {
        private byte _pageReloadAttempts;
        private HttpClient _client;

        public CatalogClient(byte pageReloadAttemptsAllowed = 3)
        {
            _client = new HttpClient();
            _pageReloadAttempts = pageReloadAttemptsAllowed;
        }

        public CatalogClient(HttpClient client, byte pageReloadAttemptsAllowed = 3)
        {
            _client = client;
            _pageReloadAttempts = pageReloadAttemptsAllowed;
        }
        
        /// <summary>
        /// Sends search query to catalog.update.microsoft.com
        /// </summary>
        /// <param name="Query">Search Query</param>
        /// <param name="ignoreDuplicates">
        /// TRUE - founded updates that have the same Title and SizeInBytes
        /// fields as any of already founded updates will be ignored.
        /// FALSE - collects every founded update.
        /// </param>
        /// <returns>List of objects derived from UpdateBase class (Update or Driver)</returns>
        public async Task<List<CatalogSearchResult>> SendSearchQueryAsync(string Query, bool ignoreDuplicates = true)
        {
            string catalogBaseUrl = "https://www.catalog.update.microsoft.com/Search.aspx";
            string searchQueryUrl = String.Format($"{catalogBaseUrl}?q={UrlEncode(Query)}"); 
            
            CatalogResponse? lastCatalogResponse = null;
            byte pageReloadAttemptsLeft = _pageReloadAttempts;
            
            while (lastCatalogResponse is null)
            {
                if (pageReloadAttemptsLeft == 0)
                {
                    throw new CatalogErrorException($"Search results page was not successfully loaded after {_pageReloadAttempts} attempts to refresh it");
                }

                try
                {
                    lastCatalogResponse = await _sendSearchQueryAsync(searchQueryUrl);
                }
                catch (TaskCanceledException)
                {
                    // Request timed out - it happens. We'll try to reload a page
                    pageReloadAttemptsLeft--;
                    continue;
                }
                catch (CatalogFailedToLoadSearchResultsPageException)
                {
                    // Sometimes catalog responses with an empty search results table.
                    // Refreshing a page usually helps, so that's what we'll try to do
                    pageReloadAttemptsLeft--;
                    continue;
                }
                catch (CatalogNoResultsException)
                {
                    // Search query returned no results
                    return new List<CatalogSearchResult>();
                }
            }
            
            List<CatalogSearchResult> searchResults = lastCatalogResponse.SearchResults;
            pageReloadAttemptsLeft = _pageReloadAttempts;

            while (lastCatalogResponse.NextPage != null)
            {
                if (pageReloadAttemptsLeft == 0)
                {
                    throw new CatalogErrorException($"One of the search result pages was not successfully loaded after {_pageReloadAttempts} attempts to refresh it");
                }

                try
                {
                    lastCatalogResponse = await _loadNextPageResults(searchQueryUrl, lastCatalogResponse);
                    searchResults.AddRange(lastCatalogResponse.SearchResults);
                    pageReloadAttemptsLeft = _pageReloadAttempts; // Reset page refresh attempts count
                }
                catch (TaskCanceledException) 
                {
                    // Request timed out - it happens
                    pageReloadAttemptsLeft--;
                    continue;
                }
                catch (CatalogFailedToLoadSearchResultsPageException)
                {
                    // Sometimes catalog responses with an empty search results table.
                    // Refreshing a page usually helps, so that's what we'll try to do
                    pageReloadAttemptsLeft--;
                    continue;
                }
            }

            return searchResults;
        }
        
        /// <summary>
        /// Attempts to collect update details from Update Details Page and Download Page 
        /// </summary>
        /// <param name="UpdateID">Update's UpdateID</param>
        /// <returns>Null is request was unsuccessful or UpdateBase (Driver/Update) object with all collected details</returns>
        public async Task<UpdateBase?> TryGetUpdateDetailsAsync(string UpdateID)
        {
            try
            {
                var update = await GetUpdateDetailsAsync(UpdateID);
                return update;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Collect update details from Update Details Page and Download Page 
        /// </summary>
        /// <param name="UpdateID">Update's UpdateID</param>
        /// <returns>Ether Driver of Update object derived from UpdateBase class with all collected details</returns>
        /// <exception cref="UnableToCollectUpdateDetailsException">Thrown when catalog response with an error page or when request was unsuccessful</exception>
        /// <exception cref="UpdateWasNotFoundException">Thrown when catalog response with an error page with error code 8DDD0024 (Not found)</exception>
        /// <exception cref="CatalogErrorException">Thrown when catalog response with an error page with unknown error code</exception>
        /// <exception cref="RequestToCatalogTimedOutException">Thrown when request to catalog was canceled due to timeout</exception>
        /// <exception cref="ParseHtmlPageException">Thrown when function was not able to parse ScopedView HTML page</exception>
        public async Task<UpdateBase> GetUpdateDetailsAsync(string UpdateID)
        {
            var updateBase = new UpdateBase() { UpdateID = UpdateID };
            await updateBase.CollectGenericInfo(_client);

            if (updateBase.Classification.Contains("Driver"))
            {
                var driverUpdate = new Driver(updateBase);
                driverUpdate.CollectDriverDetails();

                return driverUpdate;
            }

            switch (updateBase.Classification)
            {
                case "Security Updates":
                case "Critical Updates":
                case "Definition Updates":
                case "Feature Packs": 
                case "Service Packs":
                case "Update Rollups":
                case "Updates": 
                case "Hotfix":
                    var update = new Update(updateBase);
                    update.CollectUpdateDetails();
                    return update;

                default: throw new NotImplementedException();
            }
        }

        private async Task<CatalogResponse> _sendSearchQueryAsync(string requestUri)
        {
            HttpResponseMessage response = await _client.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();
            
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.Load(await response.Content.ReadAsStreamAsync());

            if (HtmlDoc.GetElementbyId("ctl00_catalogBody_noResultText") is not null)
            {
                throw new CatalogNoResultsException();
            }

            return CatalogResponse.ParseFromHtmlPage(HtmlDoc);
        }

        private async Task<CatalogResponse> _loadNextPageResults(string requestUri, CatalogResponse previousResponse)
        {
            var formData = new Dictionary<string, string>() 
            {
                { "__EVENTTARGET",          "ctl00$catalogBody$nextPageLinkText" },
                { "__EVENTARGUMENT",        previousResponse.EventArgument },
                { "__VIEWSTATE",            previousResponse.ViewState },
                { "__VIEWSTATEGENERATOR",   previousResponse.ViewStateGenerator },
                { "__EVENTVALIDATION",      previousResponse.EventValidation }
            };

            var requestContent = new FormUrlEncodedContent(formData); 

            HttpResponseMessage response = await _client.PostAsync(requestUri, requestContent);
            response.EnsureSuccessStatusCode();
            
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.Load(await response.Content.ReadAsStreamAsync());

            return CatalogResponse.ParseFromHtmlPage(HtmlDoc);
        }
    }
}
