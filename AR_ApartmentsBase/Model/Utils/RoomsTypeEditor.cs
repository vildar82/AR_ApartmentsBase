﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using AR_ApartmentBase.Model.Revit;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.Model.Utils
{
    public static class RoomsTypeEditor
    {
        public static void SetRoomsType(List<Apartment> apartments)
        {            
            Database db = HostApplicationServices.WorkingDatabase;
            using (var t = db.TransactionManager.StartTransaction())
            {
                foreach (var apart in apartments)
                {
                    string typeFlatValue = getRoomType(apart.Layer);
                    if (string.IsNullOrEmpty(typeFlatValue))
                    {
                        Inspector.AddError($"Не определен параметр типа квартиры '{Options.Instance.ApartmentTypeFlatParameter}' по слою {apart.Layer} блока квартиры {apart.Name}",
                            apart.IdBlRef, System.Drawing.SystemIcons.Error);
                        continue;
                    }
                    // Добавление атрибута в блок квартиры и во все вхождения
                    addAttrTypeFlat(apart, typeFlatValue);
                }
                t.Commit();
            }            
        }        

        private static string getRoomType(string layer)
        {
            var splitDash = layer.Split('_');
            if (splitDash.Length>2)
            {
                return splitDash[2];
            }
            return null;
        }

        private static void addAttrTypeFlat(Apartment apart, string typeFlatValue)
        {
            var btrApart = apart.IdBtr.GetObject(OpenMode.ForRead) as BlockTableRecord;

            var atrDeftypeFlat = defineAtrDefTypeFlat(btrApart, typeFlatValue);           
            
            if (atrDeftypeFlat.Constant) return;

            var idsBlRef = btrApart.GetBlockReferenceIds(true, true);
            foreach (ObjectId idBlRef in idsBlRef)
            {
                using (var blRef = idBlRef.GetObject(OpenMode.ForRead, false, true) as BlockReference)
                {
                    bool isFind = false;
                    if (blRef.AttributeCollection != null)
                    {
                        foreach (ObjectId idAtr in blRef.AttributeCollection)
                        {
                            using (var atr = idAtr.GetObject(OpenMode.ForRead, false, true) as AttributeReference)
                            {
                                if (atr == null || !atr.Tag.Equals(Options.Instance.ApartmentTypeFlatParameter, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }
                                atr.UpgradeOpen();
                                atr.TextString = typeFlatValue;
                                atr.Invisible = true;
                                atr.LockPositionInBlock = true;
                                isFind = true;
                                break;
                            }
                        }
                    }
                    if (!isFind)
                    {
                        // Добавление атрибута к вхождению блока
                        AttributeReference atrRef = new AttributeReference();
                        atrRef.SetAttributeFromBlock(atrDeftypeFlat, blRef.BlockTransform);
                        atrRef.TextString = typeFlatValue;
                        blRef.UpgradeOpen();
                        blRef.AttributeCollection.AppendAttribute(atrRef);
                        blRef.Database.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(atrRef, true);
                    }
                }
            }
        }

        private static AttributeDefinition defineAtrDefTypeFlat(BlockTableRecord btrApart, string typeFlatValue)
        {            
            // Поиск существующего атрибута типа квартиры
            if (btrApart.HasAttributeDefinitions)
            {
                foreach (var idEnt in btrApart)
                {
                    if (idEnt.ObjectClass.Name != "AcDbAttributeDefinition") continue;
                    var atrDef = idEnt.GetObject(OpenMode.ForRead, false, true) as AttributeDefinition;
                    if (atrDef == null || !atrDef.Tag.Equals(Options.Instance.ApartmentTypeFlatParameter)) continue;
                    atrDef.UpgradeOpen();
                    atrDef.TextString = typeFlatValue;
                    atrDef.Invisible = true;
                    atrDef.LockPositionInBlock = true;
                    atrDef.DowngradeOpen();
                    return atrDef;
                }
            }
            // Создание определения атрибута и добавление в блок
            AttributeDefinition atrDefTypeFlat = new AttributeDefinition(Point3d.Origin, typeFlatValue,
                Options.Instance.ApartmentTypeFlatParameter, 
                "Параметр типа квартиры - Студия, 1комн, и т.д.", btrApart.Database.GetTextStylePIK());
            atrDefTypeFlat.Invisible = true;
            atrDefTypeFlat.LockPositionInBlock = true;
            btrApart.UpgradeOpen();
            btrApart.AppendEntity(atrDefTypeFlat);
            btrApart.Database.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(atrDefTypeFlat, true);
            btrApart.DowngradeOpen();
            return atrDefTypeFlat;
        }
    }
}
