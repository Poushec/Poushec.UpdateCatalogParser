using HtmlAgilityPack;
using System.Linq;

namespace UpdateCatalog
{
    internal class CatalogResponce
    {
        public HtmlNodeCollection Rows;
        public string EventArgument;
        public string EventValidation;
        public string ViewState;
        public string ViewStateGenerator;
        public HtmlNode NextPage; 

        public CatalogResponce() {  }
        public CatalogResponce(HtmlDocument doc)
        {
            var Table = doc.GetElementbyId("ctl00_catalogBody_updateMatches");
            Rows = Table.SelectNodes("tr");
            try { EventArgument = doc.GetElementbyId("__EVENTARGUMENT").FirstChild.Attributes["value"].Value; }
            catch { EventArgument = string.Empty; }
            EventValidation = doc.GetElementbyId("__EVENTVALIDATION").GetAttributes().Where(att => att.Name == "value").First().Value;
            ViewState = doc.GetElementbyId("__VIEWSTATE").GetAttributes().Where(att => att.Name == "value").First().Value;
            ViewStateGenerator = doc.GetElementbyId("__VIEWSTATEGENERATOR").GetAttributes().Where(att => att.Name == "value").First().Value;
            NextPage = doc.GetElementbyId("ctl00_catalogBody_nextPageLinkText");
        } 
    }
}