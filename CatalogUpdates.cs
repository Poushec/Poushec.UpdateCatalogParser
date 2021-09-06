using System.Collections.Generic;
using System.Linq;

namespace UpdateCatalog
{
    public class Update : UpdateBase
    {
        public string MSRCNumber { get; set; }
        public string MSRCSeverity { get; set; }
        public string KBArticleNumbers { get; set; }
        public List<string> SupersededBy { get; set; }
        public List<string> Supersedes { get; set; }
        
        public Update() { }
        public Update(UpdateBase updateBase) : base(updateBase) {   }

        public bool CollectUpdateDetails()
        {
            try
            {
                this.MSRCNumber = _detailsPage.GetElementbyId("securityBullitenDiv").LastChild.InnerText.Trim();
                this.MSRCSeverity = _detailsPage.GetElementbyId("ScopedViewHandler_msrcSeverity").InnerText;
                this.KBArticleNumbers = _detailsPage.GetElementbyId("kbDiv").LastChild.InnerText.Trim();
                this.SupersededBy = CollectSupersededBy();
                this.Supersedes = CollectSupersedes();
                
                return true;
            }
            catch 
            {
                return false;
            }
        }

        private List<string> CollectSupersededBy()
        {
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

        private List<string> CollectSupersedes()
        {
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