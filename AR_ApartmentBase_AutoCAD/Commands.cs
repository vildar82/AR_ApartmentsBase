﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Blocks;
using AcadLib.Blocks.Dublicate;
using AcadLib.Errors;
using AR_ApartmentBase_AutoCAD.Export;
using AR_ApartmentBase_AutoCAD.Utils;
using AR_ApartmentBase.Model;
using AR_ApartmentBase.Model.DB;
using AR_ApartmentBase.Model.DB.EntityModel;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcadLib;
using System.Diagnostics;

[assembly: CommandClass(typeof(AR_ApartmentBase_AutoCAD.Commands))]
[assembly: ExtensionApplication(typeof(AR_ApartmentBase_AutoCAD.Commands))]

namespace AR_ApartmentBase_AutoCAD
{
    public class Commands : IExtensionApplication
    {
        public static string DirExportApartments = string.Empty;
        public static string RegAppApartBase = "BaseApartment";

        public void Initialize()
        {
            // Загрузка сборок EF, MoreLinq
            LoadService.LoadEntityFramework();
            LoadService.LoadMorelinq();
            LoadService.LoadNetTopologySuite();
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
                             $"Имя блока квартиры должно соответствовать {OptionsAC.Instance.BlockApartmentNameMatch}");
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

            OptionsAC.Show();
        }

        /// <summary>
        /// Поиск квартир в Модели и экспорт в отдельные файлы каждой квартиры, и експорт в базу.
        /// </summary>
        [CommandMethod("PIK", "AR-BaseApartmentsExport", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void BaseApartmentsExport()
        {
            CommandStart.Start(doc =>
            {
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

                Database db = doc.Database;
                Editor ed = doc.Editor;

                ExportApartmentsAbout();

                ParameterAC.BlocksConstantAtrs = new Dictionary<ObjectId, List<Parameter>>();

                // Проверка дубликатов блоков    
                CheckDublicateBlocks.Tolerance = new Autodesk.AutoCAD.Geometry.Tolerance(0.02, 15);
                CheckDublicateBlocks.Check(new HashSet<string>() { "RV_EL_BS_Базовая стена", "RV_EL_BS_Вентиляционный блок" });

                // Создание папки для экспорта подложек квуартир                
                DefineDirExportFilesApartments(db);

                // Считывание блоков квартир из чертежа                
                var apartments = ApartmentAC.GetApartments(db);
                if (apartments.Count == 0)
                {
                    throw new System.Exception($"Блоки квартир не найдены. Имя блока квартиры должно соответствовать условию Match = '{OptionsAC.Instance.BlockApartmentNameMatch}'");
                }
                ed.WriteMessage($"\nВ Модели найдено {apartments.Count} блоков квартир.");

                Inspector.ShowDialog();
                Inspector.Clear();

                // Квартиры в базе
                //var apartmentsInBase = GetBaseApartments.GetAll();

                //Проверка всех элементов квартир в базе - категории, параметры.
                //CheckApartments.Check(apartments, apartmentsInBase);

                //// Сортировка квартир, модулей и элементов                
                //var alphaComparer = AcadLib.Comparers.AlphanumComparator.New;
                //apartments.Sort((a1, a2) => a1.Name.CompareTo(a2.Name));
                //apartments.ForEach(a =>
                //    {
                //        a.ModulesAC.Sort((m1, m2) => m1.Name.CompareTo(m2.Name));
                //        a.ModulesAC.ForEach(m => m.ElementsAC.Sort((e1, e2) => alphaComparer.Compare(e1.NodeName, e2.NodeName)));
                //    });

                //// Форма предпросмотра экспорта блоков
                //FormBlocksExport formExport = new FormBlocksExport(apartments);
                //var dlgRes = Application.ShowModalDialog(formExport);

                //// Прервать
                //if (dlgRes == System.Windows.Forms.DialogResult.Abort)
                //{
                //    formExport.SetModaless();
                //    Application.ShowModelessDialog(formExport);
                //    throw new System.Exception(AcadLib.General.CanceledByUser);
                //}

                //if (dlgRes == System.Windows.Forms.DialogResult.OK)
                //{
                // Экспорт блоков в файлы
                var count = ApartmentAC.ExportToFiles(apartments);
                ed.WriteMessage($"\nЭкспортированно '{count}' квартир в отдельные файлы.");

                //// Выбор квартир записываемых в базу - изменившиеся и новые
                //var apartsToDb = apartments.Where
                //         (a => !a.BaseStatus.HasFlag(EnumBaseStatus.Error) &&
                //               !a.BaseStatus.HasFlag(EnumBaseStatus.NotInDwg) &&
                //           (
                //                     a.BaseStatus.HasFlag(EnumBaseStatus.Changed) ||
                //                     a.BaseStatus.HasFlag(EnumBaseStatus.New) ||
                //                     a.Modules.Any(m => !m.BaseStatus.HasFlag(EnumBaseStatus.Error) &&
                //                                   (
                //                                       m.BaseStatus.HasFlag(EnumBaseStatus.Changed) ||
                //                                       m.BaseStatus.HasFlag(EnumBaseStatus.New)
                //                                   ))
                //           )).ToList();
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
                    // Преобразовать из ApartmentAC в Apartment
                    var sw = Stopwatch.StartNew();

                    BaseApartments.Export(apartments);

                    Logger.Log.Error($"На запись {apartments.Count} квартир в БД ушло {sw.Elapsed.TotalMinutes} минут.");
                }
                catch (System.Exception ex)
                {
                    Inspector.AddError($"Ошибка экспорта в БД - {ex.Message}", icon: System.Drawing.SystemIcons.Error);
                }

                // Запись лога экспортированных блоков      
                string logFile = Path.Combine(Path.GetDirectoryName(doc.Name), OptionsAC.Instance.LogFileName);
                ExcelLog excelLog = new ExcelLog(logFile);
                excelLog.AddtoLog(apartments);
            });
        }

        private void DefineDirExportFilesApartments(Database db)
        {
            string dirExport = OptionsAC.Instance.FolderExportApartments; //@"Z:\Revit_server\01. Libraries\Revit 2015\#Группы_квартиры & МОПы\Квартиры_PIK1_PIK1У_База квартир и МОПы";
            if (!Directory.Exists(dirExport))
            {
                dirExport = Path.Combine(Path.GetDirectoryName(db.Filename), @"Квартиры_" + Path.GetFileNameWithoutExtension(db.Filename));
                Directory.CreateDirectory(dirExport);
            }
            DirExportApartments = dirExport;            
        }

        /// <summary>
        /// Очистка базы квартир.
        /// </summary>
        [CommandMethod("PIK", "AR-BaseApartmentsClear", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void BaseApartmentsClear()
        {
            CommandStart.Start(doc =>
            {
                // Проверка допуска пользователя
                if (!AccessUsers.HasAccess())
                {
                    doc.Editor.WriteMessage("\nОтказано в доступе");
                    return;
                }
                BaseApartments.Clear();
            });
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
                var apartments = ApartmentAC.GetApartments(sel);
                AcadServices.ContourHelper.CreateContours2(apartments);
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
                var apartments = ApartmentAC.GetApartments(sel);
                AcadServices.ContourHelper.ClearOldContourAll(apartments);                
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
                var apartments = ApartmentAC.GetApartments(sel);
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