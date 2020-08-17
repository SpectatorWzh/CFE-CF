using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFrameDll;
using CommonTool;
using Communicate;
using System.Threading;
using System.Windows.Forms;

namespace AutoFrame
{
    class StationUnLoadProductTry : StationBase
    {
        enum POINT
        {
            MARK = 1,
            上层成品料盘接料位置,
            下层成品料盘接料位置,
        }
        public int nSpeedMax = 300000;
        bool bMutex;
        bool bSafeGrating = false;
        bool bShaltSafe = false;
        public StationUnLoadProductTry(string strName) : base(strName)
        {
            io_in = new string[] {};
            io_out = new string[] {};
        }

        public override void InitSecurityState()
        {
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_下成品料站通知FerriteLoad下料_Ready, false, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_ferrite机械手通知ferrite上料站OK, false, false);
            base.InitSecurityState();
        }
        public override void StationInit()
        {
            bMutex = false;
            //deng
           if (StationMgr.GetInstance().GetStation("Ferrite上下料站").StationEnable)
            {
                WaitRegBit((int)SysBitReg.Bit_ferrite机械手通知ferrite上料站OK, true);
            } 
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_ferrite机械手通知ferrite上料站OK, false, false);
            MotionMgr.GetInstance().ServoOn(AxisX);
            MotionMgr.GetInstance().ServoOn(AxisY);
            WaitTimeDelay(500);
            MotionMgr.GetInstance().Home(AxisX, 1);
            MotionMgr.GetInstance().Home(AxisY, 1);
            WaitHome(AxisX, -1);
            WaitHome(AxisY, -1);
            double fSpeed = nSpeedMax * SystemMgr.GetInstance().GetParamDouble("ProductUnloadSpeed");
            //MotionMgr.GetInstance().AbsMove(AxisX, m_dicPoint[(int)POINT.MARK].x, (int)(fSpeed * 0.01));
            //MotionMgr.GetInstance().AbsMove(AxisY, m_dicPoint[(int)POINT.MARK].y, (int)(fSpeed * 0.01));
            //WaitMotion(AxisX, -1);
            //WaitMotion(AxisY, -1);
            bSafeGrating = SystemMgr.GetInstance().GetParamBool("CoilFerriteSafeGratingCheck");
            光栅监控运动(AxisX, m_dicPoint[(int)POINT.MARK].x);//上层上料料盘回
            光栅监控运动(AxisY, m_dicPoint[(int)POINT.MARK].y);//上层上料料盘回
            ShowLog("初始化完成");
        }

        public override void StationProcess()
        {
            while (true)
            {
                CheckContinue(false);
                if (StationMgr.GetInstance().BAutoMode || IoMgr.GetInstance().ReadIoInBit("启动"))
                    break;
                if (IoMgr.GetInstance().ReadIoInBit("成品上料下启动"))
                    break;
                WaitTimeDelay(50);
            }
            bSafeGrating = SystemMgr.GetInstance().GetParamBool("CoilFerriteSafeGratingCheck");
            bShaltSafe = SystemMgr.GetInstance().GetParamBool("CoilFerriteShaltCheck");
            if (bMutex)
            { //deng
                if (!SystemMgr.GetInstance().GetParamBool("ProductOrTest"))
                {
                    if(bSafeGrating)
                    {
                        WaitIo("放成品料下层检测料盘有无", true, 0);
                        WaitIo("放成品料下层光纤水平检测", true, 0);
                    }
                }

                //double fSpeed = nSpeedMax * SystemMgr.GetInstance().GetParamDouble("CoilSupplySpeed");
                //MotionMgr.GetInstance().AbsMove(AxisX, m_dicPoint[(int)POINT.MARK].x,(int)(fSpeed / 100));//上层收料盘回mark点
                //MotionMgr.GetInstance().AbsMove(AxisY, m_dicPoint[(int)POINT.下层成品料盘接料位置].y, (int)(fSpeed / 100));//下层接成品料
                光栅监控运动(AxisX, m_dicPoint[(int)POINT.MARK].x);//上层收料盘回mark点
                光栅监控运动(AxisY, m_dicPoint[(int)POINT.下层成品料盘接料位置].y);//下层接成品料
                SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_Coil和Ferrite成品上下料盘, 1);//机械手说明是下层
                bMutex = false;
            }
            else
            {//deng
               if (!SystemMgr.GetInstance().GetParamBool("ProductOrTest"))
                {
                    if (bSafeGrating)
                    {
                        WaitIo("放成品料上层检测料盘有无", true, 0);
                        WaitIo("放成品料上层光纤水平检测", true, 0);
                    }
                }

                //double fSpeed = nSpeedMax * SystemMgr.GetInstance().GetParamDouble("CoilSupplySpeed");
                //MotionMgr.GetInstance().AbsMove(AxisY, m_dicPoint[(int)POINT.MARK].y, (int)(fSpeed / 100));//上层收料盘回mark点
                //MotionMgr.GetInstance().AbsMove(AxisX, m_dicPoint[(int)POINT.上层成品料盘接料位置].x, (int)(fSpeed / 100));//下层接成品料
                光栅监控运动(AxisY, m_dicPoint[(int)POINT.MARK].y);//下层收料盘回mark点
                光栅监控运动(AxisX, m_dicPoint[(int)POINT.上层成品料盘接料位置].x);//上层接成品料
                SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_Coil和Ferrite成品上下料盘, 0);//机械手说明是上层
                bMutex = true;
            }
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_下成品料站通知FerriteLoad下料_Ready, true, false);
            if (StationMgr.GetInstance().GetStation("Ferrite上下料站").StationEnable)
            {
                WaitRegBit((int)SysBitReg.Bit_下成品料站通知FerriteLoad下料_Ready, false, -1);
            }
            else
            {
                WaitTimeDelay(5000);
            }
        }
        public override void StationDeinit()
        {
            base.StationDeinit();
        }

        public void 光栅监控运动(int nAxis, int nPos)
        {
            double nSpeed = nSpeedMax * SystemMgr.GetInstance().GetParamDouble("CoilSupplySpeed") / 100.0;
            MotionMgr.GetInstance().AbsMove(nAxis, nPos, (int)nSpeed);
            while (true)
            {
                if (ShareInfoSpace.GetAxisStation())
                {
                    MotionMgr.GetInstance().StopAxis(nAxis);
                    return;
                }

                int nRult = MotionMgr.GetInstance().IsAxisInPos(nAxis);
                if (/*-1== nRult || */0 == nRult)
                {
                    //if(-1 == nRult)
                    //{
                    //    WarningMgr.GetInstance().Error("Coil上料轴报错！");
                    //}
                    break;
                }
                if (bShaltSafe)
                {
                    if (false == IoMgr.GetInstance().ReadIoInBit("光栅3") || false == IoMgr.GetInstance().ReadIoInBit("光栅4"))
                    {
                        MotionMgr.GetInstance().StopAxis(nAxis);
                        WaitIo("光栅3", true, -1);
                        WaitIo("光栅4", true, -1);
                        MotionMgr.GetInstance().AbsMove(nAxis, nPos, (int)nSpeed);
                    }
                }
                Thread.Sleep(10);
            }
        }
    }
}
