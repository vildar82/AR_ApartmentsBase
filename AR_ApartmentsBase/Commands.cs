using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Blocks;
using AcadLib.Blocks.Dublicate;
using AcadLib.Errors;
using AR_ApartmentBase.Model;
using AR_ApartmentBase.Model.DB;
using AR_ApartmentBase.Model.DB.EntityModel;
using AR_ApartmentBase.Model.Export;
using AR_ApartmentBase.Model.Revit;
using AR_ApartmentBase.Model.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(AR_ApartmentBase.Commands))]
[assembly: ExtensionApplication(typeof(AR_ApartmentBase.Commands))]

namespace AR_ApartmentBase
{
    public class Commands : IExtensionApplication
    {
        public static string DirExportApartments = string.Empty;
        public static string RegAppApartBase = "BaseApartment";

        public void Initialize()
        {
            // Загрузка сборок EF, MoreLinq
            AcadLib.LoadService.LoadEntityFramework();
            AcadLib.LoadService.LoadMorelinq();
            AcadLib.LoadService.LoadNetTopologySuite();
        }

        public void Terminate()
        {

        }

        [CommandMethod("PIK", "AR-BaseApartmentsAbout", CommandFlags.Modal)]
        public void ExportApartmentsAbout()
        {
            Logger.Log.Info("Start command AR-BaseApartmentsAbout");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;

            ed.WriteMessage($"\nПрограмма экспорта блоков квартир. Версия {Assembly.GetExecutingAssembly().GetName().Version}");
            ed.WriteMessage("\nКоманды:");
            ed.WriteMessage("\nAR-BaseApartmentsExport - экспорт блоков квартир найденных в Модели в отдельные файлы и в базу.\n" +
                             $"Имя блока квартиры должно соответствовать {Options.Instance.BlockApartmentNameMatch}");
            ed.WriteMessage("\nAR-BaseApartmentsOptions - настройки программы.");
            ed.WriteMessage("\nAR-BaseApartmentsAbout - описание программы.");
            ed.WriteMessage("\nAR-BaseApartmentsClear - очистка базы.");
            ed.WriteMessage("\nAR-BaseApartmentsContour - построение контура квартир с заливкой.");
            ed.WriteMessage("\nAR-BaseApartmentsContourRemove - удаление контура и заливки из квартир.");
            ed.WriteMessage("\nAR-BaseApartmentsSetTypeFlats - установка атрибута типа квартиры по слою квартиры.");
            ed.WriteMessage("\nAR-BaseApartmentsPlacement - расстановка квартир из папки с подложками.");
            ed.WriteMessage("\nAR-BaseApartmentsremoveDublicateAttributes - удаление звдублированных атрибутов.");            
        }

        [CommandMethod("PIK", "AR-BaseApartmentsOptions", CommandFlags.Modal)]
        public void ExportApartmentsOptions()
        {
            Logger.Log.Info("Start command AR-BaseApartmentsOptions");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Проверка допуска пользователя
            if (!AccessUsers.HasAccess())
            {
                doc.Editor.WriteMessage("\nОтказано в доступе");
                return;
            }

            Options.Show();
        }

        /// <summary>
        /// Поиск квартир в Модели и экспорт в отдельные файлы каждой квартиры, и експорт в базу.
        /// </summary>
        [CommandMethod("PIK", "AR-BaseApartmentsExport", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void BaseApartmentsExport()
        {
            Logger.Log.Info("Start command AR-BaseApartmentsExport");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Проверка допуска пользователя
            if (!AccessUsers.HasAccess())
            {
                doc.Editor.WriteMessage("\nОтказано в доступе");
                return;
            }

            if (!File.Exists(doc.Name))
            {
                doc.Editor.WriteMessage("\nНужно сохранить текущий чертеж.");
                return;
            }

            try
            {
                Inspector.Clear();

                Database db = doc.Database;
                Editor ed = doc.Editor;

                ExportApartmentsAbout();

                Parameter.BlocksConstantAtrs = new Dictionary<ObjectId, List<Parameter>>();

                // Проверка дубликатов блоков    
                CheckDublicateBlocks.Tolerance = new Autodesk.AutoCAD.Geometry.Tolerance(0.02, 15);                
                CheckDublicateBlocks.Check(new HashSet<string>() { "RV_EL_BS_Базовая стена", "RV_EL_BS_Вентиляционный блок" });                

                // Создание папки для экспорта подложек квуартир
                DirExportApartments = Path.Combine(Path.GetDirectoryName(db.Filename), @"Квартиры_" + Path.GetFileNameWithoutExtension(db.Filename));
                Directory.CreateDirectory(DirExportApartments);

                // Считывание блоков квартир из чертежа                
                var apartments = Apartment.GetApartments(db);
                if (apartments.Count == 0)
                {
                    throw new System.Exception($"Блоки квартир не найдены. Имя блока квартиры должно соответствовать условию Match = '{Options.Instance.BlockApartmentNameMatch}'");
                }
                ed.WriteMessage($"\nВ Модели найдено {apartments.Count} блоков квартир.");

                Inspector.ShowDialog();
                Inspector.Clear();                

                // Квартиры в базе
                var apartmentsInBase = GetBaseApartments.GetAll();

                //Проверка всех элементов квартир в базе - категории, параметры.
                CheckApartments.Check(apartments, apartmentsInBase);

                // Сортировка квартир, модулей и элементов                
                var alphaComparer = AcadLib.Comparers.AlphanumComparator.New;
                apartments.Sort((a1, a2) => a1.Name.CompareTo(a2.Name));
                apartments.ForEach(a =>
                    {
                        a.Modules.Sort((m1, m2) => m1.Name.CompareTo(m2.Name));
                        a.Modules.ForEach(m => m.Elements.Sort((e1, e2) => alphaComparer.Compare(e1.NodeName, e2.NodeName)));
                    });

                // Форма предпросмотра экспорта блоков
                FormBlocksExport formExport = new FormBlocksExport(apartments);
                var dlgRes = Application.ShowModalDialog(formExport);

                // Прервать
                if (dlgRes == System.Windows.Forms.DialogResult.Abort)
                {
                    formExport.SetModaless();
                    Application.ShowModelessDialog(formExport);
                    throw new System.Exception(AcadLib.General.CanceledByUser);
                }

                if (dlgRes == System.Windows.Forms.DialogResult.OK)
                {
                    // Экспорт блоков в файлы
                    var count = Apartment.ExportToFiles(apartments);
                    ed.WriteMessage($"\nЭкспортированно '{count}' квартир в отдельные файлы.");

                    // Выбор квартир записываемых в базу - изменившиеся и новые
                    var apartsToDb = apartments.Where
                             (a => !a.BaseStatus.HasFlag(EnumBaseStatus.Error) &&
                                   !a.BaseStatus.HasFlag(EnumBaseStatus.NotInDwg) &&
                               (
                                         a.BaseStatus.HasFlag(EnumBaseStatus.Changed) ||
                                         a.BaseStatus.HasFlag(EnumBaseStatus.New) ||
                                         a.Modules.Any(m => !m.BaseStatus.HasFlag(EnumBaseStatus.Error) &&
                                                       (
                                                           m.BaseStatus.HasFlag(EnumBaseStatus.Changed) ||
                                                           m.BaseStatus.HasFlag(EnumBaseStatus.New)
                                                       ))
                               )).ToList();
                    //var apartsNotToDB = apartments.Except(apartsToDb);
                    //foreach (var apartNotToDB in apartsNotToDB)
                    //{
                    //    ed.WriteMessage($"\nКвартира не будет записана в базу, статус '{apartNotToDB.BaseStatus}' - '{apartNotToDB.Name}'.");
                    //}

                    //// Запись квартир в xml
                    //string fileXml = Path.Combine(Path.GetDirectoryName(doc.Name), Path.GetFileNameWithoutExtension(doc.Name) + ".xml");               
                    //Apartment.ExportToXML(fileXml, apartmentsToExport);               

                    // Запись в DB    
                    try
                    {
                        BaseApartments.Export(apartsToDb);
                    }
                    catch (System.Exception ex)
                    {
                        Inspector.AddError($"Ошибка экспорта в БД - {ex.Message}", icon: System.Drawing.SystemIcons.Error);
                    }

                    // Запись лога экспортированных блоков      
                    string logFile = Path.Combine(Path.GetDirectoryName(doc.Name), Options.Instance.LogFileName);
                    ExcelLog excelLog = new ExcelLog(logFile);
                    excelLog.AddtoLog(apartments);

                    // Показ ошибок                    
                    Inspector.Show();                    
                }
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nОшибка экспорта блоков: {ex.Message}");
                if (!ex.Message.Contains(AcadLib.General.CanceledByUser))
                {                    
                    Logger.Log.Error(ex, $"Command: AR-BaseApartmentsExport. {doc.Name}");
                }
            }
        }

        /// <summary>
        /// Очистка базы квартир.
        /// </summary>
        [CommandMethod("PIK", "AR-BaseApartmentsClear", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void BaseApartmentsClear()
        {
            Logger.Log.Info("Start command AR-BaseApartmentsClear");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Проверка допуска пользователя
            if (!AccessUsers.HasAccess())
            {
                doc.Editor.WriteMessage("\nОтказано в доступе");
                return;
            }

            try
            {
                BaseApartments.Clear();
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nОшибка очистки базы: {ex.Message}");
                if (!ex.Message.Contains("Отменено пользователем"))
                {
                    Logger.Log.Error(ex, $"Command: AR-BaseApartmentsClear. {doc.Name}");
                }
            }
        }

        /// <summary>
        /// Контур квартир
        /// </summary>
        [CommandMethod("PIK", "AR-BaseApartmentsContour", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void BaseApartmentsContour()
        {
            Logger.Log.Info("Start command AR-BaseApartmentsContour");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            try
            {
                Inspector.Clear();
                var sel = ed.SelectBlRefs("Выбери квартиры");
                var apartments = Apartment.GetApartments(sel);
                Model.AcadServices.ContourHelper.CreateContours2(apartments);
                Inspector.Show();
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nОшибка : {ex.Message}");
                if (!ex.Message.Contains("Отменено пользователем"))
                {
                    Logger.Log.Error(ex, $"Command: AR-BaseApartmentsContour. {doc.Name}");
                }
            }
        }

        /// <summary>
        /// Контур квартир
        /// </summary>
        [CommandMethod("PIK", "AR-BaseApartmentsContourRemove", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void BaseApartmentsContourRemove()
        {
            Logger.Log.Info("Start command AR-BaseApartmentsContourRemove");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            try
            {
                Inspector.Clear();
                var sel = ed.SelectBlRefs("Выбери квартиры");
                var apartments = Apartment.GetApartments(sel);
                Model.AcadServices.ContourHelper.ClearOldContourAll(apartments);                
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nОшибка : {ex.Message}");
                if (!ex.Message.Contains(AcadLib.General.CanceledByUser))
                {
                    Logger.Log.Error(ex, $"Command: AR-BaseApartmentsContourRemove. {doc.Name}");
                }
            }
        }

        /// <summary>
        /// Сохранение дин параметра и восстановление
        /// </summary>
        [CommandMethod("PIK", "AR-BaseApartmentsDynPropSave", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void BaseApartmentsDynPropSave()
        {
            Logger.Log.Info("Start command AR-BaseApartmentsSaveDynProp");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            try
            {
                Inspector.Clear();                
                var count = SaveDynPropsHelper.Save();
                ed.WriteMessage($"\nСохранено параметров из {count} блоков.");
                Inspector.Show();
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nОшибка : {ex.Message}");
                if (!ex.Message.Contains("Отменено пользователем"))
                {
                    Logger.Log.Error(ex, $"Command: AR-BaseApartmentsDynPropSave. {doc.Name}");
                }
            }
        }

        /// <summary>
        /// Сохранение дин параметра и восстановление
        /// </summary>
        [CommandMethod("PIK", "AR-BaseApartmentsDynPropLoad", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void BaseApartmentsDynPropLoad()
        {
            Logger.Log.Info("Start command AR-BaseApartmentsDynPropLoad");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            try
            {
                Inspector.Clear();
                var count = SaveDynPropsHelper.Load();
                ed.WriteMessage($"\nВосстановлено параметров в {count} блоков.");
                Inspector.Show();
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nОшибка : {ex.Message}");
                if (!ex.Message.Contains("Отменено пользователем"))
                {
                    Logger.Log.Error(ex, $"Command: AR-BaseApartmentsDynPropLoad. {doc.Name}");
                }
            }
        }

        /// <summary>
        /// Запись параметра имени квартиры в помещения
        /// </summary>
        [CommandMethod("PIK", "AR-BaseApartmentsSetTypeFlats", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void BaseApartmentsSetTypeFlats()
        {
            Logger.Log.Info("Start command AR-BaseApartmentsSetTypeFlats");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            try
            {
                Inspector.Clear();
                var sel = ed.SelectBlRefs("Выбери квартиры");
                var apartments = Apartment.GetApartments(sel);
                RoomsTypeEditor.SetRoomsType(apartments);
                ed.WriteMessage($"\nПараметры записаны.");
                Inspector.Show();
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nОшибка : {ex.Message}");
                if (!ex.Message.Contains(AcadLib.General.CanceledByUser))
                {
                    Logger.Log.Error(ex, $"Command: AR-BaseApartmentsSetTypeFlats. {doc.Name}");
                }
            }
        }

        /// <summary>
        /// Расстановка квартир из папки с подложками
        /// </summary>
        [CommandMethod("PIK", "AR-BaseApartmentsPlacement", CommandFlags.Modal 
            | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor | CommandFlags.Session)]
        public void BaseApartmentsPlacement()
        {
            Logger.Log.Info("Start command AR-BaseApartmentsPlacement");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            using (doc.LockDocument())
            {
                try
                {
                    Inspector.Clear();
                    ApartmentPlacement.Placement();                    
                    Inspector.Show();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage($"\nОшибка : {ex.Message}");
                    if (!ex.Message.Contains(AcadLib.General.CanceledByUser))
                    {
                        Logger.Log.Error(ex, $"Command: AR-BaseApartmentsPlacement. {doc.Name}");
                    }
                }
            }
        }

        /// <summary>
        /// Расстановка квартир из папки с подложками
        /// </summary>
        [CommandMethod("PIK", "AR-BaseApartmentsremoveDublicateAttributes", CommandFlags.Modal
            | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void BaseApartmentsremoveDublicateAttributes()
        {
            Logger.Log.Info("Start command AR-BaseApartmentsremoveDublicateAttributes");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            using (doc.LockDocument())
            {
                try
                {
                    Inspector.Clear();
                    int count = RemoveDublicateAttributes.Remove();
                    ed.WriteMessage($"Удалено {count} дублирующихся атрибутов.");
                    Inspector.Show();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage($"\nОшибка : {ex.Message}");
                    if (!ex.Message.Contains(AcadLib.General.CanceledByUser))
                    {
                        Logger.Log.Error(ex, $"Command: AR-BaseApartmentsremoveDublicateAttributes. {doc.Name}");
                    }
                }
            }
        }
    }
}        