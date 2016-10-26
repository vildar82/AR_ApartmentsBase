using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit_FlatExporter
{
    public class RoomInfo : DbElement
    {
        public string Name { get; set; }
    }

    public class ElementInfo : DbElement
    {
        public string FlatName { get; set; }
        public string FlatType { get; set; }
        public ElementId IdInModel { get; set; }
        public string Name { get; set; }
        public string Toretz { get; set; }
        public int IdIElementInModule { get; set; }
        public WallType WallType { get; set; }
        public FamilySymbol FamilyType { get; set; }
        public string Floors { get; set; }
        public Level Level { get; set; }
        public double Height { get; set; }
        public double Length { get; set; }

        public List<ParameterInfo> parameters = new List<ParameterInfo>();
        public List<DbElement> Doors { get; set; }
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public string Type { get; set; }

        public ParameterInfo(string name, object value, string type)
        {
            this.Name = name;
            this.Value = value;
            this.Type = type;
        }

    }

    public class DoorInfo : DbElement
    {
        public int IdIElementInModule { get; set; }
        public FamilySymbol FamilyType { get; set; }
        public double Height { get; set; }
        public bool IsRight { get; set; }
        public List<ParameterInfo> parameters = new List<ParameterInfo>();
    }

    public abstract class DbElement
    {
        public XYZ LocationPoint { get; set; }
        public XYZ Direction { get; set; }
        public string CategoryName { get; set; }
    }
}
