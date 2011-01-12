using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ServiceWrapper
{
    public partial class wrapper : Form
    {
        public wrapper()
        {
            InitializeComponent();
            cmbxType.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            openFileDialog1.Filter = "All files (*.*)|*.*|excutive files (*.exe,*.cmd,*.bat)|*.exe;*.cmd;*.bat";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Title = "选择需要处理的应用程序";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFileName.Text = openFileDialog1.FileName;
                btnStart.Enabled = true;
            }
            else
            {
                txtFileName.Text = "";
                btnStart.Enabled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!File.Exists(txtFileName.Text.Trim()))
            {
                MessageBox.Show("文件不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(txtServiceName.Text.Trim()))
            {
                MessageBox.Show("服务名不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {

                StringBuilder v = new StringBuilder();
                v.AppendLine("@echo off");
                v.AppendLine("::设置服务名称");
                v.AppendLine("set service_name=" + txtServiceName.Text.Trim());
                v.AppendLine("::设置服务描述");
                v.AppendLine("set service_description=" + txtDescript.Text.Trim());
                v.AppendLine("::设置服务程序路径");
                v.AppendLine("set prog_path=" + openFileDialog1.FileName);
                v.AppendLine("::设置服务的启动方式 auto:自动 demand:手动 disabled:禁用");
                v.AppendLine("set strt=" + getServiceType());
                v.AppendLine("::设置资源文件路径");
                v.AppendLine("set srcpath=" + AppDomain.CurrentDomain.BaseDirectory+"src");
                v.AppendLine();

                string s = v.ToString();

                //读取操作模板
                StreamReader sr = new StreamReader(getFile(@"src\\","core.txt"), Encoding.Default);
                s += sr.ReadToEnd();
                sr.Close();

                //写到文件，当前目录
                string desPath = getFile(@"User\\"+txtServiceName.Text.Trim(), @"\\install.bat");
                StreamWriter sw = new StreamWriter(desPath, false, Encoding.Default);
                sw.Write(s);
                sw.Close();

                //生成卸载文件
                v = new StringBuilder();
                v.AppendLine("@echo off");
                v.AppendLine("set service_name=" + txtServiceName.Text.Trim());
                v.AppendLine("set reg_file=UninstallService.reg");
                v.AppendLine("echo 开始卸载;");
                v.AppendLine("sc stop %service_name%");
                v.AppendLine("sc delete %service_name%");
                v.AppendLine("echo 完成");
                v.AppendLine("pause");
                sw = new StreamWriter(getFile(@"User\\" + txtServiceName.Text.Trim(), @"\\uninstall.bat"), false, Encoding.Default);
                sw.Write(v.ToString());
                sw.Close();

                //执行安装文件
                System.Diagnostics.Process.Start(desPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void button2_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private string getFile(string filename)
        {
            string p = AppDomain.CurrentDomain.BaseDirectory + filename;
            if (!File.Exists(p))
            {
                FileStream fs = File.Create(p);
                fs.Close();
            }
            return p;
        }
        /// <summary>
        /// 根据路径和文件名判断路径、文件是否存在，然后再返回完整文件名
        /// </summary>
        /// <param name="subDir">从应该程序当前路径开始的所有路径</param>
        /// <param name="filename">文件名和扩展名</param>
        /// <returns>文件名</returns>
        private string getFile(string subDir, string filename)
        {
            string s = AppDomain.CurrentDomain.BaseDirectory + subDir;
            if (!Directory.Exists(s))
            {
                DirectoryInfo di = Directory.CreateDirectory(s);
            }
            string p = s + filename;
            if (!File.Exists(p))
            {
                FileStream fs = File.Create(p);
                fs.Close();
            }
            return p;
        }

        private string getServiceType()
        {
            switch (cmbxType.SelectedIndex)
            {
                case 1:
                    return "demand";
                case 2:
                    return "disabled";
                default:
                    return "auto";
            }
        }

        private void txtFileName_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFileName.Text.Trim()))
            {
                btnStart.Enabled = false;
                return;
            }
            //处理路径格式
            string fixpath = txtFileName.Text.Trim();
            fixpath = fixpath.Replace(@"\", @"\\");
            openFileDialog1.FileName = fixpath;
            btnStart.Enabled = true;
        }

        private void txtServiceName_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtServiceName.Text.Trim()))
            {
                btnStart.Enabled = false;
                return;
            }
            btnStart.Enabled = true;
            txtDescript.Text = txtServiceName.Text;
        }

        private void txtDescript_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtDescript.Text.Trim()))
            {
                btnStart.Enabled = false;
                return;
            }
            btnStart.Enabled = true;
        }
    }
}
