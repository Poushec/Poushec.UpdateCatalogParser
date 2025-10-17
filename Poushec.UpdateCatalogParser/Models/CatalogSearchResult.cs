using System;

namespace Poushec.UpdateCatalogParser.Models
{
    public class CatalogSearchResult
    {
        public string Title { get; set; }
        public string Products { get; set; }
        public string Classification { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Version { get; set; }
        public string Size { get; set; }
        public long SizeInBytes { get; set; }
        public string UpdateID { get; set; }

        internal CatalogSearchResult(
            string title, 
            string products, 
            string classification, 
            DateTime lastUpdated, 
            string version, 
            string size, 
            long sizeInBytes, 
            string updateId) 
        {
            this.Title = title;
            this.Products = products;
            this.Classification = classification;
            this.LastUpdated = lastUpdated;
            this.Version = version;
            this.Size = size;
            this.SizeInBytes = sizeInBytes;
            this.UpdateID = updateId;
        }

    }
} 
