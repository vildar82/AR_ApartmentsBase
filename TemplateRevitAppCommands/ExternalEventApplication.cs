using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Revit_FlatExporter
{
 public   class ExternalEventApplication : IExternalEventHandler
    {
        public ExternalEventApplication() { }
   

        public void Execute(UIApplication app)
        {
            switch (MainForm.EventName)     //Здесь выполнять методы, связанные с транзакцией документа.
            {
                case "MethodExample": MethodExample(); break; 
                default:
                    break;
            }
        }

        public string GetName()
        {
            throw new NotImplementedException();
        }


        void MethodExample()
        {
            MessageBox.Show("");
        }
    }
}
