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
        internal readonly bool _finalPage;

        public List<CatalogSearchResult> SearchResults;
        public int ResultsCount;

        public CatalogResponse() { }

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
            this._finalPage = finalPage;

            this.ResultsCount = resultsCount;
        }
    }
}