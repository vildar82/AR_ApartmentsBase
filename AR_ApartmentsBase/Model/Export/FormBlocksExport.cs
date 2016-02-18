using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using System.IO;
using AR_ApartmentBase.Model.Revit;

namespace AR_ApartmentBase.Model.Export
{
   public partial class FormBlocksExport : Form
   {      
      private Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

      public FormBlocksExport(List<Apartment> apartments)
      {
         InitializeComponent();

         labelCount.Text = apartments.Count.ToString();

         fillTreeView(apartments);        
      }

      private void treeViewApartments_DoubleClick(object sender, EventArgs e)
      {
         buttonShow_Click(null, null);
      }

      private void buttonShow_Click(object sender, EventArgs e)
      {
         // TreeView         
         var block = treeViewApartments.SelectedNode?.Tag as IRevitBlock;
         if (block !=null)
         {
            ed.Zoom(block.ExtentsInModel);
         }
      }      

      private void buttonOptions_Click(object sender, EventArgs e)
      {
         Options.Show();
      }

      private void fillTreeView(List<Apartment> apartments)
      {
         treeViewApartments.Nodes.Clear();
         foreach (var apart in apartments)
         {
            TreeNode nodeApart = new TreeNode(apart.BlockName);
            nodeApart.Tag = (IRevitBlock)apart;
            treeViewApartments.Nodes.Add(nodeApart);

            foreach (var module in apart.Modules)
            {
               TreeNode nodeModule = new TreeNode(module.BlockName);
               nodeApart.Nodes.Add(nodeModule);
               nodeModule.Tag = (IRevitBlock)module;

               foreach (var elem in module.Elements)
               {
                  TreeNode nodeElem = new TreeNode(elem.BlockName);
                  nodeModule.Nodes.Add(nodeElem);
                  nodeElem.Tag = (IRevitBlock)elem;
               }
            }
         }
      }

      private void treeViewApartments_DrawNode(object sender, DrawTreeNodeEventArgs e)
      {
         Brush brush = null;
         IRevitBlock rBlock = e.Node.Tag as IRevitBlock;
         if (rBlock != null)
         {
            if (rBlock.BaseStatus.HasFlag(EnumBaseStatus.Error))
            {
               // Ошибка
               brush = Brushes.Red;
            }
            else if (rBlock.BaseStatus.HasFlag(EnumBaseStatus.NotInBase))
            {
               // Новый
               brush = Brushes.Green;
            }
            else if (rBlock.BaseStatus.HasFlag(EnumBaseStatus.NotInDwg))
            {
               // Нет в чертеже, но есть в базе
               brush = Brushes.Pink;
            }
            else if (rBlock.BaseStatus.HasFlag(EnumBaseStatus.Changed))
            {
               // Изменился
               brush = Brushes.Yellow;
            }
            else if (rBlock.BaseStatus == EnumBaseStatus.OK)
            {
               // Не изменился
               brush = Brushes.Blue;
            }
         }
         else
         {
            brush = Brushes.Black;
         }

         //e.Graphics.FillRectangle(brush, e.Node.Bounds);

         // Retrieve the node font. If the node font has not been set,
         // use the TreeView font.
         System.Drawing.Font nodeFont = e.Node.NodeFont;
         if (nodeFont == null) nodeFont = ((TreeView)sender).Font;

         // Draw the node text.
         e.Graphics.DrawString(e.Node.Text, nodeFont, brush,
             Rectangle.Inflate(e.Bounds, 2, 0));


         //// If the node has focus, draw the focus rectangle large, making
         //// it large enough to include the text of the node tag, if present.
         //if ((e.State & TreeNodeStates.Focused) != 0)
         //{
         //   using (Pen focusPen = new Pen(Color.Black))
         //   {
         //      focusPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
         //      Rectangle focusBounds = e.Node.Bounds;
         //      focusBounds.Size = new Size(focusBounds.Width - 1,
         //      focusBounds.Height - 1);
         //      e.Graphics.DrawRectangle(focusPen, focusBounds);
         //   }
         //}
      }

      private void treeViewApartments_AfterSelect(object sender, TreeViewEventArgs e)
      {
         textBoxInfo.Text = string.Empty;
         var rBlock = e?.Node?.Tag as IRevitBlock;
         if (rBlock != null)
         {
            if (rBlock.Error != null)
            {
               textBoxInfo.Text = rBlock.Error.Message;
            }
            else
            {
               textBoxInfo.Text = "Элемент совпадает с базой.";
            }
         }
      }
   }
}
