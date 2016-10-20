using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_ApartmentBase.Model.DB.EntityModel;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase.AutoCAD
{
    public static class ElementFactory
    {
        private const string categoryWall = "Стены";
        private const string categoryDoor = "Двери";
        private const string categoryWin = "Окна";

        /// <summary>
        /// Создание элемента из автокадовского блока блока
        /// </summary>      
        public static ElementAC CreateElementDWG (BlockReference blRefElem, ModuleAC module, string blName, List<ParameterAC> parameters, string category)
        {
            ElementAC elem = null;

            if (category.Equals(categoryWall, StringComparison.OrdinalIgnoreCase))
            {
                elem = new WallElement(blRefElem, module, blName, parameters, category);
            }
            else if (category.Equals(categoryDoor, StringComparison.OrdinalIgnoreCase))
            {
                elem = new DoorElement(blRefElem, module, blName, parameters, category);                
            }
            else if (category.Equals(categoryWin, StringComparison.OrdinalIgnoreCase))
            {
                elem = new WindowElement(blRefElem, module, blName, parameters, category);
            }
            else
            {
                elem = new ElementAC(blRefElem, module, blName, parameters, category);
            }

            return elem;
        }

        ///// <summary>
        ///// Создание элемента из базы
        ///// </summary>      
        //public static Element CreateElementDB (Module module, F_nn_Elements_Modules emEnt)
        //{
        //    Element elem = null;
        //    string category = emEnt.F_S_Elements.F_S_Categories.NAME_RUS_CATEGORY;

        //    if (category.Equals(categoryWall, StringComparison.OrdinalIgnoreCase))
        //    {
        //        elem = new WallElement(module, emEnt);
        //    }
        //    else if (category.Equals(categoryDoor, StringComparison.OrdinalIgnoreCase))
        //    {
        //        elem = new DoorElement(module, emEnt);
        //    }
        //    else if (category.Equals(categoryWin, StringComparison.OrdinalIgnoreCase))
        //    {
        //        elem = new WindowElement(module, emEnt);
        //    }
        //    else
        //    {
        //        elem = new Element(module, emEnt);
        //    }
        //    return elem;
        //}
    }
}
