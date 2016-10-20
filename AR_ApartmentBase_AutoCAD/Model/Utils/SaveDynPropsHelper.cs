using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_ApartmentBase.AutoCAD.Utils
{
    public static class SaveDynPropsHelper
    {
        private static Dictionary<string, string> TranslatorValues = new Dictionary<string, string>
        {
            { "1", "1" },
            { "15_2-15#25_2-10", "9_1*15_1*18_6-18*25_11-25" },
            { "15_2-15#25_11-25", "9_1*15_1*18_6-18*25_11-25" },
            { "15_0#25_2-25", "9_0*15_0*18_2-5*25_2-10" },
            { "15_2-15#25_0", "9_0*15_0*18_2-5*25_2-10" }            
        };

        public static int Save()
        {
            List<SaveDynProp> propsToSave = new List<SaveDynProp>();
            Database db = HostApplicationServices.WorkingDatabase;
            using (var t = db.TransactionManager.StartTransaction())
            {
                var bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;                
                GetPropsToSave("RV_EL_BS_Базовая стена", bt, ref propsToSave);
                GetPropsToSave("RV_EL_BS_Вентиляционный блок", bt, ref propsToSave);

                string fileXml = Path.Combine(Path.GetDirectoryName(db.Filename), Path.GetFileNameWithoutExtension(db.Filename) + "_DynPropFloors.xml");

                SavePropsToXml(propsToSave, fileXml);
                t.Commit();
            }
            return propsToSave.Count;
        }

        public static int Load()
        {
            int resCount = 0;
            Database db = HostApplicationServices.WorkingDatabase;
            string fileXml = Path.Combine(Path.GetDirectoryName(db.Filename), Path.GetFileNameWithoutExtension(db.Filename) + "_DynPropFloors.xml");
            List<SaveDynProp> propsToSave = LoadPropsFromXml(fileXml);
            foreach (var savedProp in propsToSave)
            {
                Handle h = new Handle(savedProp.Handle);
                ObjectId idBlRef;
                if (db.TryGetObjectId(h, out idBlRef))
                {
                    using (var t = db.TransactionManager.StartTransaction())
                    {                        
                        var blRef = idBlRef.GetObject(OpenMode.ForWrite, false, true) as BlockReference;
                        if (blRef == null) continue;
                        foreach (DynamicBlockReferenceProperty prop in blRef.DynamicBlockReferencePropertyCollection)
                        {
                            if (prop.PropertyName.Equals("Floors"))
                            {
                                string newValue;
                                if (TranslatorValues.TryGetValue(savedProp.FloorValue, out newValue))
                                {
                                    if (!prop.Value.ToString().Equals(newValue))
                                    {
                                        prop.Value = newValue;
                                    }
                                    resCount++;
                                }
                                else
                                {
                                    string blName = blRef.GetEffectiveName();           
                                    Inspector.AddError($"В блоке '{blName}' не найдено соответствие для параметра {savedProp.FloorValue}",
                                        System.Drawing.SystemIcons.Error);
                                }
                                break;
                            }
                        }
                        t.Commit();
                    }
                }
            }
            return resCount;
        }        

        public static void GetPropsToSave(string blName, BlockTable bt, ref List<SaveDynProp> propsToSave)
        {            
            if (bt.Has(blName))
            {
                var btr = bt[blName].GetObject(OpenMode.ForRead) as BlockTableRecord;
                var idsBlRef = btr.GetBlockReferenceIds(true, false);                
                IterateIdsBlRef(idsBlRef, blName, ref propsToSave);

                var idsBtrAnonyms = btr.GetAnonymousBlockIds();
                foreach (ObjectId idBtrAnonym in idsBtrAnonyms)
                {
                    var btrAnonym = idBtrAnonym.GetObject(OpenMode.ForRead) as BlockTableRecord;
                    var idsBlRefAnonym = btrAnonym.GetBlockReferenceIds(true, false);
                    IterateIdsBlRef(idsBlRefAnonym, blName, ref propsToSave);
                }                
            }            
        }

        private  static void IterateIdsBlRef(ObjectIdCollection idsblRefsWall, string blName, ref List<SaveDynProp> propsToSave)
        {
            foreach (ObjectId item in idsblRefsWall)
            {                
                var blRef = item.GetObject(OpenMode.ForRead, false, true) as BlockReference;
                if (blRef == null) continue;
                foreach (DynamicBlockReferenceProperty prop in blRef.DynamicBlockReferencePropertyCollection)
                {
                    if (prop.PropertyName.Equals("Floors"))
                    {
                        propsToSave.Add(new SaveDynProp(blRef.Handle.Value, prop.Value.ToString()));
                        break;
                    }
                }
            }
        }


        private static void SavePropsToXml(List<SaveDynProp> propsToSave, string  fileXmlToSave)
        {
            AcadLib.Files.SerializerXml ser = new AcadLib.Files.SerializerXml(fileXmlToSave);
            ser.SerializeList(propsToSave);
        }

        private static List<SaveDynProp> LoadPropsFromXml(string fileXmlToSave)
        {
            AcadLib.Files.SerializerXml ser = new AcadLib.Files.SerializerXml(fileXmlToSave);
            return ser.DeserializeXmlFile<List<SaveDynProp>>();
        }

        public static void GetDyns()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (var t = db.TransactionManager.StartTransaction())
            {
                var ms = db.CurrentSpaceId.GetObject(OpenMode.ForRead) as BlockTableRecord;

                t.Commit();
            }
        }
    }
}
