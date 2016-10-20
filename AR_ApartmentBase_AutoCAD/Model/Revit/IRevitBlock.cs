using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.AutoCAD
{
    /// <summary>
    /// Блок ревитовского элемента
    /// </summary>
    public interface IRevitBlock
    {
        /// <summary>
        /// Имя элемнта
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Точка вставки блока.
        /// </summary>
        Point3d Position { get; }

        /// <summary>
        /// Поворот блока
        /// </summary>      
        double Rotation { get; }

        /// <summary>
        /// Направление - единичный вектор
        /// </summary>
        string Direction { get; }
        string LocationPoint { get; }

        /// <summary>
        /// Параметры элемента
        /// </summary>
        List<ParameterAC> ParametersAC { get; }

        /// <summary>
        /// Границы блока в Модели
        /// </summary>
        Extents3d ExtentsInModel { get; }

        /// <summary>
        /// Трансформация блока.
        /// </summary>
        Matrix3d BlockTransform { get; }

        /// <summary>
        /// Описание ошибки элемента если есть.
        /// </summary>
        AcadLib.Errors.Error Error { get; }

        /// <summary>
        /// Статус элемента блока квартиры в базек данных - есть, нет, изменился
        /// </summary>
        EnumBaseStatus BaseStatus { get; }

        /// <summary>
        /// Соответствующая запись в базе данных
        /// </summary>
        object DBObject { get; set; }
                
        ObjectId IdBlRef { get; set; }

        string NodeName { get; }
        string Info { get; }

        ObjectId[] GetSubentPath();
    }
}
