using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Poushec.UpdateCatalog.Exceptions;

namespace Poushec.UpdateCatalog.Models
{
    /// <summary>
    /// Class represents the shared content of Update Details page of any Update type (classification)
    /// </summary>
    public class UpdateBase
    {
        private readonly Regex _urlRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
        private readonly Regex _downloadLinkRegex = new Regex(@"(http[s]?\://dl\.delivery\.mp\.microsoft\.com\/[^\'\""]*)|(http[s]?\://download\.windowsupdate\.com\/[^\'\""]*)|(http[s]://catalog\.s\.download\.windowsupdate\.com.*?(?=\'))");

        protected HtmlDocument? _detailsPage; 

        // Info from search results
        public string Title { get; set; }
        public string UpdateID { get; set; }
        public List<string> Products { get; set; }
        public string Classification { get; set; }
        public DateOnly LastUpdated { get; set; }
        public string Size { get; set; }
        public int SizeInBytes { get; set; }

        // Info from details page
        public string Description { get; set; } = String.Empty;
        public List<string> Architectures { get; set; } = new();
        public List<string> SupportedLanguages { get; set; } = new();
        public List<string> MoreInformation { get; set; } = new();
        public List<string> SupportUrl { get; set; } = new(); 
        public string RestartBehavior { get; set; } = String.Empty;
        public string MayRequestUserInput { get; set; } = String.Empty;
        public string MustBeInstalledExclusively { get; set; } = String.Empty;
        public string RequiresNetworkConnectivity { get; set; } = String.Empty;
        public string UninstallNotes { get; set; } = String.Empty;
        public string UninstallSteps { get; set; } = String.Empty;

        // Download links from download page
        public List<string> DownloadLinks { get; set; } = new();

        internal UpdateBase(CatalogSearchResult resultRow) 
        {
            this.UpdateID = resultRow.UpdateID;
            this.Title = resultRow.Title;
            this.Classification = resultRow.Classification;
            this.LastUpdated = resultRow.LastUpdated;
            this.Size = resultRow.Size;
            this.SizeInBytes = resultRow.SizeInBytes;
            this.Products = resultRow.Products.Trim().Split(",").ToList(); 
        }

        internal UpdateBase(UpdateBase updateBase)
        {
            this._detailsPage = updateBase._detailsPage;
            this.Title = updateBase.Title;
            this.UpdateID = updateBase.UpdateID;
            this.Products = updateBase.Products;
            this.Classification = updateBase.Classification;
            this.LastUpdated = updateBase.LastUpdated;
            this.Size = updateBase.Size;
            this.SizeInBytes = updateBase.SizeInBytes;
            this.DownloadLinks = updateBase.DownloadLinks;
            this.Description = updateBase.Description;
            this.Architectures = updateBase.Architectures;
            this.SupportedLanguages = updateBase.SupportedLanguages;
            this.MoreInformation = updateBase.MoreInformation;
            this.SupportUrl = updateBase.SupportUrl;
            this.RestartBehavior = updateBase.RestartBehavior;
            this.MayRequestUserInput = updateBase.MayRequestUserInput;
            this.MustBeInstalledExclusively = updateBase.MustBeInstalledExclusively;
            this.RequiresNetworkConnectivity = updateBase.RequiresNetworkConnectivity;
            this.UninstallNotes = updateBase.UninstallNotes;
            this.UninstallSteps = updateBase.UninstallSteps;
        }

        internal async Task ParseCommonDetails(HttpClient client)
        {
            await _getDetailsPage(client);
            string downloadPageContent = await _getDownloadPageContent(client);
            
            _parseCommonDetails();
            _parseDownloadLinks(downloadPageContent);
        }

        protected void _parseCommonDetails()
        {
            if (_detailsPage is null)
            {
                throw new ParseHtmlPageException("_parseCommonDetails() failed. _detailsPage is null");
            }

            this.Description = _detailsPage.GetElementbyId("ScopedViewHandler_desc").InnerText;

            _detailsPage.GetElementbyId("archDiv")
                .LastChild
                .InnerText.Trim()
                .Split(",")
                .ToList()
                .ForEach(arch => 
                {
                   Architectures.Add(arch.Trim()); 
                });

            _detailsPage.GetElementbyId("languagesDiv")
                .LastChild
                .InnerText.Trim()
                .Split(",")
                .ToList()
                .ForEach(lang => 
                {
                    SupportedLanguages.Add(lang.Trim());
                });

            string moreInfoDivContent = _detailsPage.GetElementbyId("moreInfoDiv").InnerHtml;
            MatchCollection moreInfoUrlMatches = _urlRegex.Matches(moreInfoDivContent);

            if (moreInfoUrlMatches.Any())
            {
                this.MoreInformation = moreInfoUrlMatches.Select(match => match.Value)
                    .Distinct()
                    .ToList();
            }
                
            string supportUrlDivContent = _detailsPage.GetElementbyId("suportUrlDiv").InnerHtml;
            MatchCollection supportUrlMatches = _urlRegex.Matches(supportUrlDivContent);

            if (supportUrlMatches.Any())
            {
                this.SupportUrl = supportUrlMatches.Select(match => match.Value)
                    .Distinct()
                    .ToList();
            }

            this.RestartBehavior = _detailsPage.GetElementbyId("ScopedViewHandler_rebootBehavior").InnerText;

            this.MayRequestUserInput = _detailsPage.GetElementbyId("ScopedViewHandler_userInput").InnerText;

            this.MustBeInstalledExclusively = _detailsPage.GetElementbyId("ScopedViewHandler_installationImpact").InnerText;

            this.RequiresNetworkConnectivity = _detailsPage.GetElementbyId("ScopedViewHandler_connectivity").InnerText;

            var uninstallNotesDiv = _detailsPage.GetElementbyId("uninstallNotesDiv");

            if (uninstallNotesDiv.ChildNodes.Count == 3)
            {
                this.UninstallNotes = uninstallNotesDiv.LastChild.InnerText.Trim();
            }
            else
            {
                this.UninstallNotes = _detailsPage.GetElementbyId("uninstallNotesDiv")
                    .ChildNodes[3]
                    .InnerText.Trim();
            }
                
            this.UninstallSteps = _detailsPage.GetElementbyId("uninstallStepsDiv")
                .LastChild
                .InnerText.Trim();
        }

        private async Task<string> _getDownloadPageContent(HttpClient client)
        {
            var RequestUri = "https://www.catalog.update.microsoft.com/DownloadDialog.aspx";

            var request = new HttpRequestMessage(HttpMethod.Post, RequestUri);

            var post = JsonSerializer.Serialize(new {size = 0, languages = "", uidInfo = UpdateID, updateId = UpdateID});
            var body = $"[{post}]";

            var requestContent = new MultipartFormDataContent();
            requestContent.Add(new StringContent(body), "updateIds");

            request.Content = requestContent;

            var response = new HttpResponseMessage();

            try
            {
                response = await client.SendAsync(request);
            }
            catch (TaskCanceledException)
            {
                throw new RequestToCatalogTimedOutException();
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        protected void _parseDownloadLinks(string downloadPageContent)
        {   
            var downloadLinkMatches = _downloadLinkRegex.Matches(downloadPageContent);

            if (!downloadLinkMatches.Any())
            {
                throw new UnableToCollectUpdateDetailsException($"Downloads page does not contains any valid download links");
            }

            DownloadLinks = downloadLinkMatches.Select(mt => mt.Value).ToList();
        }

        protected async Task _getDetailsPage(HttpClient client)
        {
            var RequestUri = $"https://www.catalog.update.microsoft.com/ScopedViewInline.aspx?updateid={this.UpdateID}";
            var response = new HttpResponseMessage();

            try
            {
                response = await client.SendAsync(new HttpRequestMessage() { RequestUri = new Uri(RequestUri) } );
            }
            catch (TaskCanceledException ex)
            {
                throw new RequestToCatalogTimedOutException("Catalog was not responded", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new UnableToCollectUpdateDetailsException($"Catalog responded with {response.StatusCode} code");
            }

            var tempPage = new HtmlDocument();
            tempPage.Load(await response.Content.ReadAsStreamAsync());
            var errorDiv = tempPage.GetElementbyId("errorPageDisplayedError"); 

            if (errorDiv != null)
            {
                var errorCode = errorDiv.LastChild.InnerText.Trim().Replace("]", "");

                if (errorCode == "8DDD0010")
                {
                    throw new UnableToCollectUpdateDetailsException("Catalog cannot proceed your request right now. Send request again later");
                }
                else if (errorCode == "8DDD0024")
                {
                    throw new UpdateWasNotFoundException("Update by this UpdateID does not exists or was removed");
                }
                else
                {
                    throw new CatalogErrorException($"Catalog returned unknown error code: {errorCode}");
                }
            }

            _detailsPage = tempPage;
        }
    }
}
