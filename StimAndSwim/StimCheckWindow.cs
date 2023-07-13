using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BehaveAndScanGECI
{
    public  partial class StimCheckWindow : Form 
    {
        SystemMenuManager menuManager;

        public StimCheckWindow()
        {
            this.Width  = 300;
            this.Height = 240;
            this.ControlBox = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;        
            this.menuManager = new SystemMenuManager(this, SystemMenuManager.MenuItemState.Removed);

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

    }
}
