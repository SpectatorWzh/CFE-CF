using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFrameDll;
using CommonTool;
using Communicate;

namespace AutoFrame
{
    class StationUnloadFerriteTry : StationBase
    {
        enum POINT
        {
            安全点 = 1,
            收料等待点,
            收料满极限点,
        }
        private bool m_bReady = false;
        private long nZspeed = 300000;
        private long nUspeed = 200000;
        int nIndex = 1;
        int nDryRun = 1;
        public StationUnloadFerriteTry(string strName) : base(strName)
        {
                io_in = new string[] { "3.1", "3.2", "3.3", "3.4", "3.5", "3.6", "3.7", "3.8", "3.9", "3.10", "3.11", "3.12", "3.13", "3.14", "3.15" };
                io_out = new string[] { "3.5", "3.6", "3.7", "3.8", "3.9", "3.10", "3.11", "3.12" };
        }

        public override void InitSecurityState()
        {
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_下料Try通知供料Try_InitOk, false, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_料盘空_Empy, false, false);
            //SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_下料Tray盘站通知Ferrite机器人取料_Ok), false, false);
        }

        
        public override void StationInit()
        {
            TryInit();
        }
        public override void StationProcess()
        {
            while (true)
            {
                CheckContinue(false);
                if (StationMgr.GetInstance().BAutoMode)
                    break;
                if (IoMgr.GetInstance().ReadIoInBit("启动"))
                    break;
                WaitTimeDelay(50);
            }
                TryProcess();
            //if (1 == SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_Calib_Index))//
            //{
            //    CalibProcess();
            //}
            //else
            //{
               
            //}
        }

        public void TryProcess()
        {
           // WaitRegBit((int)SysBitReg.Bit_供料Try通知下料Try_ReadOk, true, -1);//等待供料站StationFeedFerriteTry站上料完成，由此站通知机器人Ferrite料准备完成，进行取料
          //  SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_下料Tray盘站通知Ferrite机器人取料_Ok, true, false);//Bit_下料Tray盘站通知Ferrite机器人取料_Ok,通知机器人Ferrite料准备完成，进行取料
            //if (StationMgr.GetInstance().GetStation("Ferrite上下料站").StationEnable)//Ferrite上下料站是否禁用
            //{
            //    WaitRegBit((int)SysBitReg.Bit_料盘空_Empy, true, -1);
            //    SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_料盘空_Empy, false, false);
            //}
            //else
            //{
            //    WaitTimeDelay(5000);
            //}

            WaitRegBit((int)SysBitReg.Bit_料盘空_Empy, true, -1);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_料盘空_Empy, false, false);

            while (true)
            {
                CheckContinue(false);
                if (false==IoMgr.GetInstance().GetIoInState("收空料盘预到位检测"))
                {
                    break;
                }
                long nCurrPos = (long)(1000 * SystemMgr.GetInstance().GetParamDouble("CallBackTryStep")) + MotionMgr.GetInstance().GetAixsPos(AxisZ);
                MotionMgr.GetInstance().AbsMove(AxisZ, (int)nCurrPos, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("UnloadAxisZSpeed")) / 100.0));
                WaitMotion(AxisZ, -1);
            }
            搬运气缸去供料站();
            WaitIo("Tray搬运气缸回到供料盘", true, 0);
            搬运气缸下();
            WaitIo("搬运气缸下到位", true, 0);
            搬运真空吸();
            WaitIo("搬运真空吸", true, 0);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_收料Tray盘站通知供料Tray盘站气缸松开, true, false);
            WaitRegBit((int)SysBitReg.Bit_收料Tray盘站通知供料Tray盘站气缸松开, false);
            搬运气缸上();
            WaitIo("搬运气缸上到位", true, 0);
            搬运气缸回收料站();
            WaitIo("Tray搬运气缸到达下料盘", true, 0);
            WaitIo("Tray搬运气缸到达下料盘", true, -1);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_收料Tray盘站通知供料Try盘站上料, true, false);
            int RelatDistance = (int)(1000 * SystemMgr.GetInstance().GetParamDouble("StepFixedUnload"));
            //MotionMgr.GetInstance().RelativeMove(AxisZ, RelatDistance, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("UnloadAxisZSpeed")) / 100.0));
            //WaitMotion(AxisZ, -1);
            搬运气缸下();
            WaitIo("搬运气缸下到位", true, 0);
            搬运真空破();
            搬运气缸上();
            WaitIo("搬运气缸上到位", true, 0);
            WaitTimeDelay(200);
            MotionMgr.GetInstance().RelativeMove(AxisZ, -RelatDistance, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("UnloadAxisZSpeed")) / 100.0));
            WaitMotion(AxisZ, -1);



            while (true)
            {
                CheckContinue(false);
                if (IsFullTry())
                {
                    CallBackTry();
                    break;
                }
                if(false==IoMgr.GetInstance().ReadIoInBit("收空料盘预到位检测"))
                {
                    break;
                }
                long nCurrPos = (long)(1000 * SystemMgr.GetInstance().GetParamDouble("CallBackTryStep")) + MotionMgr.GetInstance().GetAixsPos(AxisZ);
                MotionMgr.GetInstance().AbsMove(AxisZ, (int)nCurrPos, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("UnloadAxisZSpeed")) / 100.0));
                WaitMotion(AxisZ, -1);
            }
        }

        public void CallBackTry()
        {
            MotionMgr.GetInstance().AbsMove(AxisZ, m_dicPoint[(int)POINT.安全点].z, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("UnloadAxisZSpeed"))/100.0));
            WaitMotion(AxisZ, -1);
            收料抽屉锁松开();
            WaitIo("收料抽屉锁紧气缸锁松开", true,0);
            WaitIo("收料抽屉到位检测", false,-1);
            WaitIo("收空料盘有无料检测", false, -1);
            WaitTimeDelay(500);
            //WaitIo("收空料盘有无料检测", true, -1);
            WaitIo("收料抽屉到位检测", true, -1);
            收料抽屉锁紧();
            WaitIo("收料抽屉锁紧气缸锁锁紧", true, 0);
            ShareInfoSpace.SetUnloadTryState(true);
            while (true)
            {
                CheckContinue(false);
                if(false==IoMgr.GetInstance().GetIoInState("收空料盘预到位检测"))
                {
                    break;
                }
                long nCurrPos = (long)(1000 * SystemMgr.GetInstance().GetParamDouble("CallBackTryStep")) + MotionMgr.GetInstance().GetAixsPos(AxisZ);
                MotionMgr.GetInstance().AbsMove(AxisZ, (int)nCurrPos, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("UnloadAxisZSpeed")) / 100.0));
                WaitMotion(AxisZ, -1);
            }
        }

        /// <summary>
        /// 判断料盘是否满
        /// 满：返回true
        /// 不满：返回false
        /// </summary>
        /// <returns></returns>
        public bool IsFullTry()
        {
            long nCurrPos = MotionMgr.GetInstance().GetAixsPos(AxisZ);
            if (false==IoMgr.GetInstance().ReadIoInBit("收空料盘预到位检测") && nCurrPos < m_dicPoint[(int)(POINT.收料满极限点)].z)
            {
                ShareInfoSpace.SetUnloadTryState(false);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 料盘模块初始化
        /// </summary>
        public void TryInit()
        {
            //IoMgr.GetInstance().WriteIoBit("Tray搬运气缸向收料站伸出", true);
            //WaitIo("Tray搬运气缸到达下料盘", true);
            //IoMgr.GetInstance().WriteIoBit("料盘搬运吸真空", false);
            //WaitTimeDelay(50);
            //IoMgr.GetInstance().WriteIoBit("料盘搬运破真空", false);

            搬运气缸上();
            WaitIo("搬运气缸上到位", true,0);
            搬运气缸回收料站();
            WaitIo("Tray搬运气缸到达下料盘",true, 0);
            搬运真空破();
            WaitTimeDelay(200);
            MotionMgr.GetInstance().ServoOn(AxisZ);
            WaitTimeDelay(400);
            MotionMgr.GetInstance().Home(AxisZ, 1);
            WaitHome(AxisZ, -1);
            收料抽屉锁松开();
            WaitIo("收料抽屉锁紧气缸锁松开", true, 0);
            MotionMgr.GetInstance().AbsMove(AxisZ, m_dicPoint[(int)POINT.安全点].z, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("UnloadAxisZSpeed"))/100.0));
            WaitMotion(AxisZ, -1);
            WaitIo("收料抽屉到位检测", false, -1);
            WaitIo("收空料盘有无料检测", false, -1);
            //WaitIo("收空料盘有无料检测", true, -1);
            WaitIo("收料抽屉到位检测", true, -1);
            收料抽屉锁紧();
            WaitIo("收料抽屉锁紧气缸锁锁紧", true, 0);
            ShareInfoSpace.SetUnloadTryState(true);
            //MotionMgr.GetInstance().AbsMove(AxisZ, m_dicPoint[(int)POINT.收料等待点].z, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("UnloadAxisZSpeed"))/100.0));
            //WaitMotion(AxisZ, -1);
            while (true)
            {
                CheckContinue(false);
                if (false==IoMgr.GetInstance().GetIoInState("收空料盘预到位检测"))
                {
                    break;
                }
                long nCurrPos = (long)(1000 * SystemMgr.GetInstance().GetParamDouble("CallBackTryStep")) + MotionMgr.GetInstance().GetAixsPos(AxisZ);
                MotionMgr.GetInstance().AbsMove(AxisZ, (int)nCurrPos, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("UnloadAxisZSpeed")) / 100.0));
                WaitMotion(AxisZ, -1);
            }
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_下料Try通知供料Try_InitOk, true, false);
            ShowLog("料盘Try盘站初始化完成");
        }

        public override void StationDeinit()
        {
            
        }
        /// <summary>
        /// 夹紧气缸松开
        /// </summary>
        public void Try加紧气缸松开()
        {
            IoMgr.GetInstance().WriteIoBit("Tray夹紧气缸紧", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("Tray夹紧气缸松", true);
        }


        /// <summary>
        /// 夹紧气缸加紧
        /// </summary>
        public void ClyFixedLock()
        {
            IoMgr.GetInstance().WriteIoBit("Tray夹紧气缸松", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("Tray夹紧气缸紧", true);
        }


        /// <summary>
        /// 下料抽屉锁紧气缸打开
        /// </summary>
        public void 收料抽屉锁松开()
        {
            IoMgr.GetInstance().WriteIoBit("收料抽屉锁紧气缸紧", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("收料抽屉锁紧气缸松", true);
        }

        /// <summary>
        /// 下料抽屉锁紧气缸上锁
        /// </summary>
        public void 收料抽屉锁紧()
        {
            IoMgr.GetInstance().WriteIoBit("收料抽屉锁紧气缸松", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("收料抽屉锁紧气缸紧", true);
        }

        /// 料台升降气缸升
        /// </summary>
        public void 料台升降气缸升()
        {
            IoMgr.GetInstance().WriteIoBit("料台升降气缸降", false);
            IoMgr.GetInstance().WriteIoBit("料台升降气缸升", true);
        }

        /// <summary>
        /// 料台升降气缸降
        /// </summary>
        public void 料台升降气缸降()
        {
            IoMgr.GetInstance().WriteIoBit("料台升降气缸升", false);
            IoMgr.GetInstance().WriteIoBit("料台升降气缸降", true);
        }

        /// <summary>
        /// 料台前进气缸伸出
        /// </summary>
        public void 料台前进气缸伸出()
        {
            IoMgr.GetInstance().WriteIoBit("料台前进气缸缩回", false);
            IoMgr.GetInstance().WriteIoBit("料台前进气缸伸出", true);
        }

        /// <summary>
        /// 料台前进气缸缩回
        /// </summary>
        public void 料台前进气缸缩回()
        {
            IoMgr.GetInstance().WriteIoBit("料台前进气缸伸出", false);
            IoMgr.GetInstance().WriteIoBit("料台前进气缸缩回", true);
        }

        public void 剥料轮压紧气缸压紧()
        {
            IoMgr.GetInstance().WriteIoBit("剥料轮压紧气缸松开", false);
            IoMgr.GetInstance().WriteIoBit("剥料轮压紧气缸压紧", true);
        }

        public void 剥料轮压紧气缸松开()
        {
            IoMgr.GetInstance().WriteIoBit("剥料轮压紧气缸压紧", false);
            IoMgr.GetInstance().WriteIoBit("剥料轮压紧气缸松开", true);
        }

        public void 料台真空吸()
        {
            IoMgr.GetInstance().WriteIoBit("料台真空破", false);
            IoMgr.GetInstance().WriteIoBit("料台真空吸", true);
        }

        public void 料台真空破()
        {
            IoMgr.GetInstance().WriteIoBit("料台真空吸", false);
            IoMgr.GetInstance().WriteIoBit("料台真空破", true);
        }

        public void 剥刀真空吸()
        {
            IoMgr.GetInstance().WriteIoBit("剥刀真空破", false);
            IoMgr.GetInstance().WriteIoBit("剥刀真空吸", true);
        }

        public void 剥刀真空破()
        {
            IoMgr.GetInstance().WriteIoBit("剥刀真空吸", false);
            IoMgr.GetInstance().WriteIoBit("剥刀真空破", true);
        }

        public void 卷料真空吸()
        {
            IoMgr.GetInstance().WriteIoBit("卷料真空破", false);
            IoMgr.GetInstance().WriteIoBit("卷料真空吸", true);
        }

        public void 卷料真空破()
        {
            IoMgr.GetInstance().WriteIoBit("卷料真空吸", false);
            IoMgr.GetInstance().WriteIoBit("卷料真空破", true);
        }

        public void CalibProcess()
        {
            if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_T1_Apply))
            {
                goto lable_Calib_Over;
            }
            while(true)
            {
                CheckContinue(false);

                WaitRegBit((int)SysBitReg.Bit_T1_标定通信_Pross, true, -1);
                if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_T1))
                {
                    WarningMgr.GetInstance().Warning("T1_标定");
                    goto lable_Calib_Over;
                }
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_T1_标定通信_Pross, false, false);
                int nCount = SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_T1_标定_Count);
                if (11== nCount)
                {
                    break;
                }
            }
        lable_Calib_Over:
            ;
        }
       public void 搬运气缸去供料站()
        {
            WaitIo("搬运气缸下到位", false, -1);
            WaitIo("搬运气缸上到位", true, -1);
            IoMgr.GetInstance().WriteIoBit("Tray搬运气缸向收料站伸出", false);
            WaitTimeDelay(20);
            IoMgr.GetInstance().WriteIoBit("Tray搬运气缸向供料站返回", true);
        }
        public void 搬运气缸回收料站()
        {
            WaitIo("搬运气缸下到位", false, -1);
            WaitIo("搬运气缸上到位", true, -1);
            IoMgr.GetInstance().WriteIoBit("Tray搬运气缸向供料站返回", false);
            WaitTimeDelay(20);
            IoMgr.GetInstance().WriteIoBit("Tray搬运气缸向收料站伸出", true);
        }
        public void 搬运气缸下()
        {
            IoMgr.GetInstance().WriteIoBit("搬运气缸上", false);
            WaitTimeDelay(20);
            IoMgr.GetInstance().WriteIoBit("搬运气缸下", true);
        }
        public void 搬运气缸上()
        {
            IoMgr.GetInstance().WriteIoBit("搬运气缸下", false);
            WaitTimeDelay(20);
            IoMgr.GetInstance().WriteIoBit("搬运气缸上", true);
        }
        public void 搬运真空吸()
        {
            IoMgr.GetInstance().WriteIoBit("搬运真空破", false);
            WaitTimeDelay(20);
            IoMgr.GetInstance().WriteIoBit("搬运真空吸",true);
        }
        public void 搬运真空破()
        {
            IoMgr.GetInstance().WriteIoBit("搬运真空吸", false);
            WaitTimeDelay(20);
            IoMgr.GetInstance().WriteIoBit("搬运真空破", true);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("搬运真空破", false);
        }
    }
}
