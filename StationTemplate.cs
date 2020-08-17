using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AutoFrameDll;
using CommonTool;


namespace AutoFrame
{
    class StationTemplate : StationEx
    {  
        /// <summary>
        /// 构造函数，需要设置站位当前的IO输入，IO输出，轴方向及轴名称，以显示在手动页面方便操作
        /// </summary>
        /// <param name="strName"></param>
        public StationTemplate(string strName) : base(strName)
        {

        }
        /// <summary>
        /// 站位初始化，用来添加伺服上电，打开网口，站位回原点等动作
        /// </summary>
        public override void StationInit()
        {
        }
        /// <summary>
        /// 站位退出退程时调用，用来关闭伺服，关闭网口等动作
        /// </summary>
        public override void StationDeinit()
        {
        }

         //当所有站位均为全自动运行模式时，不需要重载该函数
        //当所有站位为半自动运行模式时，也不需要重载该函数， 只需要在站位流程开始时插入WaitBegin()即可保证所有站位同步开始。
        //当所有站位中，有的为半自动，有的为全自动时，半自动的站位不重载该函数，使用WaitBegin()控制同步，全自动的站位重载该函数返回true即可。
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //public override bool IsReady()
        //{
        //    return true;
        //}


        /// <summary>
        /// 初始化时设置站位安全状态，用来设备初始化时各站位之间相互卡站，
        /// 控制各站位先后动作顺序用，比如流水线组装，肯定需要组装的Z轴升
        /// 起后，流水线才能动作这种情况
        /// </summary>
        public override void InitSecurityState()
        {
        }

        /// <summary>
        /// 站位急停时调用，如果站位急停时只需要停轴，不需要重载
        /// 如果需要关闭流水线，停止机器人等IO操作，则必须重载此
        /// 函数响应急停的处理
        /// </summary>
        public override void EmgStop()
        {


        }

        /// <summary>
        /// 响应流程暂停的处理，比如流水线在暂停时需要停止,如果不需要可以不用重载
        /// </summary>
        public override void OnPause()
        {

        }
        /// <summary>
        /// 响应流程恢复的处理，比如流水线在恢复时需要继续运行，如果不需要可以不用重载
        /// </summary>
        public override void OnResume()
        {
        }


        /// <summary>
        /// 站位流程函数，程序框架默认一个循环调用一次该函数
        /// </summary>
        public override void StationProcess()
        {
           WaitBegin();
        }
    }
}
