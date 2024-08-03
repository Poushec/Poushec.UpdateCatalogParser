using System;
using System.Collections.Generic;

namespace Poushec.UpdateCatalogParser.Models
{
    public class AdditionalProperties
    {
        public string MSRCNumber { get; set; } = String.Empty;
        public string MSRCSeverity { get; set; } = String.Empty;
        public string KBArticleNumbers { get; set; } = String.Empty;
        public List<string> SupersededBy { get; set; } = new List<string>();
        public List<string> Supersedes { get; set; } = new List<string>();
    }
}