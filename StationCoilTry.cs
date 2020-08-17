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
    class StationCoilTry : StationBase
    {
        enum POINT
        {
            MARK = 1,
            上层Coil料盘第一次扫码位置,
            上层Coil料盘第二次扫码位置,
            上层Coil料盘第三次扫码位置,
            上层Coil料盘供料位置,
            下层Coil料盘第一次扫码位置,
            下层Coil料盘第二次扫码位置,
            下层Coil料盘第三次扫码位置,
            下层Coil料盘供料位置,

        }
        bool bMutex=false;
        bool m_bReady=false;
        bool m_bRuning = true;
        bool bRuning = false;
        bool bTryrun = false;
        bool bSafeGrating = false;
        bool bShaltSafe = false;
        public int nSpeedMax = 400000;
        public TcpLink[] pCodeLink=new TcpLink[4];
        private CognexCode pCode1;
        private CognexCode pCode2;
        private CognexCode pCode3;
        private CognexCode pCode4;
        public StationCoilTry(string strName) : base(strName)
        {
            io_in = new string[] { };
            io_out = new string[] { };
            bMutex = false;
        }

        public override void InitSecurityState()
        {
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_组装机械手通知Coil上料站OK, false, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_Coil料盘准备就绪_Ok, false, false);
            base.InitSecurityState();
        }
        
        public override void StationInit()
        {
            bMutex = false;
            pCode1 = new CognexCode(TcpMgr.GetInstance().GetTcpLink(4), 1);
            pCode2 = new CognexCode(TcpMgr.GetInstance().GetTcpLink(5), 2);
            pCode3 = new CognexCode(TcpMgr.GetInstance().GetTcpLink(6), 3);
            pCode4 = new CognexCode(TcpMgr.GetInstance().GetTcpLink(7), 4);
            pCode1.StartTreadRun();
            pCode2.StartTreadRun();
            pCode3.StartTreadRun();
            pCode4.StartTreadRun();
            bTryrun = SystemMgr.GetInstance().GetParamBool("ProductOrTest");

            if (StationMgr.GetInstance().GetStation("组装站").StationEnable)
            {
                WaitRegBit((int)SysBitReg.Bit_组装机械手通知Coil上料站OK, true);
            } 
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_组装机械手通知Coil上料站OK, false, false);
            MotionMgr.GetInstance().ServoOn(AxisX);
            MotionMgr.GetInstance().ServoOn(AxisY);
            WaitTimeDelay(500);
            MotionMgr.GetInstance().Home(AxisX, 1);
            MotionMgr.GetInstance().Home(AxisY, 1);
            WaitHome(AxisX, -1);
            WaitHome(AxisY, -1);
            double fSpeed = nSpeedMax * SystemMgr.GetInstance().GetParamDouble("CoilSupplySpeed");
            bSafeGrating = SystemMgr.GetInstance().GetParamBool("CoilSafeGratingCheck");
            光栅监控运动(AxisX, m_dicPoint[(int)POINT.MARK].x);//上层上料料盘回
            光栅监控运动(AxisY, m_dicPoint[(int)POINT.MARK].y);//上层上料料盘回
            //MotionMgr.GetInstance().AbsMove(AxisX, m_dicPoint[(int)POINT.MARK].x, (int)(fSpeed/100));
            //MotionMgr.GetInstance().AbsMove(AxisY, m_dicPoint[(int)POINT.MARK].y, (int)(fSpeed/100));
            //WaitMotion(AxisX, -1);
            //WaitMotion(AxisY, -1);
            ShowLog("初始化完成");
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
            
            if (false == bRuning)
            {
                if (!bTryrun)
                {
                    WaitIo("Coil上料上启动", true, -1);
                }
                IoMgr.GetInstance().AlarmLight(LightState.蜂鸣关);
                IoMgr.GetInstance().AlarmLight(LightState.绿灯开);
            }
            else
            {
                bRuning = false;
            }
            //gao
            DateTime CTBegin = DateTime.Now;
            bSafeGrating = SystemMgr.GetInstance().GetParamBool("CoilSafeGratingCheck");
            bShaltSafe = SystemMgr.GetInstance().GetParamBool("CoilShaltCheck");
            if (bMutex)
            {
                if(bSafeGrating)
                {
                    WaitIo("上料下层光纤水平检测", true, 0);
                }
                光栅监控运动(AxisX,m_dicPoint[(int)POINT.MARK].x);//上层上料料盘回
                光栅监控运动(AxisY, m_dicPoint[(int)POINT.下层Coil料盘第一次扫码位置].y);//下层上料气缸进到第一次扫码
                扫条码(1);
                //光栅监控运动(AxisY, m_dicPoint[(int)POINT.下层Coil料盘第二次扫码位置].y);//下层上料气缸进到第二次扫码
                double fSpeed = nSpeedMax * SystemMgr.GetInstance().GetParamDouble("CoilSupplySpeed");
                MotionMgr.GetInstance().AbsMove(AxisY, m_dicPoint[(int)POINT.下层Coil料盘第二次扫码位置].y, (int)(fSpeed / 100));
                WaitMotion(AxisY, -1);
                扫条码(2);
                //光栅监控运动(AxisY, m_dicPoint[(int)POINT.下层Coil料盘第三次扫码位置].y);//下层上料气缸进到第三次扫码
                MotionMgr.GetInstance().AbsMove(AxisY, m_dicPoint[(int)POINT.下层Coil料盘第三次扫码位置].y, (int)(fSpeed / 100));
                WaitMotion(AxisY, -1);
                扫条码(3);
                //光栅监控运动(AxisY, m_dicPoint[(int)POINT.下层Coil料盘供料位置].y);//下层上料气缸进到第三次扫码
                MotionMgr.GetInstance().AbsMove(AxisY, m_dicPoint[(int)POINT.下层Coil料盘供料位置].y, (int)(fSpeed / 100));
                WaitMotion(AxisY, -1);
                SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_Coil上下料盘_Coil, 1);
                bMutex = false;
            }
            else
            {
                if (bSafeGrating)
                {
                    WaitIo("上料上层光纤水平检测", true, 0);
                }
                光栅监控运动(AxisY, m_dicPoint[(int)POINT.MARK].y);//上层上料料盘回
                光栅监控运动(AxisX, m_dicPoint[(int)POINT.上层Coil料盘第一次扫码位置].x);//上层上料气缸进到第一次扫码
                扫条码(1);
                //光栅监控运动(AxisX, m_dicPoint[(int)POINT.上层Coil料盘第二次扫码位置].x);//上层上料气缸进到第二次扫码
                double fSpeed = nSpeedMax * SystemMgr.GetInstance().GetParamDouble("CoilSupplySpeed");
                MotionMgr.GetInstance().AbsMove(AxisX, m_dicPoint[(int)POINT.上层Coil料盘第二次扫码位置].x, (int)(fSpeed / 100));
                WaitMotion(AxisX, -1);
                扫条码(2);
                //光栅监控运动(AxisX, m_dicPoint[(int)POINT.上层Coil料盘第三次扫码位置].x);//上层上料气缸进到第三次扫码
                MotionMgr.GetInstance().AbsMove(AxisX, m_dicPoint[(int)POINT.上层Coil料盘第三次扫码位置].x, (int)(fSpeed / 100));
                WaitMotion(AxisX, -1);
                扫条码(3);
                //光栅监控运动(AxisX, m_dicPoint[(int)POINT.上层Coil料盘供料位置].x);//上层上料气缸进到第三次扫码
                MotionMgr.GetInstance().AbsMove(AxisX, m_dicPoint[(int)POINT.上层Coil料盘供料位置].x, (int)(fSpeed / 100));
                WaitMotion(AxisX, -1);
                SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_Coil上下料盘_Coil, 0);
                bMutex = true;
            }
            for(int i=0;i<12;i++)
            {
                ShowLog(ShareInfoSpace.arrayCodeInfo[i].szCode);
            }
            
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_Coil料盘准备就绪_Ok, true, false);

            //gao
            TimeSpan CTTime1 = DateTime.Now - CTBegin;
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.CoilTrayCT, CTTime1.TotalSeconds);

            if (StationMgr.GetInstance().GetStation("组装站").StationEnable)
            {
                IoMgr.GetInstance().AlarmLight(LightState.黄灯开, 0);
                IoMgr.GetInstance().AlarmLight(LightState.蜂鸣闪, 5000);
                while (true)
                {
                    if (IoMgr.GetInstance().ReadIoInBit("Coil上料上启动")||bTryrun)
                    {
                        bRuning = true;
                        break;
                    }
                    WaitTimeDelay(100);
                }
                IoMgr.GetInstance().AlarmLight(LightState.蜂鸣关);
                IoMgr.GetInstance().AlarmLight(LightState.绿灯开);
                WaitRegBit((int)SysBitReg.Bit_Coil料盘准备就绪_Ok, false, -1);
            }
            else
            {
                WaitTimeDelay(5000);
            }
            
        }

        public override void StationDeinit()
        {
            if(pCode1!=null)
            {
                pCode1.StopThreadRun();
            }
            if (pCode2 != null)
            {
                pCode2.StopThreadRun();
            }
            if (pCode3 != null)
            {
                pCode3.StopThreadRun();
            }
            if (pCode4 != null)
            {
                pCode4.StopThreadRun();
            }
            base.StationDeinit();
        }
        /// <summary>
        /// 上层料盘进
        /// </summary>
        public void 上层上料气缸进()
        {
            IoMgr.GetInstance().WriteIoBit("上层上料气缸回", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("上层上料气缸进", true);
        }

        /// <summary>
        /// 上层料盘弹出
        /// </summary>
        public void 光栅监控运动(int nAxis,int nPos)
        {
            double nSpeed = nSpeedMax * SystemMgr.GetInstance().GetParamDouble("CoilSupplySpeed")/100.0;
            MotionMgr.GetInstance().AbsMove(nAxis, nPos, (int)nSpeed);
            while(true)
            {
                if (ShareInfoSpace.GetAxisStation())
                {
                    MotionMgr.GetInstance().StopAxis(nAxis);
                    return;
                }
                    
                int nRult= MotionMgr.GetInstance().IsAxisInPos(nAxis);
                if (/*-1== nRult || */0 == nRult)
                {
                    //if(-1 == nRult)
                    //{
                    //    WarningMgr.GetInstance().Error("Coil上料轴报错！");
                    //}
                    break;
                }
                if(bShaltSafe)
                {
                    if (false == IoMgr.GetInstance().ReadIoInBit("光栅1") || false == IoMgr.GetInstance().ReadIoInBit("光栅2"))
                    {
                        MotionMgr.GetInstance().StopAxis(nAxis);
                        WaitIo("光栅1", true, -1);
                        WaitIo("光栅2", true, -1);
                        MotionMgr.GetInstance().AbsMove(nAxis, nPos, (int)nSpeed);
                    }
                }         
                Thread.Sleep(10);
            }
        }
        public void 扫条码(int nIndex)
        {
            if(SystemMgr.GetInstance().GetParamBool("CognexCodeEnable"))
            {
                pCode1.SetCodeRuning(true, nIndex);
                pCode2.SetCodeRuning(true, nIndex);
                pCode3.SetCodeRuning(true, nIndex);
                pCode4.SetCodeRuning(true, nIndex);
                while(true)
                {
                    if(pCode1.IsCodeFinish() && pCode2.IsCodeFinish() && pCode3.IsCodeFinish() && pCode4.IsCodeFinish())
                    {
                        pCode1.SetCodeReady(false);
                        pCode2.SetCodeReady(false);
                        pCode3.SetCodeReady(false);
                        pCode4.SetCodeReady(false);
                        break;
                    }
                    WaitTimeDelay(50);
                }
            }
            else
            {
                for(int i= 4*(nIndex-1); i< nIndex * 4; i++)
                {
                    ShareInfoSpace.arrayCodeInfo[i].m_bRult = true;
                    ShareInfoSpace.arrayCodeInfo[i].szCode = "111111233344";
                }
                WaitTimeDelay(100);
            }
        }
        /// <summary>
        /// 下层料盘进
        /// </summary>
        public void 下层上料气缸进()
        {
            IoMgr.GetInstance().WriteIoBit("下层上料气缸回", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("下层上料气缸进", true);
        }

        /// <summary>
        /// 下层料盘弹出
        /// </summary>
        public void 下层上料气缸回()
        {
            IoMgr.GetInstance().WriteIoBit("下层上料气缸进", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("下层上料气缸回", true);
        }
    }
}
