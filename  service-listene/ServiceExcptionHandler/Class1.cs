using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;

namespace ServiceHandler
{
    public class ServiceExcptionHandler
    {
        public ServiceExcptionHandler()
        {

        }

        /// <summary>
        /// 检查服务异常，仅检查服务状态为running的程序
        /// </summary>
        /// <param name="ServiceName">服务名</param>
        /// <param name="args">检测参数</param>
        /// <returns>返回真代表服务正常，否则返回假</returns>
        public bool ExceptionCheck(string ServiceName, string args)
        {
            switch (ServiceName.ToLower())
            {
                default:
                    return true;
            }
        }
    }
}
