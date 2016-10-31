using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Revit_FlatExporter
{
    public static class Helper
    {
        public static List<string> Categories = new List<string>()
       {
           "Стены", "Двери", "Окна", "Сантехнические приборы", "Мебель", "Силовые электроприборы",
           "Электрооборудование","Перекрытия","Разделитель помещений","Помещения","Потолки"
       };

        public static string ConvertXyzToString(XYZ point, bool isDirection)
        {
            if (!isDirection)
                return Math.Round(point.X * 304.8, 4).ToString(CultureInfo.InvariantCulture) + ";" + Math.Round(point.Y * 304.8, 4).ToString(CultureInfo.InvariantCulture) + ";" +
                       Math.Round(point.Z * 304.8, 4).ToString(CultureInfo.InvariantCulture);
            return Math.Round(point.X, 4).ToString(CultureInfo.InvariantCulture) + ";" + Math.Round(point.Y, 4).ToString(CultureInfo.InvariantCulture) + ";" +
                      Math.Round(point.Z, 4).ToString(CultureInfo.InvariantCulture);
        }
    }
}
