using Poushec.UpdateCatalogParser;
using Poushec.UpdateCatalogParser.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace UpdateCatalogParser.Tests
{
    public class CatalogSearchTest
    {
        [Trait("Catalog Search", "Tests for Catalog Search queries")]
        [Theory(DisplayName = "SendSearchQueryAsync Returns Correct Results Count ")]
        [InlineData("SQL Server 2012")] // SQL2012 reached EoL, so I don't expect new updates to appear someday 
        public async Task Send_Search_Query_Returns_Correct_Results_Count(string searchQuery)
        {
            var catalogClient = new CatalogClient();
            List<CatalogSearchResult> searchResults = await catalogClient.SendSearchQueryAsync(searchQuery);

            Assert.NotNull(searchResults);
            Assert.Equal(49, searchResults.Count);
        }

        [Trait("Catalog Search", "Tests for Catalog Search queries")]
        [Theory(DisplayName = "GetFirstPageFromSearchQueryAsync Returns Correct Results Count ")]
        [InlineData("SQL Server 2012")] 
        public async Task Get_First_Page_Returns_Correct_Results_Count(string searchQuery)
        {
            var catalogClient = new CatalogClient();
            CatalogResponse firstPage = await catalogClient.GetFirstPageFromSearchQueryAsync(searchQuery);

            Assert.NotNull(firstPage);
            Assert.Equal(49, firstPage.ResultsCount);
        }
    }
}
