using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ServiceListener
{
    public partial class settings : Form
    {
        public settings()
        {
            InitializeComponent();

            initData();
        }

        private void initData()
        {
            combxSetType.SelectedIndex = 0;
            combxWeek.SelectedIndex = 0;

            combxServiceList.Items.Add("EAServer");
            combxServiceList.SelectedIndex = 0;
            combxServiceList2.Items.Add("EAServer");
        }


        //取消设置
        private void btnCancel_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 0;
        }

        //设置
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string servername = combxServiceList.SelectedItem.ToString();
            combxServiceList2.SelectedItem = servername;
        }
    }
}
