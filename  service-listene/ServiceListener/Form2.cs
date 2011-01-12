using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ServiceListener
{
    public partial class Form2 : Form
    {
        private static object o = new object();

        public Form2()
        {
            InitializeComponent();
            timer1.Interval = 1000;
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            //判断路径、文件是否存在，如不存在则生成相关资源
            //string dir = AppDomain.CurrentDomain.BaseDirectory;
            //if (!Directory.Exists(dir))
            //{
            //    Directory.CreateDirectory(dir);
            //}

            ////设置文件名
            //string currtime = DateTime.Now.ToString("yyyyMM");
            //dir += currtime + ".log";

            string dir = @"F:\DeskTop\测试.log";

            if (!File.Exists(dir))
            {
                FileStream fs = File.Create(dir);
                fs.Close();
            }
            lock (o)
            {
                StreamWriter sw = new StreamWriter(dir, true);
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 写入日志");
                sw.Close();
            }
        }
    }
}
