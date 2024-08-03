using System;
using System.Collections.Generic;

namespace Poushec.UpdateCatalogParser.Models
{
    public class DriverProperties
    {
        public string Company { get; set; } = String.Empty;
        public string DriverManufacturer { get; set; } = String.Empty;
        public string DriverClass { get; set; } = String.Empty;
        public string DriverModel { get; set; } = String.Empty;
        public string DriverProvider { get; set; } = String.Empty;
        public string DriverVersion { get; set; } = String.Empty;
        public DateTime VersionDate { get; set; } = DateTime.MinValue;
        public List<string> HardwareIDs { get; set; } = new List<string>();
    }
}