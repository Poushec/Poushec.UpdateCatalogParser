using HtmlAgilityPack;
using System;

namespace UpdateCatalog
{
    public class CatalogResultRow
    {
        public string Title { get; set; }
        public string Products { get; set; }
        public string Classification { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Version { get; set; }
        public string Size { get; set; }
        public int SizeInBytes { get; set; }
        public string UpdateID { get; set; }

        public CatalogResultRow() { }

        public CatalogResultRow(HtmlNode Row)
        {
            var Cells = Row.SelectNodes("td");
            
            Title = Cells[1].InnerText.Trim();
            Products = Cells[2].InnerText.Trim();
            Classification = Cells[3].InnerText.Trim();
            LastUpdated = DateTime.Parse(Cells[4].InnerText.Trim());
            Version = Cells[5].InnerText.Trim();
            Size = Cells[6].SelectNodes("span")[0].InnerText;
            SizeInBytes = int.Parse(Cells[6].SelectNodes("span")[1].InnerHtml);
            UpdateID = Cells[7].SelectNodes("input")[0].Id;
        }
    }
} 
