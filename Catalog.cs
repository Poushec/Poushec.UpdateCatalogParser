using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;
using System.Linq;
using UpdateCatalog.Exceptions;
using static System.Web.HttpUtility; 

namespace UpdateCatalog
{
    /// <summary>
    /// Static class that handles all communications with catalog.update.microsoft.com
    /// </summary>
    public static class CatalogClient
    {
        /// <summary>
        /// Sends search query to catalog.update.microsoft.com
        /// </summary>
        /// <param name="client">Running System.Net.Http.HttpClient</param>
        /// <param name="Query">Search Query</param>
        /// <param name="ignoreDublicates">
        /// TRUE - founded updates that have the same Title and SizeInBytes
        /// fields as any of already founded updates will be ignored.
        /// FALSE - collects every founded update.
        /// </param>
        /// <returns>List of objects derived from UpdateBase class (Update or Driver)</returns>
        public static async Task<List<CatalogResultRow>> SendSearchQuery(HttpClient client, string Query, bool ignoreDublicates = true)
        {
            string catalogBaseUrl = "https://www.catalog.update.microsoft.com/Search.aspx";
            string Uri = String.Format($"{catalogBaseUrl}?q={UrlEncode(Query)}"); 
            
            CatalogResponce responce = null;
            
            while (responce == null)
            {
                try
                {
                    responce = await InvokeCatalogRequest(client, Uri, HttpMethod.Get);
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

            ParseSearchResults(responce, ignoreDublicates, ref searchResults);

            while (responce.NextPage != null)
            {
                try
                {
                    var tempResponce = await InvokeCatalogRequest(
                        client,
                        Uri: Uri,
                        method: HttpMethod.Post,
                        EventArgument: responce.EventArgument,
                        EventTarget: "ctl00$catalogBody$nextPageLinkText",
                        EventValidation: responce.EventValidation,
                        ViewState: responce.ViewState,
                        ViewStateGenerator: responce.ViewStateGenerator
                    );

                    responce = tempResponce;
                }
                catch (TaskCanceledException)
                {
                    continue;
                }

                ParseSearchResults(responce, ignoreDublicates, ref searchResults);
            }

            return searchResults;
        }

        private static void ParseSearchResults(CatalogResponce responcePage, bool ignoreDublicates, ref List<CatalogResultRow> existingUpdates)
        {
            foreach (var row in responcePage.Rows)
            {
                if (row.Id != "headerRow")
                {
                    var Cells = row.SelectNodes("td");

                    if (ignoreDublicates)
                    {
                        //If updates collection already contains element with same SizeInBytes & same Title - skip it (assuming it is a dublicate)
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
        
        public static async Task<(bool Success, UpdateBase update)> TryGetUpdateDetails(HttpClient client, string UpdateID)
        {
            try
            {
                var update = await GetUpdateDetails(client, UpdateID);
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
        /// <param name="client">Running System.Net.Http.HttpClient</param>
        /// <param name="UpdateID">Update's UpdateID</param>
        /// <returns>Ether Driver of Update object derived from UpdateBase class with all collected details</returns>
        /// <exception cref="UnableToCollectUpdateDetailsException">Thrown when catalog responce with an error page or when request was unsuccessfull</exception>
        /// <exception cref="UpdateWasNotFoundException">Thrown when catalog responce with an error page with error code 8DDD0024 (Not found)</exception>
        /// <exception cref="CatalogErrorException">Thrown when catalog responce with an error page with unknown error code</exception>
        /// <exception cref="RequestToCatalogTimedOutException">Thrown when request to catalog was canceled due to timeout</exception>
        /// <exception cref="ParseHtmlPageException">Thrown when function was not able to parse ScopedView HTML page</exception>
        public static async Task<UpdateBase> GetUpdateDetails(HttpClient client, string UpdateID)
        {
            var updateBase = new UpdateBase() { UpdateID = UpdateID };
            await updateBase.CollectGenericInfo(client);

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
                    var update = new Update(updateBase);
                    update.CollectUpdateDetails();
                    return update;

                default: throw new NotImplementedException();
            }
        }

        private static async Task<CatalogResponce> InvokeCatalogRequest(
            HttpClient client,
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
            HttpResponseMessage rawResponce = null;

            if (method == HttpMethod.Post)
            {
                formData.Add("__EVENTTARGET", EventTarget);
                formData.Add("__EVENTARGUMENT", EventArgument);
                formData.Add("__VIEWSTATE", ViewState);
                formData.Add("__VIEWSTATEGENERATOR", ViewStateGenerator);
                formData.Add("__EVENTVALIDATION", EventValidation);

                var formContent = new FormUrlEncodedContent(formData);

                rawResponce = await client.PostAsync(Uri, formContent);
            }
            else
            {
                rawResponce = await client.SendAsync(new HttpRequestMessage() { RequestUri = new Uri(Uri) });
            }
            
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.Load(await rawResponce.Content.ReadAsStreamAsync());

            if (HtmlDoc.GetElementbyId("ctl00_catalogBody_noResultText") == null)
            {
                return new CatalogResponce(HtmlDoc);
            }

            throw new CatalogNoResultsException();
        }
    }
}
