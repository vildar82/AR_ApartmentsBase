using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit_FlatExporter
{
   public static class Helper
    {
       public static List<string> Categories = new List<string>()
       {
           "Стены", "Двери", "Окна", "Сантехнические приборы", "Мебель", "Силовые электроприборы",
           "Электрооборудование","Перекрытия","Разделитель помещений","Помещения","Потолки"
       }; 
    }
}
