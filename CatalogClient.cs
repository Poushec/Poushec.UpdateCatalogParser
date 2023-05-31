using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;
using System.Linq;
using Poushec.UpdateCatalogParser.Models;
using Poushec.UpdateCatalogParser.Exceptions;
using static System.Web.HttpUtility;
using Poushec.UpdateCatalogParser.Enums;

namespace Poushec.UpdateCatalogParser
{
    /// <summary>
    /// Class that handles all communications with catalog.update.microsoft.com
    /// </summary>
    public class CatalogClient
    {
        private byte _pageReloadAttempts;
        internal HttpClient _client;

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
        /// (Optional)
        /// TRUE - founded updates that have the same Title and SizeInBytes
        /// fields as any of already founded updates will be ignored.
        /// FALSE - collects every founded update.
        /// </param>
        /// <param name="sortBy">
        /// (Optional)
        /// Use this argument if you want Catalog to sort search results.
        /// Available values are the same as in catalog: Title, Products, Classification, LastUpdated, Version, Size 
        /// By default results are sorted by LastUpdated
        /// </param>
        /// <param name="sortDirection">Sorting direction. Ascending or Descending</param>
        /// <returns>List of objects derived from UpdateBase class (Update or Driver)</returns>
        public async Task<List<CatalogSearchResult>> SendSearchQueryAsync(
            string Query, 
            bool ignoreDuplicates = true, 
            SortBy sortBy = SortBy.None, 
            SortDirection sortDirection = SortDirection.Descending
        )
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

            if (sortBy is not SortBy.None)
            {
                // This will sort results in the ascending order
                lastCatalogResponse = await _sortSearchResults(Query, lastCatalogResponse, sortBy);
            
                if (sortDirection is SortDirection.Descending)
                {
                    // The only way to sort results in the descending order is to send the same request again 
                    lastCatalogResponse = await _sortSearchResults(Query, lastCatalogResponse, sortBy);
                }
            }
            
            List<CatalogSearchResult> searchResults = lastCatalogResponse.SearchResults;
            pageReloadAttemptsLeft = _pageReloadAttempts;
            
            while (!lastCatalogResponse.FinalPage)
            {
                if (pageReloadAttemptsLeft == 0)
                {
                    throw new CatalogErrorException($"One of the search result pages was not successfully loaded after {_pageReloadAttempts} attempts to refresh it");
                }

                try
                {
                    lastCatalogResponse = await lastCatalogResponse.ParseNextPageAsync();
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

            if (ignoreDuplicates)
            {
                return searchResults.DistinctBy(result => (result.SizeInBytes, result.Title)).ToList();
            }

            return searchResults;
        }

        /// <summary>
        /// Sends search query to catalog.update.microsoft.com and returns a CatalogResponse
        /// object representing the first results page. Other pages can be requested later by
        /// calling CatalogResponse.ParseNextPageAsync method
        /// </summary>
        /// <param name="Query">Search Query</param>
        /// <param name="sortBy">
        /// (Optional)
        /// Use this argument if you want Catalog to sort search results.
        /// Available values are the same as in catalog: Title, Products, Classification, LastUpdated, Version, Size 
        /// By default results are sorted by LastUpdated
        /// </param>
        /// <param name="sortDirection">Sorting direction. Ascending or Descending</param>
        /// <returns>CatalogResponse object representing the first results page</returns>
        public async Task<CatalogResponse> GetFirstPageFromSearchQueryAsync(
            string Query, 
            SortBy sortBy = SortBy.None, 
            SortDirection sortDirection = SortDirection.Descending
        )
        {
            string catalogBaseUrl = "https://www.catalog.update.microsoft.com/Search.aspx";
            string searchQueryUrl = String.Format($"{catalogBaseUrl}?q={UrlEncode(Query)}"); 
            
            CatalogResponse? catalogFirstPage = null;
            byte pageReloadAttemptsLeft = _pageReloadAttempts;
            
            while (catalogFirstPage is null)
            {
                if (pageReloadAttemptsLeft == 0)
                {
                    throw new CatalogErrorException($"Search results page was not successfully loaded after {_pageReloadAttempts} attempts to refresh it");
                }

                try
                {
                    catalogFirstPage = await _sendSearchQueryAsync(searchQueryUrl);
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
            }

            if (sortBy is not SortBy.None)
            {
                // This will sort results in the ascending order
                catalogFirstPage = await _sortSearchResults(Query, catalogFirstPage, sortBy);
            
                if (sortDirection is SortDirection.Descending)
                {
                    // The only way to sort results in the descending order is to send the same request again 
                    catalogFirstPage = await _sortSearchResults(Query, catalogFirstPage, sortBy);
                }
            }

            return catalogFirstPage;
        }
        
        /// <summary>
        /// Attempts to collect update details from Update Details Page and Download Page 
        /// </summary>
        /// <param name="searchResult">CatalogSearchResult from search query</param>
        /// <returns>Null is request was unsuccessful or UpdateBase (Driver/Update) object with all collected details</returns>
        public async Task<UpdateBase?> TryGetUpdateDetailsAsync(CatalogSearchResult searchResult)
        {
            try
            {
                var update = await GetUpdateDetailsAsync(searchResult);
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
        /// <param name="searchResult">CatalogSearchResult from search query</param>
        /// <returns>Ether Driver of Update object derived from UpdateBase class with all collected details</returns>
        /// <exception cref="UnableToCollectUpdateDetailsException">Thrown when catalog response with an error page or request was unsuccessful</exception>
        /// <exception cref="UpdateWasNotFoundException">Thrown when catalog response with an error page with error code 8DDD0024 (Not found)</exception>
        /// <exception cref="CatalogErrorException">Thrown when catalog response with an error page with unknown error code</exception>
        /// <exception cref="RequestToCatalogTimedOutException">Thrown when request to catalog was canceled due to timeout</exception>
        /// <exception cref="ParseHtmlPageException">Thrown when function was not able to parse ScopedView HTML page</exception>
        public async Task<UpdateBase> GetUpdateDetailsAsync(CatalogSearchResult searchResult)
        {
            var updateBase = new UpdateBase(searchResult);

            byte pageReloadAttemptsLeft = _pageReloadAttempts;

            while (true)
            {
                try 
                {
                    await updateBase.ParseCommonDetails(_client);
                    break;
                }
                catch (Exception ex)
                {
                    pageReloadAttemptsLeft--;

                    if (pageReloadAttemptsLeft == 0)
                    {
                        throw new UnableToCollectUpdateDetailsException($"Failed to properly parse update details page after {_pageReloadAttempts} attempts", ex);
                    }
                }
                
            }

            if (updateBase.Classification.Contains("Driver"))
            {
                var driverUpdate = new Driver(updateBase);

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
                    return update;

                default: throw new NotImplementedException();
            }
        }
        
        private async Task<CatalogResponse> _sortSearchResults(string searchQuery, CatalogResponse unsortedResponse, SortBy sortBy)
        {
            string eventTarget = sortBy switch 
            {
                SortBy.Title =>             "ctl00$catalogBody$updateMatches$ctl02$titleHeaderLink",
                SortBy.Products =>          "ctl00$catalogBody$updateMatches$ctl02$productsHeaderLink",
                SortBy.Classification =>    "ctl00$catalogBody$updateMatches$ctl02$classHeaderLink",
                SortBy.LastUpdated =>       "ctl00$catalogBody$updateMatches$ctl02$dateHeaderLink",
                SortBy.Version =>           "ctl00$catalogBody$updateMatches$ctl02$versionHeaderLink",
                SortBy.Size =>              "ctl00$catalogBody$updateMatches$ctl02$sizeHeaderLink",
                _ => throw new NotImplementedException("Failed to sort search results. Unknown sortBy value")
            };

            var formData = new Dictionary<string, string>() 
            {
                { "__EVENTTARGET",          eventTarget },
                { "__EVENTARGUMENT",        unsortedResponse.EventArgument },
                { "__VIEWSTATE",            unsortedResponse.ViewState },
                { "__VIEWSTATEGENERATOR",   unsortedResponse.ViewStateGenerator },
                { "__EVENTVALIDATION",      unsortedResponse.EventValidation },
                { "ctl00$searchTextBox",    searchQuery }
            };

            var requestContent = new FormUrlEncodedContent(formData); 

            HttpResponseMessage response = await _client.PostAsync(unsortedResponse.SearchQueryUri, requestContent);
            response.EnsureSuccessStatusCode();
            
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.Load(await response.Content.ReadAsStreamAsync());

            return CatalogResponse.ParseFromHtmlPage(HtmlDoc, _client, unsortedResponse.SearchQueryUri);
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

            return CatalogResponse.ParseFromHtmlPage(HtmlDoc, _client, requestUri);
        }
    }
}
