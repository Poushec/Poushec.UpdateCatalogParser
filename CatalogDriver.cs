using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace UpdateCatalog
{
    public class Driver : UpdateBase
    {
        public string Company { get; set; }
        public string DriverManufacturer { get; set; }
        public string DriverClass { get; set; }
        public string DriverModel { get; set; }
        public string DriverProvider { get; set; }
        public string DriverVersion { get; set; }
        public DateTime VersionDate { get; set; }
        public List<string> HardwareIDs { get; set; }

        public Driver(UpdateBase updateBase) : base(updateBase) {   }

        public bool CollectDriverDetails()
        {
            try
            {
                this.HardwareIDs = GetHardwareIDs();
                this.Company = _detailsPage.GetElementbyId("ScopedViewHandler_company").InnerText;
                this.DriverManufacturer = _detailsPage.GetElementbyId("ScopedViewHandler_manufacturer").InnerText;
                this.DriverClass = _detailsPage.GetElementbyId("ScopedViewHandler_driverClass").InnerText;
                this.DriverModel = _detailsPage.GetElementbyId("ScopedViewHandler_driverModel").InnerText;
                this.DriverProvider = _detailsPage.GetElementbyId("ScopedViewHandler_driverProvider").InnerText;
                this.DriverVersion = _detailsPage.GetElementbyId("ScopedViewHandler_version").InnerText;
                this.VersionDate = DateTime.Parse(_detailsPage.GetElementbyId("ScopedViewHandler_versionDate").InnerText);
            }
            catch 
            {
                return false;
            }
            
            return true;
        }

        private protected List<string> GetHardwareIDs()
        {
            var hwIdsDivs = _detailsPage.GetElementbyId("driverhwIDs");

            if (hwIdsDivs == null)
            {
                return new List<string>();
            }

            var hwIds = new List<string>();

            hwIdsDivs.ChildNodes
                .Where(node => node.Name == "div")
                .ToList()
                .ForEach(node => 
                {
                    var hid = node.ChildNodes
                        .First().InnerText
                        .Trim()
                        .Replace(@"\r\n", "")
                        .ToUpper();
                    
                    if (hid != "")
                    {
                        hwIds.Add(hid);
                    }
                });
            
            return hwIds;
        }
    }
}