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
using AcadLib;

namespace AR_ApartmentBase.AutoCAD.Export
{
    public partial class FormBlocksExport : Form
    {
        private Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

        private Brush brushNotInBase = Brushes.Lime;
        private Brush brushError = Brushes.Red;
        private Brush brushNotInDwg = Brushes.DarkViolet;
        private Brush brushChanged = Brushes.Olive;
        private Brush brushOk = Brushes.Blue;

        private Color colorNotInBase = Color.Lime;
        private Color colorError = Color.Red;
        private Color colorNotInDwg = Color.DarkViolet;
        private Color colorChanged = Color.Olive;
        private Color colorOk = Color.Blue;

        public FormBlocksExport(List<ApartmentAC> apartments)
        {
            InitializeComponent();

            labelChanged.ForeColor = colorChanged;
            labelOK.ForeColor = colorOk;
            labelError.ForeColor = colorError;
            labelNotInBase.ForeColor = colorNotInBase;
            labelNotInDwg.ForeColor = colorNotInDwg;

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
            if (block != null && !block.IdBlRef.IsNull)
            {
                ed.Zoom(block.ExtentsInModel);
                if (block is ApartmentAC)
                {
                    block.IdBlRef.FlickObjectHighlight(2, 100, 100);
                }
                else
                {
                    ObjectIdExt.FlickSubentityHighlight(block.GetSubentPath(), 2, 100, 100);
                }
            }
        }

        private void buttonOptions_Click(object sender, EventArgs e)
        {
            OptionsAC.Show();
        }

        private void fillTreeView(List<ApartmentAC> apartments)
        {
            treeViewApartments.Nodes.Clear();
            foreach (var apart in apartments)
            {
                TreeNode nodeApart = new TreeNode(apart.NodeName);
                nodeApart.Tag = (IRevitBlock)apart;
                nodeApart.ForeColor = BaseColor.GetColor(apart.BaseStatus);
                treeViewApartments.Nodes.Add(nodeApart);

                foreach (var module in apart.Modules)
                {
                    TreeNode nodeModule = new TreeNode(module.NodeName);
                    nodeApart.Nodes.Add(nodeModule);
                    nodeModule.Tag = (IRevitBlock)module;
                    nodeModule.ForeColor = BaseColor.GetColor(module.BaseStatus);

                    foreach (var elem in module.Elements)
                    {
                        TreeNode nodeElem = new TreeNode(elem.NodeName);
                        nodeModule.Nodes.Add(nodeElem);
                        nodeElem.Tag = (IRevitBlock)elem;
                        nodeElem.ForeColor = BaseColor.GetColor(elem.BaseStatus);
                    }
                }
                nodeApart.Expand();
            }
        }

        public void SetModaless()
        {
            buttonOk.Visible = false;
            buttonBreak.Visible = false;
            buttonCancel.Visible = false;
        }

        //private void treeViewApartments_DrawNode(object sender, DrawTreeNodeEventArgs e)
        //{
        //   Brush brush = Brushes.Black;
        //   IRevitBlock rBlock = e.Node.Tag as IRevitBlock;
        //   if (rBlock != null)
        //   {
        //      if (rBlock.BaseStatus.HasFlag(EnumBaseStatus.Error))
        //      {
        //         // Ошибка               
        //         brush = this.brushError;// Brushes.Red;
        //      }
        //      else if (rBlock.BaseStatus.HasFlag(EnumBaseStatus.New))
        //      {
        //         // Новый
        //         brush = this.brushNotInBase;  //Brushes.Lime;
        //      }
        //      else if (rBlock.BaseStatus.HasFlag(EnumBaseStatus.NotInDwg))
        //      {
        //         // Нет в чертеже, но есть в базе
        //         brush = this.brushNotInDwg; //Brushes.DarkViolet;
        //      }
        //      else if (rBlock.BaseStatus.HasFlag(EnumBaseStatus.Changed))
        //      {
        //         // Изменился
        //         brush = this.brushChanged; //Brushes.Olive;
        //      }
        //      else if (rBlock.BaseStatus == EnumBaseStatus.OK)
        //      {
        //         // Не изменился
        //         brush = this.brushOk; // Brushes.Blue;
        //      }
        //   }         

        //   //e.Graphics.FillRectangle(brush, e.Node.Bounds);

        //   // Retrieve the node font. If the node font has not been set,
        //   // use the TreeView font.
        //   System.Drawing.Font nodeFont = e.Node.NodeFont;
        //   if (nodeFont == null) nodeFont = ((TreeView)sender).Font;

        //   // Draw the node text.
        //   e.Graphics.DrawString(e.Node.Text, nodeFont, brush,
        //       Rectangle.Inflate(e.Bounds, 2, 0));


        //   //// If the node has focus, draw the focus rectangle large, making
        //   //// it large enough to include the text of the node tag, if present.
        //   //if ((e.State & TreeNodeStates.Focused) != 0)
        //   //{
        //   //   using (Pen focusPen = new Pen(Color.Black))
        //   //   {
        //   //      focusPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
        //   //      Rectangle focusBounds = e.Node.Bounds;
        //   //      focusBounds.Size = new Size(focusBounds.Width - 1,
        //   //      focusBounds.Height - 1);
        //   //      e.Graphics.DrawRectangle(focusPen, focusBounds);
        //   //   }
        //   //}
        //}

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
                textBoxInfo.Text += "\r\n" + rBlock.Info;
            }
        }

        private void ToolStripMenuItemExpandAll_Click(object sender, EventArgs e)
        {
            treeViewApartments.ExpandAll();
        }

        private void свернутьВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewApartments.CollapseAll();
        }

        private void ToolStripMenuItemExpandApart_Click(object sender, EventArgs e)
        {
            foreach (TreeNode nodeApart in treeViewApartments.Nodes)
            {
                nodeApart.Expand();
            }
        }
    }
}
