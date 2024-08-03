using System;
using System.Collections.Generic;

namespace Poushec.UpdateCatalogParser.Models
{
    public class DriverProperties
    {
        /// <summary>
        /// The name of the company that released the driver, e.g. Lenovo
        /// </summary>
        public string Company { get; set; } = String.Empty;

        /// <summary>
        /// The name of the driver's manufacturer, e.g. NVIDIA
        /// </summary>
        public string DriverManufacturer { get; set; } = String.Empty;

        /// <summary>
        /// The class of the device the target driver was made for, e.g. Video, Network, OtherHardware etc. 
        /// </summary>
        public string DriverClass { get; set; } = String.Empty;

        /// <summary>
        /// The name of the device model the target driver was made for
        /// </summary>
        public string DriverModel { get; set; } = String.Empty;

        /// <summary>
        /// The name of the company that released the driver, e.g. Intel
        /// </summary>
        public string DriverProvider { get; set; } = String.Empty;
        public string DriverVersion { get; set; } = String.Empty;
        public DateTime VersionDate { get; set; } = DateTime.MinValue;

        /// <summary>
        /// The list of Hardware Identifiers of the devices the target patch is applicable to
        /// </summary>
        /// <remarks>
        /// <see href="https://learn.microsoft.com/en-us/windows-hardware/drivers/install/hardware-ids">Read more - Hardware ID</see>
        /// </remarks>
        public List<string> HardwareIDs { get; set; } = new List<string>();
    }
}