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

        /// <summary>
        /// Update's unique identifier on the Microsoft Update Catalog in GUID format
        /// </summary>
        public string UpdateID { get; set; }

        /// <summary>
        /// The list of Supported Products the target patch is applicable to
        /// </summary>
        public List<string> Products { get; set; }

        /// <summary>
        /// The update's classification, e.g. Critical Update, Service Pack, Drivers etc.
        /// </summary>
        /// <remarks>
        /// <see href="https://learn.microsoft.com/en-us/troubleshoot/windows-client/installing-updates-features-roles/standard-terminology-software-updates">Description of the standard terminology that is used to describe Microsoft software updates</see>
        /// </remarks>
        public string Classification { get; set; }
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// String representation of update's size, e.g. 944.1 MB
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// Size in bytes of the Update's source files
        /// </summary>
        public long SizeInBytes { get; set; }

        // Common info from details page
        public string Description { get; set; } = String.Empty;
        public List<string> Architectures { get; set; } = new List<string>();
        public List<string> SupportedLanguages { get; set; } = new List<string>();
        public List<string> MoreInformationLinks { get; set; } = new List<string>();
        public List<string> SupportUrls { get; set; } = new List<string>(); 
        public string RestartBehavior { get; set; } = String.Empty;
        public string MayRequestUserInput { get; set; } = String.Empty;
        public string MustBeInstalledExclusively { get; set; } = String.Empty;
        public string RequiresNetworkConnectivity { get; set; } = String.Empty;
        public string UninstallNotes { get; set; } = String.Empty;
        public string UninstallSteps { get; set; } = String.Empty;

        /// <summary>
        /// Collection of Download Links for update's source files 
        /// </summary>
        public List<string> DownloadLinks { get; set; } = new List<string>();
        
        //Additional classification-specific info
        /// <summary>
        /// Update-specific properties such as <see cref="AdditionalProperties.SupersededBy">SupersededBy</see>,
        /// <see cref="AdditionalProperties.KBArticleNumbers">KBArticleNumbers</see> etc.
        /// </summary>
        /// <remarks>
        /// This property can be null if the update represents a Driver 
        /// </remarks>
        public AdditionalProperties AdditionalProperties { get; set; }

        /// <summary>
        /// Driver-specific properties such as <see cref="DriverProperties.HardwareIDs">HardwareIDs</see>,
        /// <see cref="DriverProperties.DriverClass">DriverClass</see>, <see cref="DriverProperties.DriverManufacturer">DriverManufacturer</see> etc.
        /// </summary>
        /// <remarks>
        /// This property will be NULL if the update doesn't represent a Driver
        /// </remarks>
        public DriverProperties DriverProperties { get; set; }
        
        public UpdateInfo() { }

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
