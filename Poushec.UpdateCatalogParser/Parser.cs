using HtmlAgilityPack;
using Poushec.UpdateCatalogParser.Exceptions;
using Poushec.UpdateCatalogParser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Poushec.UpdateCatalogParser
{
    /// <summary>
    /// Contains methods for parsing Catalog HTML pages
    /// </summary>
    internal static class Parser
    {
        internal static CatalogResponse ParseFromHtmlPage(HtmlDocument htmlDoc, HttpClient client, string searchQueryUri)
        {
            string eventArgument = htmlDoc.GetElementbyId("__EVENTARGUMENT")?.FirstChild?.Attributes["value"]?.Value ?? String.Empty;
            string eventValidation = htmlDoc.GetElementbyId("__EVENTVALIDATION").GetAttributes().Where(att => att.Name == "value").First().Value;
            string viewState = htmlDoc.GetElementbyId("__VIEWSTATE").GetAttributes().Where(att => att.Name == "value").First().Value;
            string viewStateGenerator = htmlDoc.GetElementbyId("__VIEWSTATEGENERATOR").GetAttributes().Where(att => att.Name == "value").First().Value;
            HtmlNode nextPage = htmlDoc.GetElementbyId("ctl00_catalogBody_nextPageLinkText");

            string resultsCountString = htmlDoc.GetElementbyId("ctl00_catalogBody_searchDuration").InnerText;
            int resultsCount = int.Parse(Regex.Match(resultsCountString, "(?<=of )\\d{1,4}").Value);

            HtmlNode table = htmlDoc.GetElementbyId("ctl00_catalogBody_updateMatches");

            if (table is null)
            {
                throw new CatalogFailedToLoadSearchResultsPageException("Catalog response does not contains a search results table");
            }

            HtmlNodeCollection searchResultsRows = table.SelectNodes("tr");

            List<CatalogSearchResult> searchResults = searchResultsRows
                .Skip(1) // First row is always a headerRow
                .Select(resultsRow => ParseFromResultsTableRow(resultsRow))
                .ToList();

            return new CatalogResponse(
                client, 
                searchQueryUri, 
                searchResults, 
                eventArgument, 
                eventValidation, 
                viewState, 
                viewStateGenerator, 
                nextPage, 
                resultsCount
            );
        }

        internal static CatalogSearchResult ParseFromResultsTableRow(HtmlNode resultsRow)
        {
            HtmlNodeCollection rowCells = resultsRow.SelectNodes("td");
            
            string title = rowCells[1].InnerText.Trim();
            string products = rowCells[2].InnerText.Trim();
            string classification = rowCells[3].InnerText.Trim();
            DateTime lastUpdated = DateTime.Parse(rowCells[4].InnerText.Trim());
            string version = rowCells[5].InnerText.Trim();
            string size = rowCells[6].SelectNodes("span")[0].InnerText;
            int sizeInBytes = int.Parse(rowCells[6].SelectNodes("span")[1].InnerHtml);
            string updateID = rowCells[7].SelectNodes("input")[0].Id;

            return new CatalogSearchResult(title, products, classification, lastUpdated, version, size, sizeInBytes, updateID);
        }
    }
}
