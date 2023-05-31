using HtmlAgilityPack;
using Poushec.UpdateCatalogParser.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Poushec.UpdateCatalogParser.Models
{
    public class CatalogResponse
    {
        private HttpClient _client;
        private HtmlNode? _nextPage; 
        
        internal string SearchQueryUri;
        internal string EventArgument;
        internal string EventValidation;
        internal string ViewState;
        internal string ViewStateGenerator;

        public List<CatalogSearchResult> SearchResults;
        public int ResultsCount;
        public bool FinalPage => _nextPage is null;

        private CatalogResponse(
            HttpClient client,
            string searchQueryUri,
            List<CatalogSearchResult> searchResults, 
            string eventArgument, 
            string eventValidation,
            string viewState,
            string viewStateGenerator,
            HtmlNode nextPage,
            int resultsCount
        ) 
        {
            _client = client;
            SearchQueryUri = searchQueryUri;

            this.SearchResults = searchResults;
            this.EventArgument = eventArgument;
            this.EventValidation = eventValidation;
            this.ViewState = viewState;
            this.ViewStateGenerator = viewStateGenerator;
            this._nextPage = nextPage;
            this.ResultsCount = resultsCount;
        }

        /// <summary>
        /// Loads and parses the next page of the search results. If this method is called 
        /// on a final page - CatalogNoResultsException will be thrown
        /// </summary>
        /// <returns>CatalogResponse object representing search query results from the next page</returns>
        public async Task<CatalogResponse> ParseNextPageAsync()
        {
            if (FinalPage)
            {
                throw new CatalogNoResultsException("No more search results available. This is a final page.");
            }

            var formData = new Dictionary<string, string>() 
            {
                { "__EVENTTARGET",          "ctl00$catalogBody$nextPageLinkText" },
                { "__EVENTARGUMENT",        EventArgument },
                { "__VIEWSTATE",            ViewState },
                { "__VIEWSTATEGENERATOR",   ViewStateGenerator },
                { "__EVENTVALIDATION",      EventValidation }
            };

            var requestContent = new FormUrlEncodedContent(formData); 

            HttpResponseMessage response = await _client.PostAsync(SearchQueryUri, requestContent);
            response.EnsureSuccessStatusCode();
            
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.Load(await response.Content.ReadAsStreamAsync());

            return CatalogResponse.ParseFromHtmlPage(HtmlDoc, _client, SearchQueryUri);
        }

        internal static CatalogResponse ParseFromHtmlPage(HtmlDocument htmlDoc, HttpClient client, string searchQueryUri)
        {
            string eventArgument = htmlDoc.GetElementbyId("__EVENTARGUMENT")?.FirstChild?.Attributes["value"]?.Value ?? String.Empty;
            string eventValidation = htmlDoc.GetElementbyId("__EVENTVALIDATION").GetAttributes().Where(att => att.Name == "value").First().Value;
            string viewState = htmlDoc.GetElementbyId("__VIEWSTATE").GetAttributes().Where(att => att.Name == "value").First().Value;
            string viewStateGenerator = htmlDoc.GetElementbyId("__VIEWSTATEGENERATOR").GetAttributes().Where(att => att.Name == "value").First().Value;
            HtmlNode nextPage = htmlDoc.GetElementbyId("ctl00_catalogBody_nextPageLinkText");

            string resultsCountString = htmlDoc.GetElementbyId("ctl00_catalogBody_searchDuration").InnerText;
            int resultsCount = int.Parse(Regex.Match(resultsCountString, "(?<=of )\\d{1,4}").Value);

            HtmlNode table = htmlDoc.GetElementbyId("ctl00_catalogBody_updateMatches");

            if (table is null)
            {
                throw new CatalogFailedToLoadSearchResultsPageException("Catalog response does not contains a search results table");
            }

            HtmlNodeCollection searchResultsRows = table.SelectNodes("tr");

            List<CatalogSearchResult> searchResults = new();

            foreach (var resultsRow in searchResultsRows.Skip(1)) // First row is always a headerRow 
            {
                searchResults.Add(CatalogSearchResult.ParseFromResultsTableRow(resultsRow));
            }

            return new CatalogResponse(
                client, 
                searchQueryUri, 
                searchResults, 
                eventArgument, 
                eventValidation, 
                viewState, 
                viewStateGenerator, 
                nextPage, 
                resultsCount
            );
        }
    }
}