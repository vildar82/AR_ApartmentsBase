using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace AR_ApartmentBase.Model
{    
    public static class Options
    {
        public static string HostWallParameter { get; set; } = "IdWall";
        public static string WallCategory { get; set; } = "Базовая стена";
    }
}