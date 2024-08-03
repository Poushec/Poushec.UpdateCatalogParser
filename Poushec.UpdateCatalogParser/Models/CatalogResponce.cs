using System.Collections.Generic;

namespace Poushec.UpdateCatalogParser.Models
{
    public class CatalogResponse
    {
        internal string _searchQueryUri;
        internal string _eventArgument;
        internal string _eventValidation;
        internal string _viewState;
        internal string _viewStateGenerator;

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
            _searchQueryUri = searchQueryUri;

            this.SearchResults = searchResults;
            this._eventArgument = eventArgument;
            this._eventValidation = eventValidation;
            this._viewState = viewState;
            this._viewStateGenerator = viewStateGenerator;
            this.FinalPage = finalPage;

            this.ResultsCount = resultsCount;
        }
    }
}