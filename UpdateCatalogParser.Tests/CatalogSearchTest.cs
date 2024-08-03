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

        [Trait("Catalog Search", "Tests for Catalog Search queries")]
        [Theory(DisplayName = "GetFirstPageFromSearchQueryAsync and Iterate through pages works correctly")]
        [InlineData("SQL Server 2012")]
        public async Task Get_First_Page_And_Iterate_To_End_Works_Correctly(string searchQuery)
        {
            var catalogClient = new CatalogClient();
            CatalogResponse currentPage = await catalogClient.GetFirstPageFromSearchQueryAsync(searchQuery);

            Assert.NotNull(currentPage);
            Assert.NotEmpty(currentPage.SearchResults);

            var allSearchResults = currentPage.SearchResults;

            while (!currentPage.FinalPage)
            {
                currentPage = await catalogClient.ParseNextPageAsync(currentPage);

                Assert.NotNull(currentPage);
                Assert.NotEmpty(currentPage.SearchResults);

                allSearchResults.AddRange(currentPage.SearchResults);
            }

            Assert.Equal(49, allSearchResults.Count);
        }

        [Trait("Catalog Search", "Tests for Catalog Search queries")]
        [Theory(DisplayName = "GetUpdateDetailsAsync method returns correct results for Update")]
        [InlineData("7b3ed3ca-5220-4325-b034-cedc5cb51253")]
        public async Task Get_Update_Details_Method_Returns_Correct_Results_For_Update(string updateId)
        {
            var catalogClient = new CatalogClient();
            List<CatalogSearchResult> searchResults = await catalogClient.SendSearchQueryAsync(updateId);

            Assert.NotNull(searchResults);
            Assert.NotEmpty(searchResults);

            UpdateInfo updateDetails = await catalogClient.GetUpdateDetailsAsync(searchResults[0]);
            
            Assert.NotNull(updateDetails);
            Assert.NotNull(updateDetails.AdditionalProperties);

            Assert.NotEmpty(updateDetails.DownloadLinks);
            Assert.NotEmpty(updateDetails.AdditionalProperties.SupersededBy);
            Assert.NotEmpty(updateDetails.AdditionalProperties.Supersedes);

            bool allLinksAreNotEmpty = updateDetails.DownloadLinks.TrueForAll(link => !string.IsNullOrEmpty(link));

            Assert.True(allLinksAreNotEmpty);
        }

        [Trait("Catalog Search", "Tests for Catalog Search queries")]
        [Theory(DisplayName = "GetUpdateDetailsAsync method returns correct results for Driver")]
        [InlineData("97f6087b-1190-4e0f-9234-352bcbf520ad")]
        public async Task Get_Update_Details_Method_Returns_Correct_Results_For_Driver(string updateId)
        {
            var catalogClient = new CatalogClient();
            List<CatalogSearchResult> searchResults = await catalogClient.SendSearchQueryAsync(updateId);

            Assert.NotNull(searchResults);
            Assert.NotEmpty(searchResults);

            UpdateInfo updateDetails = await catalogClient.GetUpdateDetailsAsync(searchResults[0]);
            
            Assert.NotNull(updateDetails);
            Assert.NotNull(updateDetails.DriverProperties);

            Assert.NotEmpty(updateDetails.DownloadLinks);
            Assert.NotEmpty(updateDetails.DriverProperties.HardwareIDs);

            bool allLinksAreNotEmpty = updateDetails.DownloadLinks.TrueForAll(link => !string.IsNullOrEmpty(link));

            Assert.True(allLinksAreNotEmpty);
        }
    }
}
