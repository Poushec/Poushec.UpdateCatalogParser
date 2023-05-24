using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;
using System.Linq;
using Poushec.UpdateCatalog.Models;
using Poushec.UpdateCatalog.Exceptions;
using static System.Web.HttpUtility;

namespace Poushec.UpdateCatalog
{
    /// <summary>
    /// Class that handles all communications with catalog.update.microsoft.com
    /// </summary>
    public class CatalogClient
    {
        private HttpClient _client;

        public CatalogClient()
        {
            _client = new HttpClient();
        }

        public CatalogClient(HttpClient client)
        {
            _client = client;
        }
        
        /// <summary>
        /// Sends search query to catalog.update.microsoft.com
        /// </summary>
        /// <param name="Query">Search Query</param>
        /// <param name="ignoreDuplicates">
        /// TRUE - founded updates that have the same Title and SizeInBytes
        /// fields as any of already founded updates will be ignored.
        /// FALSE - collects every founded update.
        /// </param>
        /// <returns>List of objects derived from UpdateBase class (Update or Driver)</returns>
        public async Task<List<CatalogResultRow>> SendSearchQueryAsync(string Query, bool ignoreDuplicates = true)
        {
            string catalogBaseUrl = "https://www.catalog.update.microsoft.com/Search.aspx";
            string Uri = String.Format($"{catalogBaseUrl}?q={UrlEncode(Query)}"); 
            
            CatalogResponse response = null;
            
            while (response == null)
            {
                try
                {
                    response = await InvokeCatalogRequestAsync(Uri, HttpMethod.Get);
                }
                catch (TaskCanceledException)
                {
                    continue;
                }
                catch (CatalogNoResultsException)
                {
                    return new List<CatalogResultRow>();
                }
            }
            
            var searchResults = new List<CatalogResultRow>();

            ParseSearchResults(response, ignoreDuplicates, ref searchResults);

            while (response.NextPage != null)
            {
                try
                {
                    var tempResponse = await InvokeCatalogRequestAsync(
                        Uri: Uri,
                        method: HttpMethod.Post,
                        EventArgument: response.EventArgument,
                        EventTarget: "ctl00$catalogBody$nextPageLinkText",
                        EventValidation: response.EventValidation,
                        ViewState: response.ViewState,
                        ViewStateGenerator: response.ViewStateGenerator
                    );

                    response = tempResponse;
                }
                catch (TaskCanceledException)
                {
                    continue;
                }

                ParseSearchResults(response, ignoreDuplicates, ref searchResults);
            }

            return searchResults;
        }

        private void ParseSearchResults(CatalogResponse responsePage, bool ignoreDuplicates, ref List<CatalogResultRow> existingUpdates)
        {
            foreach (var row in responsePage.Rows)
            {
                if (row.Id != "headerRow")
                {
                    var Cells = row.SelectNodes("td");

                    if (ignoreDuplicates)
                    {
                        //If updates collection already contains element with same SizeInBytes & same Title - skip it (assuming it is a duplicate)
                        if (existingUpdates
                            .Where(upd => upd.SizeInBytes.ToString() == Cells[6].SelectNodes("span")[1].InnerHtml 
                            && upd.Title == Cells[1].InnerText.Trim()).Count() == 0
                        )
                        {
                            existingUpdates.Add(new CatalogResultRow(row));
                        }
                    }
                    else
                    {
                        existingUpdates.Add(new CatalogResultRow(row));
                    }
                }
            }
        }
        
        public async Task<(bool Success, UpdateBase update)> TryGetUpdateDetailsAsync(string UpdateID)
        {
            try
            {
                var update = await GetUpdateDetailsAsync(UpdateID);
                return (true, update);
            }
            catch
            {
                return (false, null);
            }
        }

        /// <summary>
        /// Collect update details from Updates Details Page and it's download links 
        /// </summary>
        /// <param name="UpdateID">Update's UpdateID</param>
        /// <returns>Ether Driver of Update object derived from UpdateBase class with all collected details</returns>
        /// <exception cref="UnableToCollectUpdateDetailsException">Thrown when catalog response with an error page or when request was unsuccessful</exception>
        /// <exception cref="UpdateWasNotFoundException">Thrown when catalog response with an error page with error code 8DDD0024 (Not found)</exception>
        /// <exception cref="CatalogErrorException">Thrown when catalog response with an error page with unknown error code</exception>
        /// <exception cref="RequestToCatalogTimedOutException">Thrown when request to catalog was canceled due to timeout</exception>
        /// <exception cref="ParseHtmlPageException">Thrown when function was not able to parse ScopedView HTML page</exception>
        public async Task<UpdateBase> GetUpdateDetailsAsync(string UpdateID)
        {
            var updateBase = new UpdateBase() { UpdateID = UpdateID };
            await updateBase.CollectGenericInfo(_client);

            if (updateBase.Classification.Contains("Driver"))
            {
                var driverUpdate = new Driver(updateBase);
                driverUpdate.CollectDriverDetails();

                return driverUpdate;
            }

            switch (updateBase.Classification)
            {
                case "Security Updates":
                case "Critical Updates":
                case "Definition Updates":
                case "Feature Packs": 
                case "Service Packs":
                case "Update Rollups":
                case "Updates": 
                case "Hotfix":
                    var update = new Update(updateBase);
                    update.CollectUpdateDetails();
                    return update;

                default: throw new NotImplementedException();
            }
        }

        private async Task<CatalogResponse> InvokeCatalogRequestAsync(
            string Uri,
            HttpMethod method,
            string EventArgument = "",
            string EventTarget = "",
            string EventValidation = "",
            string ViewState = "",
            string ViewStateGenerator = "" 
        )
        {
            var formData = new Dictionary<string, string>();
            HttpResponseMessage rawResponse = null;

            if (method == HttpMethod.Post)
            {
                formData.Add("__EVENTTARGET", EventTarget);
                formData.Add("__EVENTARGUMENT", EventArgument);
                formData.Add("__VIEWSTATE", ViewState);
                formData.Add("__VIEWSTATEGENERATOR", ViewStateGenerator);
                formData.Add("__EVENTVALIDATION", EventValidation);

                var formContent = new FormUrlEncodedContent(formData);

                rawResponse = await _client.PostAsync(Uri, formContent);
            }
            else
            {
                rawResponse = await _client.SendAsync(new HttpRequestMessage() { RequestUri = new Uri(Uri) });
            }
            
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.Load(await rawResponse.Content.ReadAsStreamAsync());

            if (HtmlDoc.GetElementbyId("ctl00_catalogBody_noResultText") == null)
            {
                return new CatalogResponse(HtmlDoc);
            }

            throw new CatalogNoResultsException();
        }
    }
}
