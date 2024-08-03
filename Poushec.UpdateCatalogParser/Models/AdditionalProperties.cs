using System;
using System.Collections.Generic;
using System.Linq;
using Poushec.UpdateCatalogParser.Exceptions;

namespace Poushec.UpdateCatalogParser.Models
{
    public class AdditionalProperties
    {
        public string MSRCNumber { get; set; } = String.Empty;
        public string MSRCSeverity { get; set; } = String.Empty;
        public string KBArticleNumbers { get; set; } = String.Empty;
        public List<string> SupersededBy { get; set; } = new List<string>();
        public List<string> Supersedes { get; set; } = new List<string>();

        public AdditionalProperties(UpdateBase updateBase)
        {
            _parseUpdateDetails();
        }

        private void _parseUpdateDetails()
        {
            if (_detailsPage is null)
            {
                throw new ParseHtmlPageException("Failed to parse update details. _details page is null");
            }

            try
            {
                this.MSRCNumber = _detailsPage.GetElementbyId("securityBullitenDiv").LastChild.InnerText.Trim();
                this.MSRCSeverity = _detailsPage.GetElementbyId("ScopedViewHandler_msrcSeverity").InnerText;
                this.KBArticleNumbers = _detailsPage.GetElementbyId("kbDiv").LastChild.InnerText.Trim();
                this.SupersededBy = _parseSupersededByList();
                this.Supersedes = _parseSupersedesList();
            }
            catch (Exception ex)
            {
                throw new ParseHtmlPageException("Failed to parse Update details", ex);
            }
        }

        private List<string> _parseSupersededByList()
        {
            if (_detailsPage is null)
            {
                throw new ParseHtmlPageException("Failed to parse update details. _details page is null");
            }

            var supersededByDivs = _detailsPage.GetElementbyId("supersededbyInfo");
            var supersededBy = new List<string>();

            // If first child isn't a div - than it's just a n/a and there's nothing to gather
            if (supersededByDivs.FirstChild.InnerText.Trim() == "n/a")
            {
                return supersededBy;
            }

            supersededByDivs.ChildNodes
                .Where(node => node.Name == "div")
                .ToList()
                .ForEach(node =>
                {
                    var updateId = node.ChildNodes[1]
                        .GetAttributeValue("href", "")
                        .Replace("ScopedViewInline.aspx?updateid=", "");
                    
                    supersededBy.Add(updateId);
                });

            return supersededBy;
        }

        private List<string> _parseSupersedesList()
        {
            if (_detailsPage is null)
            {
                throw new ParseHtmlPageException("Failed to parse update details. _details page is null");
            }

            var supersedesDivs = _detailsPage.GetElementbyId("supersedesInfo");
            var supersedes = new List<string>();

            // If first child isn't a div - than it's just a n/a and there's nothing to gather
            if (supersedesDivs.FirstChild.InnerText.Trim() == "n/a")
            {
                return supersedes;
            }
            
            supersedesDivs.ChildNodes
                .Where(node => node.Name == "div")
                .ToList()
                .ForEach(node =>
                {
                    supersedes.Add(node.InnerText.Trim());
                });

            return supersedes;
        }
    }
}