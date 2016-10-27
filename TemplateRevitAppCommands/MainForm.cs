using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Revit_FlatExporter
{
  
    public partial class MainForm : Form
    {
        public static MainForm mf ;
        public static string EventName;
        private ExternalEventApplication m_Handler;
        private ExternalEvent m_ExEvent;
        public MainForm(UIApplication appRevit, ExternalEvent exEvent, ExternalEventApplication handler)
        {
            InitializeComponent();
            this.m_ExEvent = exEvent;
            this.m_Handler = handler;
            mf = this; 
        }

        public MainForm()
        {
            InitializeComponent();
            mf = this; 

        }

        void MethodExample()
        {
            EventName = "MethodExample";
            m_ExEvent.Raise();  //сигнал для обработки события
           
        }
    }
}
