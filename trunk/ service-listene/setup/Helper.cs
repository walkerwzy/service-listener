using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ServiceProcess;

namespace setup
{
    public class Helper
    {
        public Helper()
        {

        }


        public static void ddlBindData(ComboBox ddl, object datasource, string dispaly, string value, bool addEmpty)
        {
            ddl.DataSource = datasource;
            ddl.DisplayMember = dispaly;
            ddl.ValueMember = value;
        }
        
        //判断window服务是否存在：
        private static bool ServiceIsExisted(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();
            foreach (ServiceController s in services)
            {
                if (s.ServiceName == serviceName)
                {
                    return true;
                }
            }
            return false;
        }

        //检查服务运行状态
        public static string ServiceStatus(String serviceName)
        {
            if (ServiceIsExisted(serviceName))
            {
                ServiceController sc = new ServiceController(serviceName);
                return sc.Status.ToString();
            }
            else
            {
                return "服务不存在";
            }
        }

    }

    public class MyItem
    {
        public string name { get; set; }
        public string value { get; set; }
        public MyItem(string name, string value)
        {
            this.name = name;
            this.value = value;
        }
        public override string ToString()
        {
            return this.name;
        }
    }
}
