using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace UpdateCatalog
{
    public class UpdateBase
    {
        internal HtmlDocument _detailsPage; 

        //Availabe from search results
        public string Title { get; set; }
        public string UpdateID { get; set; }
        public List<string> Products { get; set; }
        public string Classification { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Size { get; set; }
        public int SizeInBytes { get; set; }
        
        //Should be collected seperatly. 
        public List<string> DownloadLinks { get; set; }
        public string Description { get; set; }
        public List<string> Architectures { get; set; }
        public List<string> SupportedLanguages { get; set; }
        public IEnumerable<string> MoreInformation { get; set; }
        public IEnumerable<string> SupportUrl { get; set; } 
        public string RestartBehavior { get; set; }
        public string MayRequestUserInput { get; set; }
        public string MustBeInstalledExclusively { get; set; }
        public string RequiresNetworkConnectivity { get; set; }
        public string UninstallNotes { get; set; }
        public string UnistallSteps { get; set; }

        public UpdateBase() { }

        internal UpdateBase(CatalogResultRow resultRow) 
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
            this.UnistallSteps = updateBase.UnistallSteps;
        }

        internal async Task<bool> CollectGenericInfo(HttpClient client)
        {
            if (!await _GetDetailsPage(client))
            {
                return false;
            }

            return await _CollectBaseDetails(client);
        }

        /// <summary>
        /// Function will collect download links of Catalog Update specified by 
        /// UpdateID of it's object. Founded links will be stored in DownloadLinks 
        /// property of this object. 
        /// </summary>
        /// <returns>TRUE if links was founded, FALSE if not</returns>
        protected async Task<bool> _CollectDownloadLinks(HttpClient client)
        {
            var ReqiestUri = "https://www.catalog.update.microsoft.com/DownloadDialog.aspx";
            
            var regex = new Regex(@"(http[s]?\://dl\.delivery\.mp\.microsoft\.com\/[^\'\""]*)|(http[s]?\://download\.windowsupdate\.com\/[^\'\""]*)");
            
            var post = JsonSerializer.Serialize(new {size = 0, updateId = UpdateID, uidInfo = UpdateID});
            var body = $"[{post}]";
            var requestBody = new Dictionary<string, string>();
            requestBody.Add("updateIDs", body);

            var requestContent = new FormUrlEncodedContent(requestBody);
            var responceDialog = new HttpResponseMessage();

            try
            {
                responceDialog = await client.PostAsync(ReqiestUri, requestContent);
            }
            catch
            {
                return false;
            }

            if (!responceDialog.IsSuccessStatusCode) 
            { 
                return false; 
            }

            string links = await responceDialog.Content.ReadAsStringAsync();

            links = links.Replace("www.download.windowsupdate", "download.windowsupdate");
            var regexMatches = regex.Matches(links);

            if (regexMatches.Count == 0)
            {
                return false;
            }

            DownloadLinks = regexMatches.Select(mt => mt.Value).ToList();

            return true;
        }

        protected async Task<bool> _GetDetailsPage(HttpClient client)
        {
            var ReqiestUri = $"https://www.catalog.update.microsoft.com/ScopedViewInline.aspx?updateid={this.UpdateID}";
            var response = new HttpResponseMessage();

            try
            {
                response = await client.SendAsync(new HttpRequestMessage() { RequestUri = new Uri(ReqiestUri) } );
            }
            catch (TaskCanceledException)
            {
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var tempPage = new HtmlDocument();
            tempPage.Load(await response.Content.ReadAsStreamAsync());

            if (tempPage.GetElementbyId("errorPageDisplayedError") != null)
            {
                return false;
            }

            _detailsPage = tempPage;
            return true;
        }

        protected async Task<bool> _CollectBaseDetails(HttpClient client)
        {
            try
            {
                if (!(await _CollectDownloadLinks(client)))
                {
                    return false;
                }

                this.Title = _detailsPage.GetElementbyId("ScopedViewHandler_titleText").InnerText;

                this.Products = new List<string>();
                _detailsPage.GetElementbyId("productsDiv")
                    .LastChild
                    .InnerText.Trim()
                    .Split(",")
                    .ToList()
                    .ForEach(prod => 
                    {
                        this.Products.Add(prod.Trim());
                    });

                this.Classification = _detailsPage.GetElementbyId("classificationDiv")
                    .LastChild
                    .InnerText.Trim();
                
                this.LastUpdated = DateTime.Parse(_detailsPage.GetElementbyId("ScopedViewHandler_date").InnerText);

                this.Size = _detailsPage.GetElementbyId("ScopedViewHandler_size").InnerText;

                this.Description = _detailsPage.GetElementbyId("ScopedViewHandler_desc").InnerText;

                Architectures = new List<string>();
                _detailsPage.GetElementbyId("archDiv")
                    .LastChild
                    .InnerText.Trim()
                    .Split(",")
                    .ToList()
                    .ForEach(arch => 
                    {
                       Architectures.Add(arch.Trim()); 
                    });

                SupportedLanguages = new List<string>();
                _detailsPage.GetElementbyId("languagesDiv")
                    .LastChild
                    .InnerText.Trim()
                    .Split(",")
                    .ToList()
                    .ForEach(lang => 
                    {
                        SupportedLanguages.Add(lang.Trim());
                    });

                var urlRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
                var moreInfoDiv = _detailsPage.GetElementbyId("moreInfoDiv").InnerHtml;
                this.MoreInformation = urlRegex.Matches(moreInfoDiv).Select(match => match.Value).ToHashSet();
                var supportUrlDiv = _detailsPage.GetElementbyId("suportUrlDiv").InnerHtml;
                this.SupportUrl = urlRegex.Matches(supportUrlDiv).Select(match => match.Value).ToHashSet();

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
                

                this.UnistallSteps = _detailsPage.GetElementbyId("uninstallStepsDiv")
                    .LastChild
                    .InnerText.Trim();

                return true;
            }
            catch 
            {
                return false;
            }
        }
    }
}
