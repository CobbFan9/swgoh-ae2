using System;
using System.Collections.Generic;
using System.Text;

namespace AssetGUIUno.Models
{
    public class MetadataShortModel
    {
        public MetadataData data { get; set; }
    }

    public class MetadataData
    {
        public int assetVersion { get; set; }
    }
}
