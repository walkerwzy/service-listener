using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.ServiceProcess;

namespace setup
{
    public partial class Form1 : Form
    {
        private List<string> weeks = new List<string>();
        private List<string> tasks = new List<string>();
        private string inipath = "";//ConfigurationSettings.AppSettings["inipath"];
        private XDocument xdoc = null;
        private bool jumpflag = false;//一个不执行listbox的change里任何代码的标志，防止listbox重绑数据源后自动触发该事件

        private bool isSaved = true;//是否保存的全局标志

        public Form1()
        {
            InitializeComponent();

            inipath = AppDomain.CurrentDomain.BaseDirectory;//获取基目录，它由程序集冲突解决程序用来探测程序集。 
            inipath += "config.xml";
            xdoc = XDocument.Load(inipath);
            //任务列表
            getTaskList();
            //服务
            ServiceController[] services = ServiceController.GetServices();
            List<MyItem> mi = new List<MyItem>();
            //mi.Add(new MyItem("请选择服务...", "-1"));
            foreach (ServiceController item in services)
            {
                mi.Add(new MyItem(item.DisplayName, item.ServiceName));
            }
            Helper.ddlBindData(combxService, mi, "name", "value", true);
            isSaved = true;
            initData(0);
            timer1.Start();
        }


        private void initData(int? sindex)
        {
            if (!sindex.HasValue)
            {
                sindex = 0;
            }
            BindingSource bs = new BindingSource();
            bs.DataSource = tasks;
            listBox1.DataSource = bs;
            if (tasks.Count > 0)
            {
                listBox1.SelectedIndex = sindex.Value;
            }
            else//当配置文件里一个任务也没有时，人为新增一个
            {
                button3_Click(null, null);
            }
            //初始化放在列表切换事件做

        }

        //保存，应用
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrent();
                xdoc.Save(inipath);
                //保存后即把主键更新为保存后的结果
                txtTaskName.Tag = txtTaskName.Text.Trim();
                isSaved = true;
                button1.Enabled = false;
                MessageBox.Show("保存成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失败\n" + ex.Message);
            }
        }

        //星期勾选
        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cbx = (CheckBox)sender;
            if (cbx.Checked)
            {
                weeks.Add(cbx.Tag.ToString());
            }
            else
            {
                weeks.Remove(cbx.Tag.ToString());
            }
            if (weeks.Count == 0)
            {
                MessageBox.Show("请至少选择一天");
                cbx.Checked = true;
                return;
            }
            triggerSaveButton();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool savedBefore = isSaved;//保存标签切换的时候的保存状态
            //切换菜单之前保存当前所有状态到内存...
            if ((sender == null && e == null) || tasks.Count == 0 || jumpflag)
            {
                return;
            }
            if (!isSaved)//删除操作不保存状态，标记为已保存不保存状态
            {

                try
                {
                    SaveCurrent();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("操作失败\n" + ex.Message);
                    return;
                }

            }

            //保存数据后，再利用切换选项来初始化本页
            combxType.SelectedIndex = 0;
            lblServiceName.Text = combxService.SelectedValue.ToString();
            txtArgs.Text = "";
            numericUpDown1.Value = (decimal)10;
            numericUpDown2.Value = (decimal)1;
            checkBox1.Checked = false; checkBox2.Checked = true; checkBox3.Checked = false; checkBox4.Checked = false;
            checkBox5.Checked = false; checkBox6.Checked = false; checkBox7.Checked = false; checkBox8.Checked = true;
            dateTimePicker1.Value = Convert.ToDateTime("2010-10-10 04:00:00");
            if (!weeks.Contains("2"))
            {
                weeks.Add("2");
            }
            txtTaskName.Text = listBox1.SelectedItem.ToString();
            txtTaskName.Tag = listBox1.SelectedItem.ToString();//保存进入的时候的任务名，退出的时候用，避免使用过程中文本框的值被改，它相当于主键

            //if (tasks.Count == 0 || (listBox1.DataSource == null))
            //{
            //    return;//所有任务删除的情况下，就不执行了。
            //}
            //加载任务数据
            try
            {
                XElement svNode = xdoc.Descendants("Task").Where(x => x.Attribute("Name").Value == listBox1.SelectedItem.ToString()).SingleOrDefault();
                if (svNode == null)
                {
                    return;
                }
                XElement scanNode = svNode.Descendants("Scan").SingleOrDefault();
                XElement scheduleNode = svNode.Descendants("Restart").SingleOrDefault();
                txtTaskName.Text = svNode.Attribute("Name").Value;
                numericUpDown1.Value = decimal.Parse(scanNode.Attribute("Interval").Value);
                txtArgs.Text = svNode.Attribute("ServiceArgs").Value;
                try
                {
                    combxService.SelectedValue = svNode.Attribute("ServiceName").Value;
                    //lblStatue.Text = Helper.ServiceStatus(combxService.SelectedValue.ToString());
                }
                catch (NullReferenceException)
                {
                    MessageBox.Show("服务\"" + svNode.Attribute("ServiceName").Value + "\"已不存在，可能已经被卸载");
                    combxService.SelectedIndex = 0;
                    lblServiceName.Text = combxService.SelectedValue.ToString();
                }
                lblServiceName.Text = svNode.Attribute("ServiceName").Value;
                setTaskType(scheduleNode.Attribute("Type").Value);
                dateTimePicker1.Value = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd ") + scheduleNode.Attribute("Time").Value);
                string strday = scheduleNode.Attribute("Day").Value;
                if (combxType.SelectedIndex == 0)
                {
                    setWeek(strday);
                    numericUpDown2.Value = decimal.Parse("1");
                    panel1.Visible = false;
                }
                else
                {
                    numericUpDown2.Value = decimal.Parse(strday);
                    panel2.Visible = false;
                    numericUpDown2.Enabled = true;
                    if (combxType.SelectedIndex == 2)
                    {
                        panel1.Visible = false;
                    }
                }
                checkBox8.Checked = svNode.Attribute("Enabled").Value.ToLower() == "true";
                isSaved = savedBefore;//绑定完数据后恢复保存状态
                button1.Enabled = !isSaved;
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取失败\n" + ex.Message);
            }
        }

        private void SaveCurrent()
        {
            //移除旧节点
            string v = txtTaskName.Tag.ToString();
            XElement xnode = xdoc.Descendants("Task").Where(x => x.Attribute("Name").Value == v).SingleOrDefault();
            xnode.Remove();
            //将新数据推入内存
            //判断配置文件中的day参数（因天，月，周不同）
            string day = "";
            string tasktype = "";
            switch (combxType.SelectedIndex)
            {
                case 0:
                    tasktype = "Week";
                    for (int i = 0; i < weeks.Count; i++)
                    {
                        day += weeks[i] + ",";
                    }
                    day = day.TrimEnd(',');
                    break;
                case 1:
                    tasktype = "Month";
                    day = ((int)numericUpDown2.Value).ToString();
                    break;
                default:
                    tasktype = "Day";
                    day = "1";
                    break;
            }

            string newname = string.IsNullOrEmpty(txtTaskName.Text.Trim()) ? getNewTaskName() : txtTaskName.Text.Trim();
            XElement newTask = new XElement("Task",
                new XAttribute("Name", newname),
                new XAttribute("ServiceName", lblServiceName.Text.Trim()),
                new XAttribute("ServiceArgs", txtArgs.Text.Trim()),
                new XAttribute("ServiceDisplay", combxService.SelectedItem.ToString()),
                new XAttribute("Enabled", checkBox8.Checked.ToString()),
                new XElement("Scan", new XAttribute("Interval", ((int)numericUpDown1.Value).ToString())),
                new XElement("Restart", new XAttribute("Type", tasktype),
                    new XAttribute("Time", dateTimePicker1.Value.ToString("HH:mm")),
                    new XAttribute("Day", day)));
            xdoc.Root.Add(newTask);


            //一旦执行过该方法，说明数据可能被改动过，则应该提醒用户保存
            isSaved = false;

            //更新任务列表，以免任务名被更改过
            tasks.Clear();
            getTaskList();//从新的xdoc读出任务列表
            jumpflag = true;
            initData(listBox1.SelectedIndex);
            jumpflag = false;

        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblServiceName.Text = combxService.SelectedValue.ToString();
            triggerSaveButton();
        }

        //退出
        private void button2_Click(object sender, EventArgs e)
        {
            if (!isSaved)
            {
                if (DialogResult.Yes == MessageBox.Show("是否保存对配置文件所做的修改？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                {
                    button1_Click(null, null);
                }
            }
            Application.Exit();
        }

        private void combxType_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (combxType.SelectedIndex)
            {
                case 0://周
                    panel2.Visible = true;
                    panel1.Visible = false;
                    break;
                case 1://月
                    panel2.Visible = false;
                    panel1.Visible = true;
                    break;
                default://天
                    panel2.Visible = false;
                    panel1.Visible = false;
                    break;
            }
            triggerSaveButton();
        }
        //删除任务
        private void button4_Click(object sender, EventArgs e)
        {
            string v = listBox1.SelectedItem.ToString();
            int i = listBox1.SelectedIndex;
            if (--i < 0)
            {
                i = 0;
            }
            XElement xnode = xdoc.Descendants("Task").Where(x => x.Attribute("Name").Value == v).SingleOrDefault();
            xnode.Remove();
            tasks.Remove(v);
            isSaved = true;//标志listbox重绑数据源后不去调用saveCurrent方法，本质上，saveCurrent是往内存中插数据
            initData(i);
            isSaved = false;//删除过，一定是未保存状态了
            button1.Enabled = true;
        }

        //新建任务
        private void button3_Click(object sender, EventArgs e)
        {
            //表现层
            string newname = getNewTaskName();
            tasks.Add(newname);

            //内存xml也新增基础数据
            XElement newTask = new XElement("Task",
                new XAttribute("Name", newname),
                new XAttribute("ServiceName", combxService.SelectedValue.ToString()),
                new XAttribute("ServiceArgs", ""),
                new XAttribute("ServiceDisplay", combxService.SelectedItem.ToString()),
                new XAttribute("Enabled", "True"),
                new XElement("Scan", new XAttribute("Interval", 10),
                new XElement("Restart", new XAttribute("Type", "Week"),
                    new XAttribute("Time", "04:00"),
                    new XAttribute("Day", 2))));
            xdoc.Root.Add(newTask);

            initData(null);
            txtTaskName.Text = newname;
            txtTaskName.Tag = newname;
            //isSaved = true;
            listBox1.SelectedItem = newname;
        }

        //确定
        private void button5_Click(object sender, EventArgs e)
        {
            if (!isSaved)
            {
                button1_Click(null, null);
            }
            Application.Exit();
        }

        #region methods

        //任务名是否存在
        private bool isTaskNameExist(string name)
        {
            //内存xml里重复
            if (xdoc.Descendants("Task").Where(t => t.Attribute("Name").Value == name).Count() > 0)
                return true;
            //listbox重复
            foreach (object item in listBox1.Items)
            {
                if (item.ToString() == name)
                {
                    return true;
                }
            }
            return false;
        }
        //得到新任务名
        private string getNewTaskName()
        {
            string newname = "新任务";
            int i = 1;
            while (isTaskNameExist(newname))
            {
                newname = "新任务" + i++.ToString();
            }
            return newname;
        }

        //获取任务列表
        private void getTaskList()
        {
            var q = xdoc.Descendants("Task");
            foreach (var item in q)
            {
                string v = item.Attribute("Name").Value;
                if (!tasks.Contains(v))
                {
                    tasks.Add(v);
                }
            }
        }


        //设置任务类型
        private void setTaskType(string typename)
        {
            switch (typename.ToLower())
            {
                case "week":
                    combxType.SelectedIndex = 0;
                    break;
                case "month":
                    combxType.SelectedIndex = 1;
                    break;
                default:
                    combxType.SelectedIndex = 2;
                    break;
            }
        }

        //设置星期
        private void setWeek(string weekdays)
        {
            string[] ws = weekdays.Split(',');
            if (ws.Contains("1"))
            {
                checkBox1.Checked = true;
            }
            if (ws.Contains("2"))
            {
                checkBox2.Checked = true;
            }
            else
            {
                checkBox2.Checked = false;//因为页面加载默认勾选了周二
            }
            if (ws.Contains("3"))
            {
                checkBox3.Checked = true;
            }
            if (ws.Contains("4"))
            {
                checkBox4.Checked = true;
            }
            if (ws.Contains("5"))
            {
                checkBox5.Checked = true;
            }
            if (ws.Contains("6"))
            {
                checkBox6.Checked = true;
            }
            if (ws.Contains("7"))
            {
                checkBox7.Checked = true;
            }
        }

        //激活保存按钮
        private void triggerSaveButton()
        {
            isSaved = false;
            button1.Enabled = true;
        }

        #endregion

        private void txtTaskName_TextChanged(object sender, EventArgs e)
        {
            triggerSaveButton();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            isSaved = true;
            button1.Enabled = false;
            timer1.Stop();
        }
    }
}
