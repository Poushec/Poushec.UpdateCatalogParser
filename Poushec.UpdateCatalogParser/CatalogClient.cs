using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;
using System.Linq;
using Poushec.UpdateCatalogParser.Models;
using Poushec.UpdateCatalogParser.Exceptions;
using static System.Web.HttpUtility;
using Poushec.UpdateCatalogParser.Extensions;
using Poushec.UpdateCatalogParser.Parsers;

namespace Poushec.UpdateCatalogParser
{
    /// <summary>
    /// Class that handles all communications with catalog.update.microsoft.com
    /// </summary>
    public class CatalogClient
    {
        private byte _pageReloadAttempts;
        private HttpClient _client;
        private CatalogParser _catalogParser;

        public CatalogClient(byte pageReloadAttemptsAllowed = 3) : this(new HttpClient(), pageReloadAttemptsAllowed) { }

        public CatalogClient(HttpClient client, byte pageReloadAttemptsAllowed = 3)
        {
            _client = client;
            _pageReloadAttempts = pageReloadAttemptsAllowed;
            _catalogParser = new CatalogParser(client);
        }
        
        /// <summary>
        /// Sends search query to <see href="https://catalog.update.microsoft.com">catalog.update.microsoft.com</see>
        /// </summary>
        /// <param name="Query">Search Query</param>
        /// <param name="ignoreDuplicates">
        /// (Optional) Excludes updates with the same Title and SizeInBytes values from the search results. 
        /// False by default. 
        /// </param>
        /// <param name="sortBy">(Optional) If provided, client will send additional request to the catalog for it to search the results list.</param>
        /// <param name="sortDirection">(Optional) Sets the sort direction</param>
        /// <returns><see cref="CatalogSearchResult"/> list representing the search results</returns>
        public async Task<List<CatalogSearchResult>> SendSearchQueryAsync(
            string Query, 
            bool ignoreDuplicates = false, 
            SortBy sortBy = SortBy.None, 
            SortDirection sortDirection = SortDirection.Descending
        )
        {
            string catalogBaseUrl = "https://www.catalog.update.microsoft.com/Search.aspx";
            string searchQueryUrl = String.Format($"{catalogBaseUrl}?q={UrlEncode(Query)}");

            CatalogResponse lastCatalogResponse = null;
            byte pageReloadAttemptsLeft = _pageReloadAttempts;
            
            while (lastCatalogResponse is null)
            {
                if (pageReloadAttemptsLeft == 0)
                {
                    throw new CatalogErrorException($"Search results page was not successfully loaded after {_pageReloadAttempts} attempts to refresh it");
                }

                try
                {
                    lastCatalogResponse = await SendSearchQueryAsync(searchQueryUrl);
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

            if (sortBy != SortBy.None)
            {
                // This will sort results in the ascending order
                lastCatalogResponse = await SortSearchResults(Query, lastCatalogResponse, sortBy);
            
                if (sortDirection is SortDirection.Descending)
                {
                    // The only way to sort results in the descending order is to send the same request again 
                    lastCatalogResponse = await SortSearchResults(Query, lastCatalogResponse, sortBy);
                }
            }
            
            List<CatalogSearchResult> searchResults = lastCatalogResponse.SearchResults;
            pageReloadAttemptsLeft = _pageReloadAttempts;
            
            while (!lastCatalogResponse._finalPage)
            {
                if (pageReloadAttemptsLeft == 0)
                {
                    throw new CatalogErrorException($"One of the search result pages was not successfully loaded after {_pageReloadAttempts} attempts to refresh it");
                }

                try
                {
                    lastCatalogResponse = await ParseNextPageAsync(lastCatalogResponse);
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
        /// Sends search query to <seealso cref="https://catalog.update.microsoft.com"/> and returns a CatalogResponse
        /// object representing the first results page. Other pages can be requested later by
        /// calling <see cref="ParseNextPageAsync"/> method
        /// </summary>
        /// <param name="Query">Search Query</param>
        /// <param name="sortBy">
        /// (Optional)
        /// Use this argument if you want Catalog to sort search results.
        /// Available values are the same as in catalog: Title, Products, Classification, LastUpdated, Version, Size 
        /// By default results are sorted by LastUpdated
        /// </param>
        /// <param name="sortDirection">Sorting direction. <see cref="SortDirection.Ascending">Ascending</see> or <see cref="SortDirection.Descending">Descending</see></param>
        /// <returns><see cref="CatalogResponse"/> object representing the first results page</returns>
        public async Task<CatalogResponse> GetFirstPageFromSearchQueryAsync(
            string Query, 
            SortBy sortBy = SortBy.None, 
            SortDirection sortDirection = SortDirection.Descending
        )
        {
            string catalogBaseUrl = "https://www.catalog.update.microsoft.com/Search.aspx";
            string searchQueryUrl = String.Format($"{catalogBaseUrl}?q={UrlEncode(Query)}"); 
            
            CatalogResponse catalogFirstPage = null;
            byte pageReloadAttemptsLeft = _pageReloadAttempts;
            
            while (catalogFirstPage is null)
            {
                if (pageReloadAttemptsLeft == 0)
                {
                    throw new CatalogErrorException($"Search results page was not successfully loaded after {_pageReloadAttempts} attempts to refresh it");
                }

                try
                {
                    catalogFirstPage = await SendSearchQueryAsync(searchQueryUrl);
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

            if (sortBy != SortBy.None)
            {
                // This will sort results in the ascending order
                catalogFirstPage = await SortSearchResults(Query, catalogFirstPage, sortBy);
            
                if (sortDirection is SortDirection.Descending)
                {
                    // The only way to sort results in the descending order is to send the same request again 
                    catalogFirstPage = await SortSearchResults(Query, catalogFirstPage, sortBy);
                }
            }

            return catalogFirstPage;
        }

        /// <summary>
        /// Loads and parses the next page of the search results. If this method is called 
        /// on a final page - <see cref="CatalogNoResultsException"/> will be thrown
        /// </summary>
        /// <returns><see cref="CatalogResponse"/> object representing search query results from the next page</returns>
        public async Task<CatalogResponse> ParseNextPageAsync(CatalogResponse currentPage)
        {
            if (currentPage._finalPage)
            {
                throw new CatalogNoResultsException("No more search results available. This is a final page.");
            }

            var formData = new Dictionary<string, string>() 
            {
                { "__EVENTTARGET",          "ctl00$catalogBody$nextPageLinkText" },
                { "__EVENTARGUMENT",        currentPage._eventArgument },
                { "__VIEWSTATE",            currentPage._viewState },
                { "__VIEWSTATEGENERATOR",   currentPage._viewStateGenerator },
                { "__EVENTVALIDATION",      currentPage._eventValidation }
            };

            var requestContent = new FormUrlEncodedContent(formData); 

            HttpResponseMessage response = await _client.PostAsync(currentPage._searchQueryUri, requestContent);
            response.EnsureSuccessStatusCode();
            
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.Load(await response.Content.ReadAsStreamAsync());

            return _catalogParser.ParseSearchResultsPage(HtmlDoc, currentPage._searchQueryUri);
        }
        
        
        /// <summary>
        /// Attempts to collect update details from Update Details Page and Download Page 
        /// </summary>
        /// <param name="searchResult">CatalogSearchResult from search query</param>
        /// <returns>Null is request was unsuccessful or UpdateBase (Driver/Update) object with all collected details</returns>
        public async Task<UpdateBase> TryGetUpdateDetailsAsync(CatalogSearchResult searchResult)
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
            HtmlDocument detailsPage;
            byte pageReloadAttemptsLeft = _pageReloadAttempts;

            while (true)
            {
                try 
                {
                    detailsPage = await _catalogParser.LoadDetailsPageAsync(searchResult.UpdateID);
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

            UpdateBase updateBase = _catalogParser.CollectUpdateInfoFromDetailsPage(searchResult, detailsPage);

            if (updateBase.Classification.Contains("Driver"))
            {
                var driverUpdate = new DriverProperties(updateBase);

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
                    var update = new AdditionalProperties(updateBase);
                    return update;

                default: throw new NotImplementedException();
            }
        }
        
        private async Task<CatalogResponse> SortSearchResults(string searchQuery, CatalogResponse unsortedResponse, SortBy sortBy)
        {
            string eventTarget = "ctl00$catalogBody$updateMatches$ctl02$";

            switch (sortBy)
            {
                case SortBy.Title:           eventTarget += "titleHeaderLink"; break;
                case SortBy.Products:        eventTarget += "productsHeaderLink"; break;
                case SortBy.Classification:  eventTarget += "classHeaderLink"; break;
                case SortBy.LastUpdated:     eventTarget += "dateHeaderLink"; break;
                case SortBy.Version:         eventTarget += "versionHeaderLink"; break;
                case SortBy.Size:            eventTarget += "sizeHeaderLink"; break;
            }

            var formData = new Dictionary<string, string>() 
            {
                { "__EVENTTARGET",          eventTarget },
                { "__EVENTARGUMENT",        unsortedResponse._eventArgument },
                { "__VIEWSTATE",            unsortedResponse._viewState },
                { "__VIEWSTATEGENERATOR",   unsortedResponse._viewStateGenerator },
                { "__EVENTVALIDATION",      unsortedResponse._eventValidation },
                { "ctl00$searchTextBox",    searchQuery }
            };

            var requestContent = new FormUrlEncodedContent(formData); 

            HttpResponseMessage response = await _client.PostAsync(unsortedResponse._searchQueryUri, requestContent);
            response.EnsureSuccessStatusCode();
            
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.Load(await response.Content.ReadAsStreamAsync());

            return _catalogParser.ParseSearchResultsPage(HtmlDoc, unsortedResponse._searchQueryUri);
        }

        private async Task<CatalogResponse> SendSearchQueryAsync(string requestUri)
        {
            HttpResponseMessage response = await _client.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();
            
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.Load(await response.Content.ReadAsStreamAsync());

            if (HtmlDoc.GetElementbyId("ctl00_catalogBody_noResultText") != null)
            {
                throw new CatalogNoResultsException();
            }

            return _catalogParser.ParseSearchResultsPage(HtmlDoc, requestUri);
        }
    }
}
