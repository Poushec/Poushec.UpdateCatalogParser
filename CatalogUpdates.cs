using System.Collections.Generic;
using System.Linq;

namespace UpdateCatalog
{
    public class Update : UpdateBase
    {
        public string MSRCNumber { get; set; }
        public string MSRCSeverity { get; set; }
        public string KBArticleNumbers { get; set; }
        public List<(string UpdateID, string Title)> SupersededBy { get; set; }
        public List<string> Supersedes { get; set; }
        
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

        private List<(string, string)> CollectSupersededBy()
        {
            var supersededByDivs = _detailsPage.GetElementbyId("supersededbyInfo");
            var supersededBy = new List<(string, string)>();

            supersededByDivs.ChildNodes
                .Where(node => node.Name == "div")
                .ToList()
                .ForEach(node =>
                {
                    var link = node.ChildNodes[1]
                        .GetAttributeValue("href", "")
                        .Replace("ScopedViewInline.aspx?updateid=", "");
                    
                    var text = node.ChildNodes[1].InnerText;

                    supersededBy.Add(new (UpdateID = link, Title = text));
                });

            return supersededBy;
        }

        private List<string> CollectSupersedes()
        {
            var supersedesDivs = _detailsPage.GetElementbyId("supersedesInfo");
            var supersedes = new List<string>();
            
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