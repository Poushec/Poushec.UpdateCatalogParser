using System;
using System.Collections.Generic;

namespace Poushec.UpdateCatalogParser.Models
{
    public class AdditionalProperties
    {
        public string MSRCNumber { get; set; } = String.Empty;
        public string MSRCSeverity { get; set; } = String.Empty;
        public string KBArticleNumbers { get; set; } = String.Empty;

        /// <summary>
        /// The list of UpdateIDs of the patches that supersede this update
        /// </summary>
        public List<string> SupersededBy { get; set; } = new List<string>();

        /// <summary>
        /// The list of Display Names (<see cref="UpdateInfo.Title">) of the patches superseded by this patch
        /// </summary>
        public List<string> Supersedes { get; set; } = new List<string>();
    }
}