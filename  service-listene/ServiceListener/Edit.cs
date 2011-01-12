using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Xml.Linq;
using System.Configuration;

namespace ServiceListener
{

    public partial class Edit : Form
    {
        private List<string> weeks = new List<string>();
        private List<string> tasks = new List<string>();
        private string inipath = "";//ConfigurationSettings.AppSettings["inipath"];
        private XDocument xdoc = null;
        private bool flag = true;//标志第一次进入

        public Edit()
        {
            InitializeComponent();

            inipath = AppDomain.CurrentDomain.BaseDirectory;//获取基目录，它由程序集冲突解决程序用来探测程序集。 
            inipath += "config.xml";
            xdoc = XDocument.Load(inipath);
            //任务列表
            var q = xdoc.Descendants("Task");
            foreach (var item in q)
            {
                string v = item.Attribute("Name").Value;
                if (!tasks.Contains(v))
                {
                    tasks.Add(v);
                }
            }
            //服务
            ServiceController[] services = ServiceController.GetServices();
            List<MyItem> mi = new List<MyItem>();
            //mi.Add(new MyItem("请选择服务...", "-1"));
            foreach (ServiceController item in services)
            {
                mi.Add(new MyItem(item.DisplayName, item.ServiceName));
            }
            Helper.ddlBindData(combxService, mi, "name", "value", true);
            initData(0);
        }

        private void initData(int? sindex)
        {
            if (!sindex.HasValue)
            {
                sindex = 0;
            }
            listBox1.DataSource = null;
            listBox1.DataSource = tasks;
            if (tasks.Count > 0)
            {
                listBox1.SelectedIndex = sindex.Value;
            }
            else//当配置文件里一个任务也没有时，人为新增一个
            {
                button3_Click(null, null);
            }
            flag = false;
            //初始化放在列表切换事件做

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblServiceName.Text = combxService.SelectedValue.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
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
        }

        //保存
        private void button1_Click(object sender, EventArgs e)
        {
            //if (string.IsNullOrEmpty(txtTaskName.Text.Trim()))
            //{
            //    MessageBox.Show("请填写任务名称");
            //    return;
            //}
            //if (combxService.SelectedIndex == 0)
            //{
            //    MessageBox.Show("请选择任务需要检测的系统服务");
            //    return;
            //}

            try
            {
                SaveCurrent();
                xdoc.Save(inipath);
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

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //切换菜单之前保存当前所有状态到内存...
            //移除旧节点
            if ((sender == null && e == null) || (tasks.Count == 0 || (listBox1.DataSource == null)))
            {
                return;
            }
            if (!flag)//几种情况下不执行保存当前状态操作，就人为传入flag值来决定
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
            numericUpDown1.Value = (decimal)10;
            numericUpDown2.Value = (decimal)1;
            checkBox1.Checked = false; checkBox2.Checked = true; checkBox3.Checked = false; checkBox4.Checked = false;
            checkBox5.Checked = false; checkBox6.Checked = false; checkBox7.Checked = false; checkBox8.Checked = true; checkBox9.Checked = true;
            combxType.SelectedIndex = 0;
            dateTimePicker1.Value = Convert.ToDateTime("2010-10-10 04:00:00");
            if (!weeks.Contains("2"))
            {
                weeks.Add("2");
            }
            txtTaskName.Text = listBox1.SelectedItem.ToString();
            txtTaskName.Tag = listBox1.SelectedItem.ToString();//保存进入的时候的服务名，退出的时候用，避免使用过程中文本框的值被改

            //默认为全激活状态
            checkBox9.Checked = true;
            checkBox8.Checked = true;

            if (tasks.Count == 0 || (listBox1.DataSource == null))
            {
                return;//所有任务删除的情况下，就不执行了。
            }
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
                checkBox9.Checked = svNode.Attribute("Enabled").Value.ToLower() == "true";

            }
            catch (Exception ex)
            {
                MessageBox.Show("读取失败\n" + ex.Message);
            }
        }

        private void SaveCurrent()
        {

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

            XElement newTask = new XElement("Task",
                new XAttribute("Name", string.IsNullOrEmpty(txtTaskName.Text.Trim()) ? getNewTaskName() : txtTaskName.Text.Trim()),
                new XAttribute("ServiceName", lblServiceName.Text.Trim()),
                new XAttribute("ServiceDisplay", combxService.SelectedItem.ToString()),
                new XAttribute("Enabled", checkBox9.Checked.ToString()),
                new XElement("Scan", new XAttribute("Interval", ((int)numericUpDown1.Value).ToString())),
                new XElement("Restart", new XAttribute("Type", tasktype),
                    new XAttribute("Time", dateTimePicker1.Value.ToString("HH:mm")),
                    new XAttribute("Day", day)));
            xdoc.Root.Add(newTask);
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
                new XAttribute("ServiceDisplay", combxService.SelectedItem.ToString()),
                new XAttribute("Enabled", "True"),
                new XElement("Scan", new XAttribute("Interval", 10),
                new XElement("Restart", new XAttribute("Type", "Week"),
                    new XAttribute("Time", "04:00"),
                    new XAttribute("Day", 2))));
            xdoc.Root.Add(newTask);

            initData(listBox1.Items.Count);
            txtTaskName.Text = newname;
            txtTaskName.Tag = newname;
            listBox1.SelectedItem = newname;
        }

        //任务名是否存在
        private bool isTaskNameExist(string name)
        {
            return xdoc.Descendants("Task").Where(t => t.Attribute("Name").Value == name).Count() > 0;
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
            flag = true;//删除任务时，也不执行数据推入内存操作，因为没必要保存数据了
            initData(i);
        }

    }
}
