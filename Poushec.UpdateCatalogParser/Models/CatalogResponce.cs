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
        private HtmlNode _nextPage; 
        
        internal string SearchQueryUri;
        internal string EventArgument;
        internal string EventValidation;
        internal string ViewState;
        internal string ViewStateGenerator;

        public List<CatalogSearchResult> SearchResults;
        public int ResultsCount;
        public readonly bool FinalPage;

        internal CatalogResponse(
            HttpClient client,
            string searchQueryUri,
            List<CatalogSearchResult> searchResults, 
            string eventArgument, 
            string eventValidation,
            string viewState,
            string viewStateGenerator,
            bool finalPage,
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
            this.FinalPage = finalPage;
            this.ResultsCount = resultsCount;
        }
    }
}