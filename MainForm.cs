using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistractionGuard
{
  internal class MainForm : Form
  {
    public MainForm() {
      this.Text = "Distraction Guard";
      this.Size = new Size(800, 600);
      Button activateButton = new Button();
      activateButton.Text = "Activate";
      activateButton.Location = new System.Drawing.Point(150, 100);
      activateButton.Size = new Size(400, 400);
      this.Controls.Add(activateButton);

      activateButton.Click += (sender, e) =>
      {
        MessageBox.Show("Button clicked!");
      };
      this.FormClosing += MainForm_Closing;
    }
    private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      DistractionGuard.Close();
      if (MessageBox.Show("Are you sure you want to exit?", "Confirm exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
      {
        e.Cancel = true;
      }
    }
  }
}
