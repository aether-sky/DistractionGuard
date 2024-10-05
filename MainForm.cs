using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DistractionGuard
{
  internal class MainForm : Form
  {
    RichTextBox activeLabel;
    Button activateButton;
    Button deactivateButton;
    bool active = false;
    int debugColor = 0;
    Dictionary<int, Color> tableDebugColors = new Dictionary<int, Color>();

    private void SetActivation(bool b)
    {
      active = b;
      DistractionGuard.SetActive(b);
      activeLabel.Clear();
      activeLabel.SelectionFont = new Font("Arial", 12, FontStyle.Regular);
      activeLabel.AppendText("DistractionGuard is ");
      activeLabel.SelectionFont = new Font("Arial", 12, FontStyle.Bold);
      if (active)
      {
        activateButton.Enabled = false;
        deactivateButton.Enabled = true;
        activeLabel.SelectionColor = Color.Green;
        activeLabel.AppendText("ACTIVE");
      }
      else
      {
        activateButton.Enabled = true;
        deactivateButton.Enabled = false;
        activeLabel.SelectionColor = Color.Red;
        activeLabel.AppendText("NOT ACTIVE");

      }
    }
    private void ToggleActivate()
    {
      SetActivation(!active);
    }
    private T MakeFillControl<T>(Func<T> make) where T : Control
    {
      var result = make();
      result.AutoSize = true;
      result.Dock = DockStyle.Fill;
      return result;
    }
    public MainForm() {
      Model m = Model.Load();
      this.Text = "Distraction Guard";
      this.Size = new Size(800, 600);
      this.Dock = DockStyle.Fill;
      this.Icon = LoadIcon();
      //supposedly helps flickering, but doesn't seem to
      this.SetStyle(ControlStyles.UserPaint, true);
      this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
      this.SetStyle(ControlStyles.DoubleBuffer, true);
      this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

      var mainTable = MakeTable("Main Table", 2, 1, 0);
      mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80));
      mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

      var leftTable = MakeTable("Left Table (Config)", 1, 4, 1);
      leftTable.RowStyles.Add(new RowStyle(SizeType.Absolute, GetLabelHeight() * 2));
      leftTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
      leftTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
      leftTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));

      var rightTable = MakeTable("Right Table (Activate/Deactivate)", 1, 2, 1);
      rightTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
      rightTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

      mainTable.Controls.Add(leftTable, 0, 0);
      mainTable.Controls.Add(rightTable, 1, 0);

      //LeftTable
      this.activeLabel = MakeFillControl(() => new RichTextBox());
      this.activeLabel.ReadOnly = true;
      this.activeLabel.BorderStyle = BorderStyle.None;
      leftTable.Controls.Add(this.activeLabel, 0, 0);

      //LeftTable.listView
      var dataTable = MakeTable("Data Table", 1, 2);
      var listView = new ListView()
      {
        Dock = DockStyle.Fill,
        View = View.Details
      };
      m.PopulateList(listView);
      leftTable.Controls.Add(listView, 0, 1);

      //LeftTable.OtherTable
      var otherTable = MakeTable("Other Table", 3, 1, 2);
      var otherLabel = new Label()
      {
        Text = "Other:",
        Dock = DockStyle.Right
      };
      var otherInput = MakeFillControl(() => new TextBox());
      otherTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, otherLabel.Width + 15));
      otherTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
      otherTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));

      otherTable.Controls.Add(otherLabel, 0, 0);
      otherTable.Controls.Add(otherInput, 1, 0);
      ToolTip otherTT = new ToolTip();
      otherTT.AutoPopDelay = 0;
      otherTT.InitialDelay = 10;
      var otherTTText = "Number of seconds to show the lock screen for windows not matching any rules above (default 0/excluded).";
      var ttControls = new List<Control>() { otherTable, otherLabel, otherInput };
      otherTT.SetToolTip(otherLabel, otherTTText);
      leftTable.Controls.Add(otherTable, 0, 2);

      //LeftTable.AddEdit
      var addEditTable = MakeTable("Add/Edit Table", 2, 1, 2);
      addEditTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
      addEditTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
      leftTable.Controls.Add(addEditTable, 0, 3);
      var addButton = MakeFillControl(() => new Button());
      addButton.Text = "Add";
      addEditTable.Controls.Add(addButton, 0, 0);
      var editButton = MakeFillControl(() => new Button());
      editButton.Text = "Edit";
      addEditTable.Controls.Add(editButton, 1, 0);

      //RightTable
      this.activateButton = MakeFillControl(() => new Button());
      activateButton.Text = "Activate";
      activateButton.Click += (sender, e) =>
      {
        SetActivation(true);
      };
      rightTable.Controls.Add(activateButton, 0, 0);
      this.deactivateButton = MakeFillControl(() => new Button());
      deactivateButton.Text = "Deactivate";
      deactivateButton.Enabled = false;
      deactivateButton.Click += (sender, e) =>
      {
        SetActivation(false);
      };
      rightTable.Controls.Add(deactivateButton, 0, 1);
      this.Controls.Add(mainTable);

      this.FormClosing += MainForm_Closing;

      SetActivation(false);
    }

    static int GetLabelHeight()
    {
      var label = new Label();
      Globals.Debug($"LABEL HEIGHT: {label.Height}");
      return label.Height;
    }

    private Icon? LoadIcon()
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      var iconPath = "DistractionGuard.iconsmall.png";
      using var pngStream = assembly.GetManifestResourceStream(iconPath);
      
      if (pngStream != null)
      {
        Globals.Debug("loaded");
        using var bitmap = new Bitmap(pngStream);
        return Icon.FromHandle(bitmap.GetHicon());
        
      }
      Globals.Debug($"Failed to load icon at {iconPath}");
      return null;
    }

    private TableLayoutPanel MakeTable(string name, int cols, int rows, int depth=0)
    {
      var result = new TableLayoutPanel()
      {
        AutoSize = true,
        Dock = DockStyle.Fill,
        ColumnCount = cols,
        RowCount = rows,
        CellBorderStyle = TableLayoutPanelCellBorderStyle.OutsetDouble
      };
      List<(Color,string)> colors = new List<(Color,string)> {
        (Color.AliceBlue, "AliceBlue"),
        (Color.Red, "Red"),
        (Color.Purple, "Purple"),
        (Color.Yellow, "Yellow"),
        (Color.Green, "Green"),
        (Color.Navy, "Navy"),
        (Color.AntiqueWhite, "AntiqueWhite"),
        (Color.Black, "Black"),
        (Color.Beige, "Beige"),
        (Color.Aqua, "Aqua"),
        (Color.Bisque, "Bisque"),
        (Color.DarkOrange, "DarkOrange")
      };
      var (color,cname) = colors[debugColor++];
      Globals.Debug($"Table {name} color {cname}");
      tableDebugColors[result.GetHashCode()] = color;
      result.CellPaint += (sender, e) =>
      {
        e.Graphics.DrawRectangle(new Pen(color), e.CellBounds);
        Rectangle adjustedRect = new Rectangle(
                   e.CellBounds.X + depth*2,  // Move right by 2 pixels
                   e.CellBounds.Y + depth*2,  // Move down by 2 pixels
                   e.CellBounds.Width - depth*4,  // Shrink width by 4 pixels
                   e.CellBounds.Height - depth*4  // Shrink height by 4 pixels
               );
        e.Graphics.FillRectangle(new SolidBrush(color), adjustedRect);
      };
      return result;

    }



    private void MainForm_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
      DistractionGuard.Close();
      /*if (MessageBox.Show("Are you sure you want to exit?", "Confirm exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
      {
        e.Cancel = true;
      }*/
    }

  }
}
