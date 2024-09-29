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

        [Trait("Catalog Search", "Tests for Catalog Search queries")]
        [Theory(DisplayName = "SendSearchQueryAsync throws a FormatException with incorrect CultureInfo")]
        [InlineData("97f6087b-1190-4e0f-9234-352bcbf520ad")]
        public async Task Throws_FormatException_With_Incorrect_CultureInfo(string updateId)
        {
            var catalogClient = new CatalogClient(new System.Globalization.CultureInfo("uk-UA"));

            await Assert.ThrowsAsync<System.FormatException>(async () =>
            {
                await catalogClient.SendSearchQueryAsync(updateId);
            });
        }
        

        [Trait("Catalog Search", "Tests for Catalog Search queries")]
        [Theory(DisplayName = "Download Links are being parsed correctly (catalog.s.download)")]
        [InlineData("6dfce7b5-4ca2-4ebd-ae75-14918abb529d")]
        public async Task Download_Links_Are_Being_Parsed_Correctly_Format_1(string updateId)
        {
            var catalogClient = new CatalogClient();
            List<CatalogSearchResult> searchResults = await catalogClient.SendSearchQueryAsync(updateId);

            Assert.NotNull(searchResults);
            Assert.NotEmpty(searchResults);

            UpdateInfo updateDetails = await catalogClient.GetUpdateDetailsAsync(searchResults[0]);
            
            Assert.NotNull(updateDetails);

            Assert.NotEmpty(updateDetails.DownloadLinks);
            bool allLinksAreNotEmpty = updateDetails.DownloadLinks.TrueForAll(link => !string.IsNullOrEmpty(link));

            Assert.True(allLinksAreNotEmpty);
            Assert.True(updateDetails.DownloadLinks.Count == 2, "Incorrect download links count");
        }

        [Trait("Catalog Search", "Tests for Catalog Search queries")]
        [Theory(DisplayName = "Download Links are being parsed correctly (catalog.sf.dl.download)")]
        [InlineData("3bdcfcf3-26a6-49da-8129-f1e293f9a634")]
        public async Task Download_Links_Are_Being_Parsed_Correctly_Format_2(string updateId)
        {
            var catalogClient = new CatalogClient();
            List<CatalogSearchResult> searchResults = await catalogClient.SendSearchQueryAsync(updateId);

            Assert.NotNull(searchResults);
            Assert.NotEmpty(searchResults);

            UpdateInfo updateDetails = await catalogClient.GetUpdateDetailsAsync(searchResults[0]);
            
            Assert.NotNull(updateDetails);

            Assert.NotEmpty(updateDetails.DownloadLinks);
            bool allLinksAreNotEmpty = updateDetails.DownloadLinks.TrueForAll(link => !string.IsNullOrEmpty(link));

            Assert.True(allLinksAreNotEmpty);
            Assert.True(updateDetails.DownloadLinks.Count == 1, "Incorrect download links count");
        }
    }
}
