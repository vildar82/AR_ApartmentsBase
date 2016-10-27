using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AcadLib.Geometry;
using AR_ApartmentBase.Model;
using AR_ApartmentBase.Model.DB.EntityModel;
using AR_ApartmentBase.Model.Elements;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase_AutoCAD
{
    public class WallHostBase : ElementAC, IWallHost
    {
        public List<IElement> HostWall { get; set; }

        public WallHostBase (BlockReference blRefElem, ApartmentAC apart, string blName, List<Parameter> parameters, string category)
              : base(blRefElem, apart, blName, parameters, category)
        {
            // Добавление параметра idWall - 0 - условно
            Parameters.Add(new Parameter(OptionsAC.Instance.HostWallParameter, ""));
            DefineOrientation(blRefElem);
        }        

        /// <summary>
        ///  Поиск блока стены по границам стен и точке вставки блока двери
        /// </summary>
        public void SearchHostWallDwg (List<IElement> elements)
        {
            this.HostWall = new List<IElement>();
            var walls = elements.OfType<WallElement>();
            foreach (var wall in walls)
            {
                // Попадает ли точка вствавки блока двери в границы стены
                if (wall.Contour == null)
                {
                    if (wall.ExtentsClean.IsPointInBounds(Position))
                    {
                        HostWall.Add(wall);
                    }
                }
                else
                {
                    if (wall.Contour.IsPointInsidePolygon(Position))
                    {
                        HostWall.Add(wall);
                    }
                }
            }
            // Ошибка если не найдена стена
            if (this.HostWall.Count == 0)
            {
                Inspector.AddError($"Не определена стена для элемента {FamilySymbolName}. ",
                      ExtentsInModel, IdBlRef, System.Drawing.SystemIcons.Error);
                // Исключить дверь из элементов модуля - и дверь не будет записана в базк
                elements.Remove(this);
            }
        }

        /// <summary>
        /// Определение стены для двери в объектах созданных не из чертежа, а из базы
        /// </summary>
        public void SearchHostWallDB ()
        {
            HostWall = new List<IElement>();
            // найти стену по id в параметре двери
            var paramIdWall = Parameters.Single(p => p.Name.Equals(OptionsAC.Instance.HostWallParameter, StringComparison.OrdinalIgnoreCase));
            var idsWall = paramIdWall.Value.Split(';');
            foreach (var item in idsWall)
            {
                int idWall = Convert.ToInt32(item);
                // найти стену в элементах модуля         
                var wall = Apartment.Elements.SingleOrDefault(e => ((F_nn_Elements_Modules)e.DBObject).ID_ELEMENT_IN_FLAT == idWall);
                HostWall.Add(wall);
            }
        }        
    }
}
