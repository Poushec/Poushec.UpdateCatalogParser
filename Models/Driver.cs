using System;
using System.Collections.Generic;
using System.Linq;
using Poushec.UpdateCatalogParser.Exceptions;

namespace Poushec.UpdateCatalogParser.Models
{
    public class Driver : UpdateBase
    {
        public string Company { get; set; } = String.Empty;
        public string DriverManufacturer { get; set; } = String.Empty;
        public string DriverClass { get; set; } = String.Empty;
        public string DriverModel { get; set; } = String.Empty;
        public string DriverProvider { get; set; } = String.Empty;
        public string DriverVersion { get; set; } = String.Empty;
        public DateOnly VersionDate { get; set; } = DateOnly.MinValue;
        public List<string> HardwareIDs { get; set; } = new();

        public Driver(UpdateBase updateBase) : base(updateBase) 
        {
            _parseDriverDetails();
        }

        private void _parseDriverDetails()
        {
            if (_detailsPage is null)
            {
                throw new ParseHtmlPageException("Failed to parse update details. _details page is null");
            }

            try
            {
                this.HardwareIDs = _parseHardwareIDs();
                this.Company = _detailsPage.GetElementbyId("ScopedViewHandler_company").InnerText;
                this.DriverManufacturer = _detailsPage.GetElementbyId("ScopedViewHandler_manufacturer").InnerText;
                this.DriverClass = _detailsPage.GetElementbyId("ScopedViewHandler_driverClass").InnerText;
                this.DriverModel = _detailsPage.GetElementbyId("ScopedViewHandler_driverModel").InnerText;
                this.DriverProvider = _detailsPage.GetElementbyId("ScopedViewHandler_driverProvider").InnerText;
                this.DriverVersion = _detailsPage.GetElementbyId("ScopedViewHandler_version").InnerText;
                this.VersionDate = DateOnly.Parse(_detailsPage.GetElementbyId("ScopedViewHandler_versionDate").InnerText);
            }
            catch (Exception ex)
            {
                throw new ParseHtmlPageException("Failed to parse Driver details", ex);
            }
        }

        private List<string> _parseHardwareIDs()
        {
            if (_detailsPage is null)
            {
                throw new ParseHtmlPageException("Failed to parse update details. _details page is null");
            }

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
                    
                    if (!String.IsNullOrEmpty(hid))
                    {
                        hwIds.Add(hid);
                    }
                });
            
            return hwIds;
        }
    }
}