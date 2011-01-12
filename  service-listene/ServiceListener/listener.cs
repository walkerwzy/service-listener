using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.Xml.Linq;

namespace ServiceListener
{
    public partial class listener : Form
    {
        private string inipath = ConfigurationSettings.AppSettings["inipath"];

        public listener()
        {
            InitializeComponent();
            initData(null);
        }

        public void initData(int? selectedindex)
        {
            try
            {
                XDocument xdoc = LoadTaskList();
                if (!selectedindex.HasValue)
                {
                    selectedindex = 0;
                }
                combxTaskList.SelectedIndex = selectedindex.Value;

                //读出第一个任务的属性
                XElement task = xdoc.Descendants("Task").Where(t => t.Attribute("Name").Value == combxTaskList.SelectedItem.ToString()).SingleOrDefault();
                lblDisplayName.Text = task.Attribute("ServiceDisplay").Value;
                lblServiceName.Text = task.Attribute("ServiceName").Value;
                lblInterval.Text = "每" + task.Descendants("Scan").SingleOrDefault().Attribute("Interval").Value + "分钟";

                //检查服务运行状态
                lblStatus.Text = Helper.ServiceStatus(task.Attribute("ServiceName").Value);

                //拼任务计划描述
                XElement xe = task.Descendants("Restart").SingleOrDefault();
                string daystr = xe.Attribute("Day").Value;
                string typestr = xe.Attribute("Type").Value;
                string timestr = xe.Attribute("Time").Value;
                lblSchedule.Text = getSchedultStr(daystr, timestr, typestr);
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失败！\n" + ex.Message);
                return;
            }
        }

        //关闭
        private void button3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        //新增
        private void button1_Click(object sender, EventArgs e)
        {
            Edit ef = new Edit();
            if (ef.ShowDialog() == DialogResult.OK)
            {
                LoadTaskList();
            }
        }

        //编辑
        private void button4_Click(object sender, EventArgs e)
        {
            //Edit ef = new Edit(combxTaskList.SelectedItem.ToString());
            Edit ef = new Edit();
            if (ef.ShowDialog() == DialogResult.OK)
            {
                initData(combxTaskList.SelectedIndex);
            }
        }

        //删除
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                XDocument xdoc = XDocument.Load(inipath);
                XElement xnode = xdoc.Descendants("Task").Where(x => x.Attribute("Name").Value == combxTaskList.SelectedItem.ToString()).SingleOrDefault();
                xnode.Remove();
                xdoc.Save(inipath);
                initData(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失败！\n" + ex.Message);
                return;
            }
        }

        private void combxTaskList_SelectedIndexChanged(object sender, EventArgs e)
        {
            initData(combxTaskList.SelectedIndex);
        }

        //加载任务列表
        private XDocument LoadTaskList()
        {
            try
            {
                XDocument xdoc = XDocument.Load(inipath);
                var q = xdoc.Descendants("Task");
                foreach (var item in q)
                {
                    string v = item.Attribute("Name").Value;
                    if (!combxTaskList.Items.Contains(v))
                    {
                        combxTaskList.Items.Add(v);   
                    }
                }
                return xdoc;
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作失败！\n" + ex.Message);
                return null;
            }
        }

        //拼计划描述
        private string getSchedultStr(string day, string time, string type)
        {
            string str = "";
            switch (type.ToLower())
            {
                case "week":
                    str = "每周";
                    string[] ws = day.Split(','); 
                    for (int i = 0; i < ws.Length; i++)
                    {
                        str += ((weekstr)(int.Parse(ws[i]))).ToString() + "、";
                    }
                    str = str.TrimEnd('、');
                    break;
                case "month":
                    str = "每月";
                    str += day + "号";
                    break;
                case "day":
                    str = "每天";
                    break;
                default:
                    break;
            }
            str += time + "重启服务";
            return str;
        }

        enum weekstr
        {
            星期一=1,星期二,星期三,星期四,星期五,星期六,星期天
        }

    }
}
