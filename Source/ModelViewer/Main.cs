﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using Microsoft.Boogie;

namespace Microsoft.Boogie.ModelViewer
{
  public partial class Main : Form
  {
    public string SearchText;
    SkeletonItem unfoldingRoot;
    int currentState;
    IState[] states;

    // TODO this should be dynamically loaded
    IEnumerable<ILanguageProvider> Providers()
    {
      yield return Vcc.Provider.Instance;
      yield return Base.Provider.Instance;
    }

    public Main()
    {
      InitializeComponent();


      var args = Environment.GetCommandLineArgs();
      Model m;

      using (var rd = File.OpenText(args[1])) {
        var models = Model.ParseModels(rd);
        m = models[0];
      }

      ILanguageProvider prov = null;
      foreach (var p in Providers()) {
        if (p.IsMyModel(m)) {
          prov = p;
          break;
        }
      }
      
      var items = new List<ListViewItem>();
      states = prov.GetStates(m).ToArray();
      unfoldingRoot = new SkeletonItem(this, states.Length);
      unfoldingRoot.PopulateRoot(states);

      var idx = 0;
      foreach (var i in states) {
        var it = new ListViewItem(new string[] { idx.ToString(), i.Name, "" });
        it.Tag = i;
        items.Add(it);
        idx++;
      }
      stateList.Items.AddRange(items.ToArray());
      unfoldingRoot.Expanded = true;
      SetState(0);

      currentStateView.Columns[1].Width = currentStateView.Width - currentStateView.Columns[0].Width - 5;
    }

    void SetState(int id)
    {
      currentState = id;
      SyncListView();
    }

    internal void Activate(TreeNode treeNode)
    {
      throw new NotImplementedException();
    }

    private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
    {

    }

    const int levelMult = 16;
    const int plusWidth = 16;

    static StringFormat center = new StringFormat() { Alignment = StringAlignment.Center };
    static Pen plusPen = new Pen(Color.FromArgb(0xaa, 0xaa, 0xaa));
    static Brush grayedOut = new SolidBrush(Color.FromArgb(0xaa, 0xaa, 0xaa));
    
    private void listView1_DrawItem(object sender, DrawListViewItemEventArgs e)
    {
      var item = (DisplayItem)e.Item;
      var skel = item.skel;
      var rect = e.Bounds;
      rect.Y += 1;
      rect.Height -= 2;

      var textBrush = Brushes.Black;
      if (currentStateView.SelectedIndices.Count > 0 && currentStateView.SelectedIndices[0] == e.ItemIndex) {
        e.Graphics.FillRectangle(Brushes.Navy, rect);
        textBrush = Brushes.White;
      } else {
        e.Graphics.FillRectangle(Brushes.White, rect);
      }

      var off = levelMult * item.skel.level;

      {
        var plusRect = rect;
        plusRect.Width = plusWidth;
        plusRect.X += off;
        var plusBorder = plusRect;
        plusBorder.Height = 8;
        plusBorder.Width = 8;
        plusBorder.X += 4;
        plusBorder.Y += 3;        
        e.Graphics.DrawRectangle(plusPen, plusBorder);
        if (skel.Expandable) {
          float midX = plusBorder.X + plusBorder.Width / 2;
          float midY = plusBorder.Y + plusBorder.Height / 2;
          e.Graphics.DrawLine(plusPen, plusBorder.X + 2, midY, plusBorder.Right - 2, midY);
          if (!item.skel.expanded)
            e.Graphics.DrawLine(plusPen, midX, plusBorder.Y + 2, midX, plusBorder.Bottom - 2);
        }
      }

      off += plusWidth + 3;
      var nameRect = rect;
      var font = currentStateView.Font;

      if ((item.dispNode.State & NodeState.Changed) != 0)
        textBrush = Brushes.Red;
      if (!item.active)
        textBrush = grayedOut;

      var sz = e.Graphics.MeasureString(item.dispNode.Name.FullName(), font);
      nameRect.Width = currentStateView.Columns[0].Width - 1 - off;
      if (nameRect.Width < sz.Width + 2)
        nameRect.Width = (int)sz.Width + 20;
      nameRect.X += off;
      e.Graphics.DrawString(item.dispNode.Name.FullName(), font, textBrush, nameRect);

      var valRect = rect;
      valRect.X = nameRect.X + nameRect.Width + 4;
      valRect.Width = currentStateView.Width - valRect.X;
      e.Graphics.DrawString(item.SubItems[1].Text, font, textBrush, valRect); // , StringFormat.GenericDefault);
    }

    private void listView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
    {
    }

    private void listView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
    {
      e.DrawBackground();
      e.DrawText();
    }

    private void listView1_MouseMove(object sender, MouseEventArgs e)
    {
    }

    private void listView1_MouseUp(object sender, MouseEventArgs e)
    {
      var clickedItem = (DisplayItem)currentStateView.GetItemAt(5, e.Y);
      if (clickedItem != null) {
        clickedItem.Selected = true;
        clickedItem.Focused = true;

        var skel = clickedItem.skel;
        int plusLoc = skel.level * levelMult;
        if (skel.Expandable && e.X >= plusLoc && e.X <= plusLoc + plusWidth) {
          skel.Expanded = !skel.Expanded;
          SyncListView();
        }
      }
    }

    private void SyncListView()
    {
      var ch = unfoldingRoot.RecChildren.ToArray();
      var missing = ch.Length - currentStateView.Items.Count;

      currentStateView.BeginUpdate();
      if (missing < 0) {
        missing = -missing;
        while (missing-- > 0) {
          currentStateView.Items.RemoveAt(currentStateView.Items.Count - 1);
        }
      } else {
        while (missing-- > 0) {
          currentStateView.Items.Add(new DisplayItem());
        }
      }
      for (int i = 0; i < ch.Length; ++i)
        ((DisplayItem)currentStateView.Items[i]).Set(ch[i], currentState);
      currentStateView.EndUpdate();
      currentStateView.Invalidate();
    }

    private void listView1_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
    {
      currentStateView.Invalidate();
    }

    private void listView1_Resize(object sender, EventArgs e)
    {
      currentStateView.Invalidate();
    }

    private void stateList_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (stateList.SelectedItems.Count == 0) return;
      var sel = stateList.SelectedItems[0].Index;
      SetState(sel);
    }

    private void currentStateView_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (currentStateView.SelectedItems.Count == 0) return;
      var sel = (DisplayItem) currentStateView.SelectedItems[0];

      stateList.BeginUpdate();
      for (int i = 0; i < sel.skel.displayNodes.Length; ++i) {
        var dn = sel.skel.displayNodes[i];
        stateList.Items[i].SubItems[2].Text = DisplayItem.ValuesAsString(dn);
      }
      stateList.EndUpdate();
    }

  }

  internal class DisplayItem : ListViewItem
  {
    internal SkeletonItem skel;
    internal int stateId;
    internal bool active;
    internal IDisplayNode dispNode;

    internal void Set(SkeletonItem s, int id)
    {
      if (skel == s && stateId == id)
        return;
      skel = s;
      stateId = id;

      dispNode = skel.displayNodes[stateId];
      active = dispNode != null;

      if (dispNode == null) {
        for (int i = stateId; i < skel.displayNodes.Length; ++i) {
          dispNode = skel.displayNodes[i];
          if (dispNode != null) break;
        }
        for (int i = stateId; i >= 0; --i) {
          if (dispNode != null) break;
          dispNode = skel.displayNodes[i];
        }
      }

      var tooltip = dispNode.ToolTip;
      var values = ValuesAsString(dispNode);
      if (tooltip != null) {
        this.ToolTipText = tooltip;
      } else {
        this.ToolTipText = values;
      }

      this.SubItems[0].Text = dispNode.Name.FullName();
      this.SubItems[1].Text = values;
    }

    internal DisplayItem()
       : base(new string[] { "", "" })
    {
    }

    static internal string ValuesAsString(IDisplayNode dn)
    {
      if (dn == null) return "";

      var sb = new StringBuilder();
      foreach (var n in dn.Values) {
        sb.Append(n).Append(", ");
        if (sb.Length > 300)
          break;
      }
      if (sb.Length > 2) sb.Length -= 2;
      return sb.ToString();
    }
  }
}