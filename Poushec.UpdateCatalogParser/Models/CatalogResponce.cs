using System;
using System.Collections.Generic;

namespace Poushec.UpdateCatalogParser.Models
{
    public class CatalogResponse
    {
        internal string SearchQueryUri;
        internal string EventArgument;
        internal string EventValidation;
        internal string ViewState;
        internal string ViewStateGenerator;

        internal int TotalPages => (int)Math.Ceiling((double)ResultsCount / 25);

        public readonly bool FinalPage;
        public List<CatalogSearchResult> SearchResults;
        public int ResultsCount;

        internal CatalogResponse(
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