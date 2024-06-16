using Poushec.UpdateCatalogParser;
using Poushec.UpdateCatalogParser.Enums;
using Poushec.UpdateCatalogParser.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace UpdateCatalogParser.Tests
{
    /// <summary>
    /// Various tests for sorting feature. 
    /// 
    /// All of these tests use 'SQL Server 2012' as search query as SQL2012 has reached EoL
    /// and I don't expect any updates to release for it in the future making it a good fit 
    /// for our purposes. 
    /// 
    /// I decided to only check the first object from search results as I can easily run 
    /// my queries directly @ catalog.update.microsoft.com and see what update go first in 
    /// search results, so since this library is merely a web-scraper I don't see any point 
    /// to check the entire list. 
    /// </summary>
    public class SortingTests
    {
        [Trait("Search Results Sorting", "Tests for Catalog Search Results Sorting feature")]
        [Theory(DisplayName = "Sorting: SortBy.Size ")]
        [InlineData("SQL Server 2012", SortBy.Size)]
        public async Task Send_Search_Query_Sort_By_Size_Parameter_Is_Respected(string searchQuery, SortBy sortBy)
        {
            var catalogClient = new CatalogClient();
            CatalogResponse firstPage = await catalogClient.GetFirstPageFromSearchQueryAsync(searchQuery, sortBy: sortBy, sortDirection: SortDirection.Ascending);

            Assert.NotNull(firstPage);
            Assert.NotEmpty(firstPage.SearchResults);
            Assert.Equal("19c1225f-1576-4042-9189-4afbb134e892", firstPage.SearchResults[0].UpdateID);
        }

        [Trait("Search Results Sorting", "Tests for Catalog Search Results Sorting feature")]
        [Theory(DisplayName = "Sorting: SortBy.Title ")]
        [InlineData("SQL Server 2012", SortBy.Title)]
        public async Task Send_Search_Query_Sort_By_Title_Parameter_Is_Respected(string searchQuery, SortBy sortBy)
        {
            var catalogClient = new CatalogClient();
            CatalogResponse firstPage = await catalogClient.GetFirstPageFromSearchQueryAsync(searchQuery, sortBy: sortBy, sortDirection: SortDirection.Ascending);

            Assert.NotNull(firstPage);
            Assert.NotEmpty(firstPage.SearchResults);
            Assert.Equal("4cd3344b-3848-423a-9d99-0b041fadfdbf", firstPage.SearchResults[0].UpdateID);
        }

        [Trait("Search Results Sorting", "Tests for Catalog Search Results Sorting feature")]
        [Theory(DisplayName = "Sorting: SortBy.Version ")]
        [InlineData("SQL Server 2012", SortBy.Version)]
        public async Task Send_Search_Query_Sort_By_Version_Parameter_Is_Respected(string searchQuery, SortBy sortBy)
        {
            var catalogClient = new CatalogClient();
            CatalogResponse firstPage = await catalogClient.GetFirstPageFromSearchQueryAsync(searchQuery, sortBy: sortBy, sortDirection: SortDirection.Ascending);

            Assert.NotNull(firstPage);
            Assert.NotEmpty(firstPage.SearchResults);
            Assert.Equal("b870730b-c565-4476-8e04-2961cb7966ca", firstPage.SearchResults[0].UpdateID);
        }

        [Trait("Search Results Sorting", "Tests for Catalog Search Results Sorting feature")]
        [Theory(DisplayName = "Sorting: SortBy.Classification ")]
        [InlineData("SQL Server 2012", SortBy.Classification)]
        public async Task Send_Search_Query_Sort_By_Classification_Parameter_Is_Respected(string searchQuery, SortBy sortBy)
        {
            var catalogClient = new CatalogClient();
            CatalogResponse firstPage = await catalogClient.GetFirstPageFromSearchQueryAsync(searchQuery, sortBy: sortBy, sortDirection: SortDirection.Ascending);

            Assert.NotNull(firstPage);
            Assert.NotEmpty(firstPage.SearchResults);
            Assert.Equal("fb8507ea-3f51-47d7-a2dc-3716d0506c56", firstPage.SearchResults[0].UpdateID);
        }

        [Trait("Search Results Sorting", "Tests for Catalog Search Results Sorting feature")]
        [Theory(DisplayName = "Sorting: SortBy.Products ")]
        [InlineData("SQL Server 2012", SortBy.Products)]
        public async Task Send_Search_Query_Sort_By_Products_Parameter_Is_Respected(string searchQuery, SortBy sortBy)
        {
            var catalogClient = new CatalogClient();
            CatalogResponse firstPage = await catalogClient.GetFirstPageFromSearchQueryAsync(searchQuery, sortBy: sortBy, sortDirection: SortDirection.Ascending);

            Assert.NotNull(firstPage);
            Assert.NotEmpty(firstPage.SearchResults);
            Assert.Equal("e737143c-35e9-4d02-8ce0-eb3f59c884f7", firstPage.SearchResults[0].UpdateID);
        }

        [Trait("Search Results Sorting", "Tests for Catalog Search Results Sorting feature")]
        [Theory(DisplayName = "Sorting: SortBy.LastUpdated ")]
        [InlineData("SQL Server 2012", SortBy.LastUpdated)]
        public async Task Send_Search_Query_Sort_By_LastUpdated_Parameter_Is_Respected(string searchQuery, SortBy sortBy)
        {
            var catalogClient = new CatalogClient();
            CatalogResponse firstPage = await catalogClient.GetFirstPageFromSearchQueryAsync(searchQuery, sortBy: sortBy, sortDirection: SortDirection.Ascending);

            Assert.NotNull(firstPage);
            Assert.NotEmpty(firstPage.SearchResults);
            Assert.Equal("6dfce7b5-4ca2-4ebd-ae75-14918abb529d", firstPage.SearchResults[0].UpdateID);
        }

        [Trait("Search Results Sorting", "Tests for Catalog Search Results Sorting feature")]
        [Theory(DisplayName = "Sorting: SortDirection.Ascending")]
        [InlineData("SQL Server 2012", SortBy.Size, SortDirection.Ascending)]
        public async Task Send_Search_Query_SortDirection_Ascending_Parameter_Is_Respected(string searchQuery, SortBy sortBy, SortDirection sortDirection)
        {
            var catalogClient = new CatalogClient();
            CatalogResponse firstPage = await catalogClient.GetFirstPageFromSearchQueryAsync(searchQuery, sortBy: sortBy, sortDirection: SortDirection.Ascending);

            Assert.NotNull(firstPage);
            Assert.NotEmpty(firstPage.SearchResults);
            Assert.Equal("19c1225f-1576-4042-9189-4afbb134e892", firstPage.SearchResults[0].UpdateID);
        }

        [Trait("Search Results Sorting", "Tests for Catalog Search Results Sorting feature")]
        [Theory(DisplayName = "Sorting: SortDirection.Descending")]
        [InlineData("SQL Server 2012", SortBy.Size, SortDirection.Descending)]
        public async Task Send_Search_Query_SortDirection_Descending_Parameter_Is_Respected(string searchQuery, SortBy sortBy, SortDirection sortDirection)
        {
            var catalogClient = new CatalogClient();
            CatalogResponse firstPage = await catalogClient.GetFirstPageFromSearchQueryAsync(searchQuery, sortBy: sortBy, sortDirection: SortDirection.Ascending);

            Assert.NotNull(firstPage);
            Assert.NotEmpty(firstPage.SearchResults);
            Assert.Equal("b870730b-c565-4476-8e04-2961cb7966ca", firstPage.SearchResults[0].UpdateID);
        }
    }
}
