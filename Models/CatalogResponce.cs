using HtmlAgilityPack;
using Poushec.UpdateCatalog.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Poushec.UpdateCatalog.Models
{
    internal class CatalogResponse
    {
        public List<CatalogSearchResult> SearchResults;
        public string EventArgument;
        public string EventValidation;
        public string ViewState;
        public string ViewStateGenerator;
        public HtmlNode NextPage; 

        private CatalogResponse(
            List<CatalogSearchResult> searchResults, 
            string eventArgument, 
            string eventValidation,
            string viewState,
            string viewStateGenerator,
            HtmlNode nextPage
        ) 
        {
            this.SearchResults = searchResults;
            this.EventArgument = eventArgument;
            this.EventValidation = eventValidation;
            this.ViewState = viewState;
            this.ViewStateGenerator = viewStateGenerator;
            this.NextPage = nextPage;
        }

        public static CatalogResponse ParseFromHtmlPage(HtmlDocument htmlDoc)
        {
            string eventArgument = htmlDoc.GetElementbyId("__EVENTARGUMENT")?.FirstChild?.Attributes["value"]?.Value ?? String.Empty;
            string eventValidation = htmlDoc.GetElementbyId("__EVENTVALIDATION").GetAttributes().Where(att => att.Name == "value").First().Value;
            string viewState = htmlDoc.GetElementbyId("__VIEWSTATE").GetAttributes().Where(att => att.Name == "value").First().Value;
            string viewStateGenerator = htmlDoc.GetElementbyId("__VIEWSTATEGENERATOR").GetAttributes().Where(att => att.Name == "value").First().Value;
            HtmlNode nextPage = htmlDoc.GetElementbyId("ctl00_catalogBody_nextPageLinkText");

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

            return new CatalogResponse(searchResults, eventArgument, eventValidation, viewState, viewStateGenerator, nextPage);
        }
    }
}