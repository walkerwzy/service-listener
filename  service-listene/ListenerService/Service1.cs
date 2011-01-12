using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Threading;
using System.Collections;

namespace ListenerService
{
    public partial class ServiceListener : ServiceBase
    {
        private int intvalFlag;
        private bool queueAction;
        private Queue queue;
        private static int ActiveThreads;

        private static object o = new object();//读写文件锁

        public ServiceListener()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 守护服务启动");

            intvalFlag = 0;//初始化计时器标记
            ActiveThreads = 0;
            queue = Queue.Synchronized(new Queue());
            queueAction = false;//标识队列还未激活

            timer2.Interval = 60000;//以分钟为单位
            timer2.Enabled = true;
            timer2.Start();

            base.OnStart(args);
        }

        protected override void OnStop()
        {
            log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 守护服务停止");
            ActiveThreads = 0;
            queue.Clear();
            base.OnStop();
        }

        protected override void OnContinue()
        {
            timer2.Start();
            log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 守护服务恢复");
            base.OnContinue();
        }

        protected override void OnCustomCommand(int command)
        {
            base.OnCustomCommand(command);
        }

        protected override void OnPause()
        {
            timer2.Stop();//服务暂停的时候，定时器并没有停
            log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 守护服务暂停");
            base.OnPause();
        }

        protected override void OnSessionChange(System.ServiceProcess.SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }

        protected override void OnShutdown()
        {
            timer2.Close();
            queue.Clear();
            base.OnShutdown();
        }

        private static string getFiles()
        {
            //判断路径、文件是否存在，如不存在则生成相关资源
            string dir = AppDomain.CurrentDomain.BaseDirectory + @"log\";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            //设置文件名
            string currtime = DateTime.Now.ToString("yyyyMM");
            dir += currtime + ".log";
            if (!File.Exists(dir))
            {
                FileStream fs = File.Create(dir);
                fs.Close();
            }
            return dir;
        }

        //写日志
        private static void log(string logstr)
        {
            lock (o)
            {
                string dir = getFiles();
                StreamWriter sw = new StreamWriter(dir, true);
                sw.WriteLine(logstr);
                sw.Close();
            }
        }

        private void timer2_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //测试，写日志文件
            //string dir = getFiles_s();
            //StreamWriter sw = new StreamWriter(dir, true);
            //sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            //sw.Close();

            //配置文件设置的是最长每60分钟检查一次，本计时器每60分钟归零一次
            if (intvalFlag > 60)
            {
                intvalFlag = 1;
            }

            //读取配置文件，获得需要守护的程序列表（此服务中，相当于一分钟读一次，以保证能读到最新的配置文件）
            string inipath = AppDomain.CurrentDomain.BaseDirectory;//获取基目录，它由程序集冲突解决程序用来探测程序集。 
            inipath += "config.xml";
            XDocument xdoc = XDocument.Load(inipath);
            var q = xdoc.Descendants("Task");
            foreach (var item in q)
            {
                //任务信息
                string taskName = item.Attribute("Name").Value;
                string serviceName = item.Attribute("ServiceName").Value;
                string serviceArgs = item.Attribute("ServiceArgs").Value;
                string displayName = item.Attribute("ServiceDisplay").Value;
                bool enabled = item.Attribute("Enabled").Value.ToLower() == "true";
                int scanIntval = int.Parse(item.Descendants("Scan").SingleOrDefault().Attribute("Interval").Value);
                XElement reNode = item.Descendants("Restart").SingleOrDefault();
                string restartType = reNode.Attribute("Type").Value;
                string restartTime = reNode.Attribute("Time").Value;
                string restartDay = reNode.Attribute("Day").Value;

                //进行判断，扫描
                //设置为不扫描，或当前服务已经不存在，均不执行扫描
                if (!enabled || !ServiceIsExisted(serviceName))
                {
                    continue;
                }
                //扫描时间没到，但是满足重启条件，直接重启，同时不再扫描
                if (CanReboot(restartType, restartTime, restartDay))
                {
                    //推入重启队列
                    addRebootQueue(serviceName);
                }
                //正常扫描，此时排除扫描时间还没到的情况，之所以要在最后做判断，因为扫描时间到不到与重启无关，所以重启优先级要高
                if (scanIntval % intvalFlag == 0)
                {
                    ServiceController sv = new ServiceController(serviceName);
                    try
                    {
                        if (sv.Status != ServiceControllerStatus.Running && sv.Status != ServiceControllerStatus.StartPending)
                        {
                            log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + serviceName + " 服务开始启动");
                            if (sv.Status == ServiceControllerStatus.PausePending)//因为暂停的时候不能用start方法来启动服务
                            {
                                sv.WaitForStatus(ServiceControllerStatus.Paused, new TimeSpan(0, 30, 0));
                                sv.Continue();
                            }
                            else if (sv.Status == ServiceControllerStatus.Paused)
                            {
                                sv.Continue();
                            }
                            else
                            {
                                sv.Start();//启动时间相对重启要短，而且不像重启一样定时计划，不再用队列
                                sv.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 30));//最多给40秒时间等待服务启动成功
                            }
                            log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + serviceName + " 服务启动成功");
                        }
                        else//过滤假死状态的服务
                        {
                            if (sv.Status != ServiceControllerStatus.StartPending)//等等正在启动中的服务30秒
                            {
                                sv.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 30));
                            }
                            if (!new ServiceHandler.ServiceExcptionHandler().ExceptionCheck(serviceName, serviceArgs))//确认有异常
                            {
                                //不适用直接启动程序，丢入重启队列
                                log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + serviceName + " 服务异常，开始启动");
                                addRebootQueue(serviceName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + serviceName + " 服务启动发生异常：" + ex.Message);
                    }
                }
            }
            intvalFlag++;
        }

        /// <summary>
        /// 将服务推入重启队列
        /// </summary>
        /// <param name="serviceName">服务名</param>
        private void addRebootQueue(string serviceName)
        {
            if (!queue.Contains(serviceName))
            {
                queue.Enqueue(serviceName);
            }
            if (!queueAction)//发现队列没有在执行，则激活
            {
                Thread newThread = new Thread(new ThreadStart(playQueue));
                newThread.Start();
            }
        }


        //判断window服务是否存在：
        private bool ServiceIsExisted(string serviceName)
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

        //判断当前是否应该重启
        private bool CanReboot(string type, string time, string day)
        {
            switch (type.ToLower())
            {
                case "week":
                    string[] s = day.Split(',');
                    int thisdayint = (int)DateTime.Now.DayOfWeek;
                    string thisday = thisdayint == 0 ? "7" : thisdayint.ToString();
                    if (s.Contains(thisday))//符合星期要求
                    {
                        string thistime = DateTime.Now.ToString("HH:mm");
                        if (string.Equals(thistime, time))//符合时间要求
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                case "month":
                    int today = DateTime.Now.Day;
                    if (int.Parse(day) == today)//符合当天要求
                    {
                        string thistime2 = DateTime.Now.ToString("HH:mm");
                        if (string.Equals(thistime2, time))//符合时间要求
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                default://每天
                    string thistime3 = DateTime.Now.ToString("HH:mm");
                    if (string.Equals(thistime3, time))//符合时间要求
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
            }
        }

        //检查队列
        private void playQueue()
        {
            while (queue.Count > 0)
            {
                queueAction = true;//标识队列在执行中
                if (ActiveThreads > 10)
                {
                    Thread.Sleep(10000);//线程达到上限，每10秒查一次
                    continue;
                }
                string serviceName = queue.Dequeue() as string;
                ThreadService ts = new ThreadService(serviceName);
                //为每个处理服务的操作开一个线程，避免重启时间过长。但限制为最多10个线程
                Thread thread = new Thread(new ThreadStart(ts.RestartServer));
                thread.Start();
                lock (thread)
                {
                    ActiveThreads++;
                }
            }
            queueAction = false;//标识为没有任务在执行
        }


        //线程调用的传参用法
        class ThreadService
        {
            public ServiceController sv { get; set; }
            public ThreadService(string serviceName)
            {
                sv = new ServiceController(serviceName);
            }

            public void RestartServer()
            {
                log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + sv.ServiceName + " 服务开始重启");
                Thread curThread = Thread.CurrentThread;
                try
                {
                    if (sv.Status != ServiceControllerStatus.Stopped && sv.Status != ServiceControllerStatus.StopPending)
                    {
                        sv.Stop();
                    }
                    sv.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 1, 0));
                    sv.Start();
                    sv.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 1, 0));
                    log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + sv.ServiceName + " 服务重启成功");
                }
                catch (Exception ex)
                {
                    log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + sv.ServiceName + " 服务重启发生异常：" + ex.Message);
                }
                finally
                {
                    lock (curThread)
                    {
                        ActiveThreads--;
                    }
                }
            }

        }

        //private void timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        //{
        //    //判断路径、文件是否存在，如不存在则生成相关资源
        //    string dir = AppDomain.CurrentDomain.BaseDirectory;
        //    if (!Directory.Exists(dir))
        //    {
        //        Directory.CreateDirectory(dir);
        //    }

        //    //设置文件名
        //    string currtime = DateTime.Now.ToString("yyyyMM");
        //    dir += currtime + ".log";
        //    if (!File.Exists(dir))
        //    {
        //        FileStream fs = File.Create(dir);
        //        fs.Close();
        //    }
        //    lock (o)
        //    {
        //        StreamWriter sw = new StreamWriter(dir, true);
        //        sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " 写入日志");
        //        sw.Close();
        //    }
        //}
    }

}
