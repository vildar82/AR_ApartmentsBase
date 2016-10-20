using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Blocks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_ApartmentBase.AutoCAD.Utils
{
    public static class RemoveDublicateAttributes
    {
        static HashSet<ObjectId> testedBtrs;

        public static int Remove()
        {
            int count = 0;
            testedBtrs = new HashSet<ObjectId>();
            Database db = HostApplicationServices.WorkingDatabase;
            using (var t = db.TransactionManager.StartTransaction())
            {
                var btr = db.CurrentSpaceId.GetObject(OpenMode.ForRead) as BlockTableRecord;
                count += removeInBtr(btr);
                t.Commit();
            }
            return count;
        }

        private static int removeInBtr(BlockTableRecord btr)
        {
            if (!testedBtrs.Add(btr.Id))
            {
                return 0;
            }

            int count = 0;

            List<ObjectId> innerBtrs = new List<ObjectId>();

            foreach (var idEnt in btr)
            {
                using (var ent = idEnt.GetObject(OpenMode.ForRead, false, true))
                {
                    if (ent is BlockReference)
                    {
                        var blRef = (BlockReference)ent;
                        var attRefs = AttributeInfo.GetAttrRefs(blRef);
                        var dublAttr = attRefs.GroupBy(a => a.Tag).Where(g => g.Skip(1).Any());
                        foreach (var atrGroup in dublAttr)
                        {
                            foreach (var atrInfo in atrGroup.Skip(1))
                            {
                                var atrRef = atrInfo.IdAtr.GetObject(OpenMode.ForWrite, false, true) as AttributeReference;
                                atrRef.Erase();
                                count++;
                            }
                        }
                        innerBtrs.Add(blRef.BlockTableRecord);
                    }
                }
            }

            foreach (var idInnerBtr in innerBtrs)
            {
                using (var innerBtr = idInnerBtr.GetObject(OpenMode.ForRead) as BlockTableRecord)
                {
                    count +=removeInBtr(innerBtr);
                }
            }
            return count;
        }
    }
}
