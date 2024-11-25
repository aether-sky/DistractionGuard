using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

/**
 * TODO: make sure secs isn't 0
 * 
 */
namespace DistractionGuard
{
  internal class MainForm : Form
  {
    [DllImport("user32.dll")]
    public static extern bool HideCaret(IntPtr hWnd);

    RichTextBox activeLabel;
    Button activateButton;
    Button deactivateButton;
    Button editButton;
    Button deleteButton;
    ListView modelList;
    bool active = false;
    int debugColor = 0;
    Dictionary<int, Color> tableDebugColors = new Dictionary<int, Color>();
    bool enableDebugColors = false;

    private void SetActivation(bool b)
    {
      active = b;
      DistractionGuard.SetActive(b);
      activeLabel.Clear();
      activeLabel.SelectionFont = new Font("Arial", 12, FontStyle.Regular);
      activeLabel.SelectionColor = Color.Black;
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
      hideCaret();
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

    private void hideCaret()
    {
      HideCaret(activeLabel.Handle);
    }

    public MainForm() {
      this.Text = "Distraction Guard";
      this.Size = new Size(800, 600);
      this.Dock = DockStyle.Fill;
      this.Icon = LoadIcon();
      this.FormBorderStyle = FormBorderStyle.FixedSingle;
      this.MaximizeBox = false;
      //supposedly helps flickering, but doesn't seem to
      this.SetStyle(ControlStyles.UserPaint, true);
      this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
      this.SetStyle(ControlStyles.DoubleBuffer, true);
      this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

      var mainTable = MakeTable("Main Table", 2, 1, 0);
      mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80));
      mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

      var leftTable = MakeTable("Left Table (Config)", 1, 3, 1);
      leftTable.RowStyles.Add(new RowStyle(SizeType.Absolute, GetLabelHeight() * 2));
      leftTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
      leftTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));

      var rightTable = MakeTable("Right Table (Activate/Deactivate)", 1, 2, 1);
      rightTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
      rightTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

      mainTable.Controls.Add(leftTable, 0, 0);
      mainTable.Controls.Add(rightTable, 1, 0);

      //LeftTable.Banner
      this.activeLabel = MakeFillControl(() => new RichTextBox());
      this.activeLabel.Enabled = false;
      this.activeLabel.BorderStyle = BorderStyle.None;
      hideCaret();
      leftTable.Controls.Add(this.activeLabel, 0, 0);

      //LeftTable.ListView
      this.modelList = new ListView
      {
        Dock = DockStyle.Fill,
        View = View.Details,
        MultiSelect = false,
        FullRowSelect = true
      };

      modelList.SelectedIndexChanged += ModelList_SelectedIndexChanged;
      modelList.Resize += (sender, e) =>
      {
        ResizeModelList();
      };
      leftTable.Controls.Add(this.modelList, 0, 1);
      Model.PopulateList(this.modelList);

      //LeftTable.AddEdit
      var addEditTable = MakeTable("Add/Edit Table", 4, 1, 2);
      addEditTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
      addEditTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
      addEditTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
      addEditTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
      leftTable.Controls.Add(addEditTable);
      var addButton = MakeFillControl(() => new Button());
      addButton.Text = "Add";
      addButton.Click += makeAddEditButton_Click(false); 
      addEditTable.Controls.Add(addButton, 0, 0);
      this.editButton = MakeFillControl(() => new Button());
      editButton.Text = "Edit";
      editButton.Enabled = false;
      editButton.Click += makeAddEditButton_Click(true);
      addEditTable.Controls.Add(editButton, 1, 0);
      this.deleteButton = MakeFillControl(() => new Button());
      deleteButton.Text = "Remove";
      deleteButton.Enabled = false;
      deleteButton.Click += (sender, e) =>
      {
        var selected = modelList.SelectedItems;
        var removes = new List<int>();
        foreach (var sel in selected)
        {
          if (sel != null)
          {
            var item = (ListViewItem)sel;
            Model.RemovePattern(item.Text);
            removes.Add(item.Index);
          }
        }
        removes.Reverse();
        foreach (var i in removes)
        {
          modelList.Items.RemoveAt(i);
        }
      };
      
      addEditTable.Controls.Add(deleteButton, 2, 0);
      var optionsButton = MakeFillControl(() => new Button());
      optionsButton.Text = "Options";
      optionsButton.Click += OptionsButton_Click;
      addEditTable.Controls.Add(optionsButton, 3, 0);

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

    private void ResizeModelList()
    {
      if (modelList.Columns.Count > 1)
      {
        modelList.Columns[0].Width = (int)(modelList.ClientSize.Width * 0.7);
        modelList.Columns[1].Width = (int)(modelList.ClientSize.Width * 0.3);
      }
    }

    private void OptionsButton_Click(object? sender, EventArgs e)
    {
      var optionsForm = new Form()
      {
        FormBorderStyle = FormBorderStyle.FixedSingle,
        Width = 500
      };
      var mainTable = MakeTable("Main options table", 1, 2);
      mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
      mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
      var optionsTable = MakeTable("Options Table", 2, 1, 1);
      var otherLabel = MakeFillControl(() => new Label());
      otherLabel.Text = "Other windows:";
      var otherttText = "Number of seconds to show the lock screen for windows not matching any configured pattern (default 0/excluded).";
      attachToolTip(otherLabel, otherttText);
      var otherInput = MakeFillControl(() => new TextBox());
      otherInput.KeyPress += makeKeypressHandler();
      otherInput.Text = Model.GetOtherSecs();
      optionsTable.Controls.Add(otherLabel, 0, 0);
      optionsTable.Controls.Add(otherInput, 1, 0);
      optionsForm.Controls.Add(optionsTable);
      mainTable.Controls.Add(optionsTable, 0, 0);

      var buttonsTable = MakeTable("Options Button Table", 2, 0, 1);
      buttonsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
      buttonsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

      var okButton = MakeFillControl(() => new Button());
      okButton.Text = "Save";
      okButton.Click += (sender, e) =>
      {
        var otherSec = otherInput.Text;
        int secs = -1;
        if ((int.TryParse(otherSec, out secs) && secs > -1) || otherSec.Length == 0) 
        {
          Model.UpdateOption("other", secs);
        }
        optionsForm.Close();
      };
      var cancelButton = MakeFillControl(() => new Button());
      cancelButton.Text = "Cancel";
      cancelButton.Click += (sender, e) =>
      {
        optionsForm.Close();
      };
      buttonsTable.Controls.Add(okButton, 0, 0);
      buttonsTable.Controls.Add(cancelButton, 1, 0);

      mainTable.Controls.Add(buttonsTable, 0, 1);
      optionsForm.Controls.Add(mainTable);
      optionsForm.ShowDialog();
    }

    private void attachToolTip(Control control, string text)
    {
      ToolTip tooltip = new ToolTip();
      tooltip.AutoPopDelay = 0;
      tooltip.InitialDelay = 10;
      tooltip.SetToolTip(control, text);
    }

    private void ModelList_SelectedIndexChanged(object? sender, EventArgs e)
    {
      if (modelList.SelectedItems.Count > 0)
      {
        editButton.Enabled = true;
        deleteButton.Enabled = true;
      }
      else
      {
        editButton.Enabled = false;
        deleteButton.Enabled = false;
      }
    }

    private KeyPressEventHandler makeKeypressHandler()
    {
      return (sender, e) =>
      {
        e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
      };
    }

    private Form MakeAddModal(bool isEdit)
    {
      var type = isEdit ? "Edit" : "Add";
      var result = new Form
      {
        MinimizeBox = false,
        MaximizeBox = false,
        Width = 700,
        Height = 500,
        FormBorderStyle = FormBorderStyle.FixedSingle
      };

      var table = MakeTable($"{type}->Main Table", 1, 2, 0);
      table.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
      var lowerTable = MakeTable($"{type}->Lower Table", 2, 3, 1);
      var instruction = new Label();
      instruction.AutoSize = true;
      instruction.Width = 600;
      instruction.Dock = DockStyle.Left;

      instruction.Text = @"
        The window title will be matched according to the regular
        expression added below. Plaintext will be treated as an 
        exact match. Put .* on either side of the input to match
        any window containing the input.";

      table.Controls.Add(instruction, 0, 0);
      table.Controls.Add(lowerTable, 0, 1);
      string selPattern = null;
      int selSecs = -1;
      if (isEdit)
      {
        if (modelList.SelectedIndices.Count > 0)
        {
          var item = modelList.SelectedItems[0];
          selPattern = item.Text;
          selSecs = int.Parse(item.SubItems[1].Text);
        }
        else
        {
          isEdit = false;
        }

      }
      var strLabel = MakeFillControl(() => new Label());
      strLabel.Text = "String to match:";
      var strInput = MakeFillControl(() => new TextBox());
      if (selPattern != null)
      {
        strInput.Text = selPattern;
      }
      var secLabel = MakeFillControl(() => new Label());
      secLabel.Text = "Timeout seconds:";
      var secInput = MakeFillControl(() => new TextBox());
      if (selSecs != -1)
      {
        secInput.Text = selSecs.ToString();
      }
      secInput.KeyPress += makeKeypressHandler();

      var addButton = MakeFillControl(() => new Button());
      
      addButton.Text = isEdit? "Save" : "Add";
      addButton.Click += (sender, e) =>
      {
        int secs;
        var parsed = int.TryParse(secInput.Text, out secs);
        if (!parsed) {
          secs = 15;
        }
        if (isEdit && selPattern != null)
        {
          Model.RemovePattern(selPattern);
        }
        Model.AddPattern(strInput.Text, secs);
        
        result.Close();
        Model.PopulateList(modelList);
        
        ResizeModelList();
      };

      var cancelButton = MakeFillControl(() => new Button());
      cancelButton.Text = "Cancel";
      cancelButton.Click += (sender, e) =>
      {
        result.Close();
      };

      lowerTable.Controls.Add(strLabel, 0, 0);
      lowerTable.Controls.Add(strInput, 1, 0);
      lowerTable.Controls.Add(secLabel, 0, 1);
      lowerTable.Controls.Add(secInput, 1, 1);
      lowerTable.Controls.Add(addButton, 0, 2);
      lowerTable.Controls.Add(cancelButton, 1, 2);

      lowerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, strLabel.Width + 20));
      lowerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
      lowerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

      result.Controls.Add(table);

      return result;
    }
    private EventHandler makeAddEditButton_Click(bool isEdit)
    {
      return (sender, e) =>
      {
        var modal = MakeAddModal(isEdit);
        modal.ShowDialog();
      };
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      hideCaret(); // Ensure the caret is hidden on form load
    }

    static int GetLabelHeight()
    {
      var label = new Label();
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
        //CellBorderStyle = TableLayoutPanelCellBorderStyle.OutsetDouble
      };
      if (enableDebugColors)
      {
        List<(Color, string)> colors = new List<(Color, string)> {
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
        var (color, cname) = colors[debugColor++ % colors.Count];

        Globals.Debug($"Table {name} color {cname}");
        tableDebugColors[result.GetHashCode()] = color;
        result.CellPaint += (sender, e) =>
        {
          e.Graphics.DrawRectangle(new Pen(color), e.CellBounds);
          Rectangle adjustedRect = new Rectangle(
                     e.CellBounds.X + depth * 2,  // Move right by 2 pixels
                     e.CellBounds.Y + depth * 2,  // Move down by 2 pixels
                     e.CellBounds.Width - depth * 4,  // Shrink width by 4 pixels
                     e.CellBounds.Height - depth * 4  // Shrink height by 4 pixels
                 );
          e.Graphics.FillRectangle(new SolidBrush(color), adjustedRect);
        };
      }
      return result;

    }

    private void MainForm_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
      DistractionGuard.Close();
    }

  }
  }
