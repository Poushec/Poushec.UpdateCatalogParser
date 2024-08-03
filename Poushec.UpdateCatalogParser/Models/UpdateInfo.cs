using System;
using System.Collections.Generic;
using System.Linq;

namespace Poushec.UpdateCatalogParser.Models
{
    /// <summary>
    /// Class represents the shared content of Update Details page of any Update type (classification)
    /// </summary>
    public class UpdateInfo
    {
        // Info from search results
        public string Title { get; set; }
        public string UpdateID { get; set; }
        public List<string> Products { get; set; }
        public string Classification { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Size { get; set; }
        public int SizeInBytes { get; set; }

        // Common info from details page
        public string Description { get; set; } = String.Empty;
        public List<string> Architectures { get; set; } = new List<string>();
        public List<string> SupportedLanguages { get; set; } = new List<string>();
        public List<string> MoreInformation { get; set; } = new List<string>();
        public List<string> SupportUrl { get; set; } = new List<string>(); 
        public string RestartBehavior { get; set; } = String.Empty;
        public string MayRequestUserInput { get; set; } = String.Empty;
        public string MustBeInstalledExclusively { get; set; } = String.Empty;
        public string RequiresNetworkConnectivity { get; set; } = String.Empty;
        public string UninstallNotes { get; set; } = String.Empty;
        public string UninstallSteps { get; set; } = String.Empty;

        // Download links from download page
        public List<string> DownloadLinks { get; set; } = new List<string>();
        
        //Additional classification-specific info
        public AdditionalProperties AdditionalProperties { get; set; }
        public DriverProperties DriverInfo { get; set; }
        
        internal UpdateInfo() { }

        internal UpdateInfo(CatalogSearchResult resultRow) 
        {
            this.UpdateID = resultRow.UpdateID;
            this.Title = resultRow.Title;
            this.Classification = resultRow.Classification;
            this.LastUpdated = resultRow.LastUpdated;
            this.Size = resultRow.Size;
            this.SizeInBytes = resultRow.SizeInBytes;
            this.Products = resultRow.Products.Trim().Split(',').ToList(); 
        }
    }
}
