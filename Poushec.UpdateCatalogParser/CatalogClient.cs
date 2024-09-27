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
using System.Threading;
using System.Globalization;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogClient"/> class using the specified <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="cultureInfo">The culture information to use for parsing catalog Dates.</param>
        /// <remarks>
        /// This constructor creates a new instance of <see cref="HttpClient"/> internally and initializes the <see cref="CatalogClient"/>
        /// with the provided culture information.
        /// The default page reload attempts is 3.
        /// </remarks>
        public CatalogClient(CultureInfo cultureInfo) : this(new HttpClient(), cultureInfo) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogClient"/> class with a default culture ("en-US")
        /// and a specified number of allowed page reload attempts.
        /// </summary>
        /// <param name="pageReloadAttemptsAllowed">The number of page reload attempts allowed. Default is 3.</param>
        /// <remarks>
        /// This constructor creates a new instance of <see cref="HttpClient"/> and uses a default culture of "en-US".
        /// It also allows you to specify the maximum number of page reload attempts allowed.
        /// </remarks>
        public CatalogClient(byte pageReloadAttemptsAllowed = 3) : this(new HttpClient(), new CultureInfo("en-US"), pageReloadAttemptsAllowed) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogClient"/> class with the specified <see cref="HttpClient"/>, <see cref="CultureInfo"/>,
        /// and allowed page reload attempts.
        /// </summary>
        /// <param name="client">An instance of <see cref="HttpClient"/> to be used for making HTTP requests.</param>
        /// <param name="cultureInfo">The culture information to use for parsing catalog data.</param>
        /// <param name="pageReloadAttemptsAllowed">The number of page reload attempts allowed. Default is 3.</param>
        /// <remarks>
        /// This constructor allows full control over the HTTP client used for requests, the culture settings, and the number of page reload attempts allowed.
        /// </remarks>
        public CatalogClient(HttpClient client, CultureInfo cultureInfo, byte pageReloadAttemptsAllowed = 3)
        {
            _client = client;
            _pageReloadAttempts = pageReloadAttemptsAllowed;
            _catalogParser = new CatalogParser(client, cultureInfo);
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
            SortDirection sortDirection = SortDirection.Descending,
            CancellationToken cancellationToken = default
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
                    lastCatalogResponse = await InternalSendSearchQueryAsync(searchQueryUrl, cancellationToken);
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
                lastCatalogResponse = await SortSearchResultsAsync(Query, lastCatalogResponse, sortBy, cancellationToken);
            
                if (sortDirection is SortDirection.Descending)
                {
                    // The only way to sort results in the descending order is to send the same request again 
                    lastCatalogResponse = await SortSearchResultsAsync(Query, lastCatalogResponse, sortBy, cancellationToken);
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
            SortDirection sortDirection = SortDirection.Descending,
            CancellationToken cancellationToken = default
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
                    catalogFirstPage = await InternalSendSearchQueryAsync(searchQueryUrl, cancellationToken);
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
                catalogFirstPage = await SortSearchResultsAsync(Query, catalogFirstPage, sortBy);
            
                if (sortDirection is SortDirection.Descending)
                {
                    // The only way to sort results in the descending order is to send the same request again 
                    catalogFirstPage = await SortSearchResultsAsync(Query, catalogFirstPage, sortBy);
                }
            }

            return catalogFirstPage;
        }

        /// <summary>
        /// Loads and parses the next page of the search results. If this method is called 
        /// on a final page - <see cref="CatalogNoResultsException"/> will be thrown
        /// </summary>
        /// <returns><see cref="CatalogResponse"/> object representing search query results from the next page</returns>
        public async Task<CatalogResponse> ParseNextPageAsync(CatalogResponse currentPage, CancellationToken cancellationToken = default)
        {
            if (currentPage.FinalPage)
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

            HttpResponseMessage response = await _client.PostAsync(currentPage._searchQueryUri, requestContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.Load(await response.Content.ReadAsStreamAsync());

            return _catalogParser.ParseSearchResultsPage(HtmlDoc, currentPage._searchQueryUri);
        }
        
        
        /// <summary>
        /// Attempts to collect update details from Update Details Page and Download Page 
        /// </summary>
        /// <param name="searchResult">CatalogSearchResult from search query</param>
        /// <returns>Null if request was unsuccessful or <see cref="UpdateInfo"/> object with all collected details</returns>
        public async Task<UpdateInfo> TryGetUpdateDetailsAsync(CatalogSearchResult searchResult, CancellationToken cancellationToken = default)
        {
            try
            {
                var update = await GetUpdateDetailsAsync(searchResult, cancellationToken);
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
        /// <returns><see cref="UpdateInfo"/> object representing all collected details on the given update</returns>
        /// <exception cref="UnableToCollectUpdateDetailsException">Thrown when catalog response with an error page or request was unsuccessful</exception>
        /// <exception cref="UpdateWasNotFoundException">Thrown when catalog response with an error page with error code 8DDD0024 (Not found)</exception>
        /// <exception cref="CatalogErrorException">Thrown when catalog response with an error page with unknown error code</exception>
        /// <exception cref="RequestToCatalogTimedOutException">Thrown when request to catalog was canceled due to timeout</exception>
        /// <exception cref="ParseHtmlPageException">Thrown when function was not able to parse ScopedView HTML page</exception>
        public async Task<UpdateInfo> GetUpdateDetailsAsync(CatalogSearchResult searchResult, CancellationToken cancellationToken = default)
        {
            HtmlDocument detailsPage;
            byte pageReloadAttemptsLeft = _pageReloadAttempts;

            while (true)
            {
                try 
                {
                    detailsPage = await _catalogParser.LoadDetailsPageAsync(searchResult.UpdateID, cancellationToken);
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

            UpdateInfo updateBase = _catalogParser.CollectUpdateInfoFromDetailsPage(searchResult, detailsPage);

            IEnumerable<string> patchDownloadLinks = await _catalogParser.GetDownloadLinksAsync(searchResult.UpdateID, cancellationToken);
            updateBase.DownloadLinks.AddRange(patchDownloadLinks);

            if (updateBase.Classification.Contains("Driver"))
            {
                updateBase.DriverProperties = _catalogParser.CollectDriverProperties(detailsPage);
            }
            else
            {
                updateBase.AdditionalProperties = _catalogParser.CollectAdditionalUpdateProperties(detailsPage);
            }

            return updateBase;
        }
        
        private async Task<CatalogResponse> SortSearchResultsAsync(
            string searchQuery, 
            CatalogResponse unsortedResponse, 
            SortBy sortBy, 
            CancellationToken cancellationToken = default
        )
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

            HttpResponseMessage response = await _client.PostAsync(unsortedResponse._searchQueryUri, requestContent, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.Load(await response.Content.ReadAsStreamAsync());

            return _catalogParser.ParseSearchResultsPage(HtmlDoc, unsortedResponse._searchQueryUri);
        }

        private async Task<CatalogResponse> InternalSendSearchQueryAsync(string requestUri, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _client.GetAsync(requestUri, cancellationToken);
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
