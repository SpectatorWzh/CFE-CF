using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFrameDll;
using CommonTool;
using Communicate;
using System.Threading;

namespace AutoFrame
{
    class StationFeedFerriteTry : StationBase
    {
        enum POINT
        {
            MARK = 1,
            Z_LIMIT,
            Z_LIMIT_UP,
        }
        private int nZspeed = 300000;
        private int nUspeed = 300000;
        long nOffsetStep = 0;
        bool m_bReady = false;
        //private long nYspeed = 300000;
        public StationFeedFerriteTry(string strName) : base(strName)
        {
                io_in = new string[] { "3.1", "3.2", "3.3", "3.4", "3.5", "3.6", "3.7", "3.8", "3.9", "3.10", "3.11","3.12","3.13","3.14","3.15" };
                io_out = new string[] { "3.5", "3.6", "3.7", "3.8", "3.9", "3.10", "3.11", "3.12" };
        }

        public override void InitSecurityState()
         {
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_供料Try通知下料Try_InitOk, false, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_供料Try通知下料Try_ReadOk, false, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_收料Tray盘站通知供料Try盘站上料, true, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_收料Tray盘站通知供料Tray盘站气缸松开, true, false);
            SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_空跑_Count, 1, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_StartAxisRun, false);
            ShareInfoSpace.SetAxisRunStation(false);
        }

        public override bool IsReady()
        {
            return true;
        }

        public override void StationInit()
        {
            TryInit();
        }

        public override void StationProcess()
        {
            //base.StationProcess();
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
        }

        /// <summary>
        /// 料盘模式的流程
        /// </summary>
        public void TryProcess()
        {
            if (IsEmptyTry())
            {
                PeedFerrite();
            }
            else
            {
                供料盘抽屉锁锁紧();
                WaitIo("供料抽屉锁紧气缸锁锁紧", true, 0);
                ShareInfoSpace.SetFeedTryState(true);
            }
            WaitRegBit((int)SysBitReg.Bit_收料Tray盘站通知供料Tray盘站气缸松开, true);
            ClyFixedLoosen();
            等待气缸松到位();
            托盘限位气缸缩回();//deng
            等待托盘限位气缸缩回到位();
            WaitTimeDelay(100);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_收料Tray盘站通知供料Tray盘站气缸松开, false, false);
            WaitRegBit((int)SysBitReg.Bit_收料Tray盘站通知供料Try盘站上料, true, -1);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_收料Tray盘站通知供料Try盘站上料, false, false);
            托盘限位气缸伸出();//deng
            等待托盘限位气缸伸出到位();
            MotionMgr.GetInstance().VelocityMove(AxisZ, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("FeedAxisZSlowSpeed")) / 100.0));
            while (true)
            {
                if (ShareInfoSpace.GetAxisStation())
                    goto label_over;
                if (SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_StartAxisRun))
                {
                    MotionMgr.GetInstance().StopAxis(AxisZ);
                    WaitRegBit((int)SysBitReg.Bit_StartAxisRun, false, -1);
                    MotionMgr.GetInstance().VelocityMove(AxisZ, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("FeedAxisZSlowSpeed")) / 100.0));
                }
                if (false == IoMgr.GetInstance().GetIoInState("供料盘升预到位检测"))
                {
                    MotionMgr.GetInstance().StopAxis(AxisZ);
                    break;
                }
                if (IsEmptyTry())
                {
                    MotionMgr.GetInstance().StopAxis(AxisZ);
                    PeedFerrite();
                    MotionMgr.GetInstance().VelocityMove(AxisZ, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("FeedAxisZSlowSpeed")) / 100.0));
                }
                Thread.Sleep(10);
            }

            long nCurrPos = (long)(1000 * SystemMgr.GetInstance().GetParamDouble("StepFixedTryUp")) + MotionMgr.GetInstance().GetAixsPos(AxisZ);
            MotionMgr.GetInstance().AbsMove(AxisZ, (int)nCurrPos, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("FeedAxisZSlowSpeed")) / 100.0));
            WaitMotion(AxisZ, -1);

            ClyFixedLock();
            等待气缸紧到位();
            if (StationMgr.GetInstance().GetStation("Ferrite上下料站").StationEnable)
            {
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_供料Tray盘站通知Ferrite机器人取料_Ok, true, false);
            }
            else
            {
                WaitTimeDelay(5000);
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_料盘空_Empy, true, false);
            }
        label_over:
            ;
        }



        /// <summary>
        /// 料盘Try模式初始化
        /// </summary>
        public void TryInit()
        {

            //IoMgr.GetInstance().WriteIoBit("Tray搬运气缸向收料站伸出", true);
            //WaitIo("Tray搬运气缸到达下料盘", true);
            //IoMgr.GetInstance().WriteIoBit("料盘搬运吸真空", false);
            //WaitTimeDelay(50);
            //IoMgr.GetInstance().WriteIoBit("料盘搬运破真空", false);
            ClyFixedLoosen();
            等待气缸松到位();
            托盘限位气缸缩回();
            等待托盘限位气缸缩回到位();
            //脱料气缸上();
            //等待脱料气缸上到位();
            MotionMgr.GetInstance().ServoOn(AxisZ);
            WaitTimeDelay(400);

            MotionMgr.GetInstance().Home(AxisZ, 1);  // 负方向回原点
            WaitHome(AxisZ, -1);

            供料盘抽屉锁开();
            WaitIo("供料抽屉锁紧气缸锁松开", true, 0);
            MotionMgr.GetInstance().AbsMove(AxisZ, m_dicPoint[(int)POINT.MARK].z, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("FeedAxisZSpeed"))/100.0));
            WaitMotion(AxisZ, -1);
            WaitRegBit((int)SysBitReg.Bit_下料Try通知供料Try_InitOk, true,-1);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_下料Try通知供料Try_InitOk, false, false);
            ShowLog("料盘Try盘站初始化完成");
        }

        public override void StationDeinit()
        {
            MotionMgr.GetInstance().StopAxis(AxisZ);
            MotionMgr.GetInstance().ServoOff(AxisZ);
        }
        /// <summary>
        /// 判断料盘是否为空
        /// 空返回 true
        /// 有料返回 false
        /// </summary>
        /// <returns></returns>
        public bool IsEmptyTry()
        {
            long nCurrPos = MotionMgr.GetInstance().GetAixsPos(AxisZ);
            if (false == IoMgr.GetInstance().GetIoInState("供料料盘有无料检测") || nCurrPos > m_dicPoint[(int)POINT.Z_LIMIT].z)
            {
                ShareInfoSpace.SetFeedTryState(false);
                return true;
            }
            return false;
        }

        //public bool IsUpEmptyTry()
        //{
        //    if (nOffsetStep>)
        //        return true;
        //    return false;
        //}
        /// <summary>
        /// 无料时操作员上料流程
        /// </summary>
        public void PeedFerrite()
        {
            MotionMgr.GetInstance().AbsMove(AxisZ, m_dicPoint[(int)POINT.MARK].z, (int)(nZspeed * (SystemMgr.GetInstance().GetParamDouble("FeedAxisZSpeed"))/100.0));
            WaitMotion(AxisZ, -1);
            供料盘抽屉锁开();
            WaitIo("供料抽屉锁紧气缸锁松开", true, 0);
            WaitIo("供料抽屉到位检测", false, -1);
            WaitIo("供料料盘有无料检测", true, -1);
            WaitIo("供料抽屉到位检测", true, -1);
            供料盘抽屉锁锁紧();
            WaitIo("供料抽屉锁紧气缸锁锁紧", true, 0);
            ShareInfoSpace.SetFeedTryState(true);
        }
       

        /// <summary>
        /// 夹紧气缸夹紧
        /// </summary>
        public void ClyFixedLock()
        {
            IoMgr.GetInstance().WriteIoBit("后夹紧气缸松", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("后夹紧气缸紧", true);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("前夹紧气缸松", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("前夹紧气缸紧", true);    
        }


        /// <summary>
        /// 夹紧气缸松开
        /// </summary>
        public void ClyFixedLoosen()  
        {
            IoMgr.GetInstance().WriteIoBit("前夹紧气缸紧", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("前夹紧气缸松", true);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("后夹紧气缸紧", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("后夹紧气缸松", true);
        }


        /// <summary>
        /// 搬运气缸伸出
        /// </summary>
        public void 搬运气缸向收料站伸出()  
        {
            IoMgr.GetInstance().WriteIoBit("Tray搬运气缸向供料站返回", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("Tray搬运气缸向收料站伸出", true);
        }


        /// <summary>
        /// 搬运气缸伸出
        /// </summary>
        public void 搬运气缸向供料站返回() 
        {
            IoMgr.GetInstance().WriteIoBit("Tray搬运气缸向收料站伸出", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("Tray搬运气缸向供料站返回", true);
        }

        /// <summary>
        /// 供料抽屉锁紧气缸关闭
        /// </summary>
        public void 供料盘抽屉锁开()
        {
            IoMgr.GetInstance().WriteIoBit("供料抽屉锁紧气缸紧", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("供料抽屉锁紧气缸松", true);
        }

        /// <summary>
        /// 供料抽屉锁紧气缸打开
        /// </summary>
        public void 供料盘抽屉锁锁紧()
        {
            IoMgr.GetInstance().WriteIoBit("供料抽屉锁紧气缸松", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("供料抽屉锁紧气缸紧", true);
        }

        public void CylLockUnloadOpen()
        {
            IoMgr.GetInstance().WriteIoBit("收料抽屉锁紧气缸紧", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("收料抽屉锁紧气缸松", true);
        }

        /// <summary>
        /// 供料抽屉锁紧气缸打开
        /// </summary>
        public void CylLockUnloadClose()
        {
            IoMgr.GetInstance().WriteIoBit("供料抽屉锁紧气缸紧", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("供料抽屉锁紧气缸松", true);
        }
        public void 托盘限位气缸伸出()//deng
        {
            IoMgr.GetInstance().WriteIoBit("托盘左1限位气缸缩回", false);
            IoMgr.GetInstance().WriteIoBit("托盘左2限位气缸缩回", false);
            IoMgr.GetInstance().WriteIoBit("托盘右1限位气缸缩回", false);
            IoMgr.GetInstance().WriteIoBit("托盘右2限位气缸缩回", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("托盘左1限位气缸伸出", true);
            IoMgr.GetInstance().WriteIoBit("托盘左2限位气缸伸出", true);
            IoMgr.GetInstance().WriteIoBit("托盘右1限位气缸伸出", true);
            IoMgr.GetInstance().WriteIoBit("托盘右2限位气缸伸出", true);

        }

        public void 托盘限位气缸缩回()//deng
        {
            IoMgr.GetInstance().WriteIoBit("托盘左1限位气缸伸出", false);
            IoMgr.GetInstance().WriteIoBit("托盘左2限位气缸伸出", false);
            IoMgr.GetInstance().WriteIoBit("托盘右1限位气缸伸出", false);
            IoMgr.GetInstance().WriteIoBit("托盘右2限位气缸伸出", false);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("托盘左1限位气缸缩回", true);
            IoMgr.GetInstance().WriteIoBit("托盘左2限位气缸缩回", true);
            IoMgr.GetInstance().WriteIoBit("托盘右1限位气缸缩回", true);
            IoMgr.GetInstance().WriteIoBit("托盘右2限位气缸缩回", true);

        }
        public void 等待托盘限位气缸伸出到位()//deng
        {
            WaitIo("托盘左1限位气缸伸出到位", true, 0);
            WaitIo("托盘左2限位气缸伸出到位", true, 0);
            WaitIo("托盘右1限位气缸伸出到位", true, 0);
            WaitIo("托盘右2限位气缸伸出到位", true, 0);
        }

        public void 等待托盘限位气缸缩回到位()//deng
        {
            WaitIo("托盘左1限位气缸缩回到位", true, 0);
            WaitIo("托盘左2限位气缸缩回到位", true, 0);
            WaitIo("托盘右1限位气缸缩回到位", true, 0);
            WaitIo("托盘右2限位气缸缩回到位", true, 0);
        }



        //public void 脱料气缸上()
        //{
        //    IoMgr.GetInstance().WriteIoBit("前脱料气缸下", false);
        //    WaitTimeDelay(20);
        //    IoMgr.GetInstance().WriteIoBit("前脱料气缸上", true);
        //    WaitTimeDelay(20);
        //    IoMgr.GetInstance().WriteIoBit("后脱料气缸下", false);
        //    WaitTimeDelay(20);
        //    IoMgr.GetInstance().WriteIoBit("后脱料气缸上", true);
        //}

        //public void 脱料气缸下()
        //{
        //    IoMgr.GetInstance().WriteIoBit("前脱料气缸上", false);
        //    IoMgr.GetInstance().WriteIoBit("前脱料气缸下", true);
        //    IoMgr.GetInstance().WriteIoBit("后脱料气缸上", false);
        //    IoMgr.GetInstance().WriteIoBit("后脱料气缸下", true); 
        //}

        public void 等待气缸紧到位()
        {
            WaitIo("前夹紧气缸紧到位", true, 0);
            WaitIo("后夹紧气缸紧到位", true, 0);
        }
        public void 等待气缸松到位()
        {
            WaitIo("前夹紧气缸松到位", true, 0);
            WaitIo("后夹紧气缸松到位", true, 0);
        }
        //public void 等待脱料气缸上到位()
        //{
        //    WaitIo("前脱料气缸上到位", true, 0);
        //    WaitIo("后脱料气缸上到位", true, 0);
        //}
        //public void 等待脱料气缸下到位()
        //{
        //    WaitIo("前脱料气缸下到位", true, 0);
        //    WaitIo("后脱料气缸下到位", true, 0);
        //}

    }
}
