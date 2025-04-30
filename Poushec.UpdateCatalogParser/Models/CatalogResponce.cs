using System;
using System.Collections.Generic;

namespace Poushec.UpdateCatalogParser.Models
{
    public class CatalogResponse
    {
        internal string SearchQueryUri;

        internal int TotalPages => (int)Math.Ceiling((double)ResultsCount / 25) - 1; 
        internal readonly int CurrentPage;
        internal string SortQuery = string.Empty; 

        public bool FinalPage => CurrentPage == TotalPages;
        public List<CatalogSearchResult> SearchResults;
        public readonly int ResultsCount;

        internal CatalogResponse(
            string searchQueryUri,
            List<CatalogSearchResult> searchResults, 
            int resultsCount,
            int currentPage
        ) 
        {
            SearchQueryUri = searchQueryUri;

            this.SearchResults = searchResults;
            this.ResultsCount = resultsCount;
            this.CurrentPage = currentPage;
        }
    }
}