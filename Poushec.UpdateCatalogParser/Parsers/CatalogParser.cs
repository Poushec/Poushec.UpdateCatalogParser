using HtmlAgilityPack;
using Poushec.UpdateCatalogParser.Exceptions;
using Poushec.UpdateCatalogParser.Extensions;
using Poushec.UpdateCatalogParser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Poushec.UpdateCatalogParser.Parsers
{
    internal class CatalogParser
    {
        private readonly Regex _urlRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
        private readonly Regex _downloadLinkRegex = new Regex(@"(http[s]?\://dl\.delivery\.mp\.microsoft\.com\/[^\'\""]*)|(http[s]?\://download\.windowsupdate\.com\/[^\'\""]*)|(http[s]://catalog\.s\.download\.windowsupdate\.com.*?(?=\'))");

        private readonly HttpClient _httpClient;

        public CatalogParser(HttpClient httpClient) 
        {
            _httpClient = httpClient;
        }
        
        internal UpdateBase CollectUpdateInfoFromDetailsPage(CatalogSearchResult searchResult, HtmlDocument detailsPage)
        {
            var update = new UpdateBase(searchResult);

            update.Architectures = detailsPage.GetElementbyId("archDiv").LastChild.InnerText
                .Split(',')
                .Select(arch => arch.Trim())
                .ToList();

            update.SupportedLanguages = detailsPage.GetElementbyId("languagesDiv").LastChild.InnerText
                .Split(',')
                .Select(lang => lang.Trim())
                .ToList();

            update.MoreInformation = _urlRegex.Matches(detailsPage.GetElementbyId("moreInfoDiv").InnerHtml)
                .Select(match => match.Value)
                .Distinct()
                .ToList();

            update.SupportUrl = _urlRegex.Matches(detailsPage.GetElementbyId("supportUrlDiv").InnerHtml)
                .Select(match => match.Value)
                .Distinct()
                .ToList();

            update.Description = detailsPage.GetElementbyId("ScopedViewHandler_desc").InnerText;
            update.RestartBehavior = detailsPage.GetElementbyId("ScopedViewHandler_rebootBehavior").InnerText;
            update.MayRequestUserInput = detailsPage.GetElementbyId("ScopedViewHandler_userInput").InnerText;
            update.MustBeInstalledExclusively = detailsPage.GetElementbyId("ScopedViewHandler_installationImpact").InnerText;
            update.RequiresNetworkConnectivity = detailsPage.GetElementbyId("ScopedViewHandler_connectivity").InnerText;

            var uninstallNotesDiv = detailsPage.GetElementbyId("uninstallNotesDiv");

            if (uninstallNotesDiv.ChildNodes.Count == 3)
            {
                update.UninstallNotes = uninstallNotesDiv.LastChild.InnerText.Trim();
            }
            else
            {
                update.UninstallNotes = detailsPage.GetElementbyId("uninstallNotesDiv")
                    .ChildNodes[3]
                    .InnerText.Trim();
            }
                
            update.UninstallSteps = detailsPage.GetElementbyId("uninstallStepsDiv")
                .LastChild
                .InnerText.Trim();

            return update;
        }
        
        internal async Task<HtmlDocument> LoadDetailsPageAsync(string updateId)
        {
            string detailsPageUri = $"https://www.catalog.update.microsoft.com/ScopedViewInline.aspx?updateid={updateId}";
            HttpResponseMessage responseMessage;

            try
            {
                responseMessage = await _httpClient.GetAsync(detailsPageUri);
            }
            catch (TaskCanceledException ex)
            {
                throw new RequestToCatalogTimedOutException("Catalog did not response", ex);
            }

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new UnableToCollectUpdateDetailsException($"Catalog responded with {responseMessage.StatusCode} code");
            }

            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.Load(responseStream);

                var errorDiv = htmlDocument.GetElementbyId("errorPageDisplayedError"); 

                if (errorDiv != null)
                {
                    return htmlDocument;
                }

                var errorCode = errorDiv.LastChild.InnerText.Trim().Replace("]", "");

                if (errorCode == "8DDD0010")
                {
                    throw new UnableToCollectUpdateDetailsException("Catalog cannot proceed your request right now. Send request again later");
                }
                else if (errorCode == "8DDD0024")
                {
                    throw new UpdateWasNotFoundException($"Update with UpdateID {updateId} does not exists or was removed");
                }
                else
                {
                    throw new CatalogErrorException($"Catalog returned unknown error code: {errorCode}");
                }
            }
        }

        internal async Task<IEnumerable<string>> GetDownloadLinksAsync(string updateId)
        {
            var downloadPageUri = "https://www.catalog.update.microsoft.com/DownloadDialog.aspx";

            var requestBody = JsonSerializer.Serialize(new { size = 0, languages = "", uidInfo = updateId, updateId = updateId });
            var requestPayload = $"[{requestBody}]";

            var requestContent = new MultipartFormDataContent
            {
                { new StringContent(requestPayload), "updateIds" }
            };

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.PostAsync(downloadPageUri, requestContent);
            }
            catch (TaskCanceledException)
            {
                throw new RequestToCatalogTimedOutException(); 
            }

            response.EnsureSuccessStatusCode();

            string downloadPageContent = await response.Content.ReadAsStringAsync();
            var downloadLinkMatches = _downloadLinkRegex.Matches(downloadPageContent);

            if (!downloadLinkMatches.Any())
            {
                throw new UnableToCollectUpdateDetailsException($"Downloads page does not contains any valid download links");
            }

            return downloadLinkMatches.Select(match => match.Value);
        }

        internal CatalogResponse ParseSearchResultsPage(HtmlDocument htmlDoc, HttpClient client, string searchQueryUri)
        {
            string eventArgument = htmlDoc.GetElementbyId("__EVENTARGUMENT")?.FirstChild?.Attributes["value"]?.Value ?? String.Empty;
            string eventValidation = htmlDoc.GetElementbyId("__EVENTVALIDATION").GetAttributes().Where(att => att.Name == "value").First().Value;
            string viewState = htmlDoc.GetElementbyId("__VIEWSTATE").GetAttributes().Where(att => att.Name == "value").First().Value;
            string viewStateGenerator = htmlDoc.GetElementbyId("__VIEWSTATEGENERATOR").GetAttributes().Where(att => att.Name == "value").First().Value;
            bool finalPage = htmlDoc.GetElementbyId("ctl00_catalogBody_nextPageLinkText") is null;

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
                .Select(resultsRow => ParseResultsTableRow(resultsRow))
                .ToList();

            return new CatalogResponse(
                client, 
                searchQueryUri, 
                searchResults, 
                eventArgument, 
                eventValidation, 
                viewState, 
                viewStateGenerator, 
                finalPage, 
                resultsCount
            );
        }

        internal CatalogSearchResult ParseResultsTableRow(HtmlNode resultsRow)
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
