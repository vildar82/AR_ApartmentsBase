using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit_FlatExporter
{
    class ExternalApplication : IExternalApplication
    {

        public static ExternalApplication thisApp = null;
       private MainForm mainForm;
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            mainForm = null;
            thisApp = this;
            return Result.Succeeded;
        }

        public void ShowMainForm(UIApplication appRevit)
        {
            ExternalEventApplication handler = new ExternalEventApplication();    
            ExternalEvent exEvent = ExternalEvent.Create(handler);                //Создаем событие
            mainForm = new MainForm(appRevit, exEvent, handler);                 
            mainForm.Show();

        }
    }
}
