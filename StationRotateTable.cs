using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AutoFrameDll;
using CommonTool;
using System.Windows.Forms;
using AutoFrameVision;

namespace AutoFrame
{
    class StationRotateTable : StationBase
    {

        enum POINT
        {
            MARK = 1,
        }

        public string Station;
        public int LastStation;
        public bool First = true;
        bool m_bReady = false;
        public int nMark;
        public StationRotateTable(string strName) : base(strName)
        {
            //this.bPositiveMove[3] =  false ;
            io_in = new string[] { "1.7", "1.8", "1.9", "1.10", "1.11", "1.12", "1.13", "1.14", "1.15", "1.16" };
            io_out = new string[] { "1.5", "1.6", "1.7", "1.8", "1.9", "1.10", "1.11", "1.12", "1.13", "1.14", "1.15", "1.16", "1.17", "1.18", "1.19", "1.20", "2.1", "2.2" ,"3.5"};
            InitSapceInfo();
        }

        public override void InitSecurityState()
        {
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知Ferrite上下料站_OK, false, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知撕料站_OK, false, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知组装站_OK, false, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知保压站_OK, false, false);
        }

        public override void StationInit()
        {
            //各站已准备好，对转盘没有干涉
            //bool 
            nMark = SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_ChooseMark);
            ShowLog("等待各站Ready");
            转盘定位气缸降();
            while (true)//等待各站初始化完成
            {
                CheckContinue(false);
                if (WaitAllStationReady())
                    break;
                WaitTimeDelay(50);
            }
            WaitIo("转盘定位气缸降到位", true, 0);//检查转盘气缸下降到位
            InitSapceInfo();
            for(int i=0;i<4;i++)
            {
                IoMgr.GetInstance().WriteIoBit(ShareInfoSpace.arraySpaceInfo[i].szIoOutDetail[0], false);
                IoMgr.GetInstance().WriteIoBit(ShareInfoSpace.arraySpaceInfo[i].szIoOutDetail[1], false);
                IoMgr.GetInstance().WriteIoBit(ShareInfoSpace.arraySpaceInfo[i].szIoOutDetail[2], false);
                IoMgr.GetInstance().WriteIoBit(ShareInfoSpace.arraySpaceInfo[i].szIoOutDetail[3], false);
                IoMgr.GetInstance().WriteIoBit(ShareInfoSpace.arraySpaceInfo[i].szIoOutDetail[4], false);
                IoMgr.GetInstance().WriteIoBit(ShareInfoSpace.arraySpaceInfo[i].szIoOutDetail[5], false);
            }
            OffCKDIo();
            WaitTimeDelay(1000);
            IoMgr.GetInstance().WriteIoBit("CKD-ALM-RST", true);
            WaitTimeDelay(1000);
            IoMgr.GetInstance().WriteIoBit("CKD-ALM-RST", false);
            WaitTimeDelay(500);
            IoMgr.GetInstance().WriteIoBit("CKD-SERVO-ON",true);
            WaitTimeDelay(1000);
            WaitIo("转盘定位气缸降到位", true, 0);
            IoMgr.GetInstance().WriteIoBit("CKD-ORG", true);
            WaitTimeDelay(100);
            WaitIo("CKD-READ", true, -1);
            //WaitIo("CKD-INP", true, -1);
            IoMgr.GetInstance().WriteIoBit("CKD-ORG", false);
            int nClyTimes = SystemMgr.GetInstance().GetParamInt("TableRotateTimes");
            if(nClyTimes!=ShareInfoSpace.m_nRotateTimes)
            {
                WarningMgr.GetInstance().Error("转盘初始化旋转次数配置不对");
            }
            for(;0!=nClyTimes;nClyTimes--)
            {
                转盘旋转();               //DD 马达运动
                WaitTimeDelay(1000);
            }
            转盘定位气缸升();
            WaitIo("转盘定位气缸升到位", true, 0);

            //CommuToAllStation(true);
            ShowLog("初始化完成");
        }


        public override void StationProcess()
        {
            //WaitBegin();
            while(true)
            {
                CheckContinue(false);
                if (StationMgr.GetInstance().BAutoMode)
                    break;
                if (IoMgr.GetInstance().ReadIoInBit("启动"))
                    break;
                WaitTimeDelay(50);
            }
            if(nMark==3||nMark==4)
            {
                MarkProcess();
                goto label_Over;
            }
            CommuToAllStation(true);
            //等待各站准备好
            ShowLog("等待各站Ready");
            DateTime CTBegin = DateTime.Now;
            while (true)
            {
                CheckContinue(false);
                if (WaitAllStationReady())
                    break;
                WaitTimeDelay(20);
            }
            转盘定位气缸降();
            WaitIo("转盘定位气缸降到位", true, 0);//检查转盘气缸下降到位
            //WaitTimeDelay(100);
            if(false==StationMgr.GetInstance().GetStation("Ferrite上下料站").StationEnable)
            {
                ShareInfoSpace.SetSpaceInfobRult(true,1,1);
                ShareInfoSpace.SetSpaceInfobRult(true,1,2);
            }
            转盘旋转();               //DD 马达运动
            WaitTimeDelay(100);
            转盘定位气缸升();            //转盘气缸上升
            WaitIo("转盘定位气缸升到位", true, 0);
//            WaitTimeDelay(8000);
            ShareInfoSpace.UpdateSpaceInfo();   //
            //WaitTimeDelay(100);
            
            TimeSpan CTTime = DateTime.Now - CTBegin;
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.TotalCT, CTTime.TotalSeconds);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_运行数据保存, true, true);
        label_Over:
            ;

        }

        
        public override void EmgStop()
        {
            IoMgr.GetInstance().WriteIoBit("CKD-SERVO-ON", false);
            base.EmgStop();
        }

        public override void StationDeinit()
        {
            //伺服下电
            转盘定位气缸降();
            IoMgr.GetInstance().WriteIoBit("CKD-SERVO-ON", false);
            Thread.Sleep(100);
            OffVacuum();
            base.StationDeinit();
        }

        /// <summary>
        /// 转盘定位气缸升
        /// </summary>
        public void 转盘定位气缸升()
        {
            IoMgr.GetInstance().WriteIoBit("转盘定位气缸降", false);
            Thread.Sleep(50);
            IoMgr.GetInstance().WriteIoBit("转盘定位气缸升", true);
        }

        /// <summary>
        /// 转盘定位气缸降
        /// </summary>
        public void 转盘定位气缸降()
        {
            IoMgr.GetInstance().WriteIoBit("转盘定位气缸升", false);
            Thread.Sleep(50);
            IoMgr.GetInstance().WriteIoBit("转盘定位气缸降", true);
        }

        public void MarkProcess()
        {
           
            string b = String.Format("工位{0}真空检测1", SystemMgr.GetInstance().GetParamInt("MarkStation"));
            int nIndex = SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_ChooseMark);   //3为检测，4为训练

            while (true)
            {
                CheckContinue(false);
                转盘定位气缸降();
                WaitIo("转盘定位气缸降到位", true, 0);
                WaitTimeDelay(300);
                转盘旋转();
                WaitTimeDelay(300);
                转盘定位气缸升();
                WaitTimeDelay(100);
                ShareInfoSpace.UpdateSpaceInfo();
                string a = ShareInfoSpace.GetSpaceInfo((int)StationIndex.Coil组装站).szIoInDetail[0];
                if (a==b)
                {
                    break;
                }
            }
           
            switch (nIndex)
            {
                case 3:
                    CongexVision.GetInstance().ProcessStep(Vision_Step.Mark_Check_T3);
                    double x = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Rult_CheckX);
                    double y = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Rult_CheckY);
                    if (Math.Abs(x) > SystemMgr.GetInstance().GetParamDouble("CheckValue") || Math.Abs(y) > SystemMgr.GetInstance().GetParamDouble("CheckValue"))
                    {
                        MessageBox.Show("X= "+x+","+ "Y= "+ y +";" + " T3检测失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show("X= " + x + "," + "Y= " + y + ";" + " T3检测成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    CongexVision.GetInstance().ProcessStep(Vision_Step.Mark_Check_T4);
                    double X = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Rult_CheckX);
                    double Y = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Rult_CheckY);
                    if (Math.Abs(x) > SystemMgr.GetInstance().GetParamDouble("CheckValue") || Math.Abs(y) > SystemMgr.GetInstance().GetParamDouble("CheckValue"))
                    {
                        MessageBox.Show("X= " + X + "," + "Y= " + Y + ";" + " T4检测失败", "提示",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show("X= " + X + "," + "Y= " + Y + ";" + " T4检测成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    break;

                case 4:
                    if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Mark_Train_T3))
                    {
                        MessageBox.Show("T3训练失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show("T3训练成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Mark_Train_T4))
                    {
                        MessageBox.Show("T4训练失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show("T4训练成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    break;

            }
        }


        /// <summary>
        /// DD马达运转
        /// </summary>
        public void 转盘旋转()
        {
            WaitIo("保压气缸1下降到位", false, 0);
            WaitIo("保压气缸1上升到位", true, 0);
            WaitIo("保压气缸2下降到位", false, 0);
            WaitIo("保压气缸2上升到位", true, 0);

            WaitIo("保压气缸1下降到位", false, -1);
            WaitIo("保压气缸1上升到位", true, -1);
            WaitIo("保压气缸2下降到位", false, -1);
            WaitIo("保压气缸2上升到位", true, -1);
            WaitIo("转盘定位气缸升到位", false, -1);
            WaitIo("转盘定位气缸降到位", true, -1);
            IoMgr.GetInstance().WriteIoBit("CKD-START", true);
            while (true)
            {
                if (IoMgr.GetInstance().ReadIoInBit("CKD-INP"))
                {
                    break;
                }
                //WaitTimeDelay(100);
            }
            WaitIo("CKD-READ", true, -1);
            //WaitIo("CKD-INP", true, -1);
            IoMgr.GetInstance().WriteIoBit("CKD-START", false);
        }


        /// <summary>
        /// 向各个站发送工作通知信号
        /// </summary>
        /// <param name="bReady"></param>
        void CommuToAllStation(bool bReady = false)
        {
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知Ferrite上下料站_OK, bReady);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知撕料站_OK, bReady);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知组装站_OK, bReady); 
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知保压站_OK, bReady);
        }

        /// <summary>
        /// 等待各个站工作完成
        /// </summary>
        /// <returns></returns>
        bool WaitAllStationReady()
        {
            bool bFerrite = SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_Ferrite上下料站_Ready) || (false==StationMgr.GetInstance().GetStation("Ferrite上下料站").StationEnable);
            bool bPeel = SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_撕料站_Ready) || (false == StationMgr.GetInstance().GetStation("撕膜站").StationEnable);
            bool bAssmbly= SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_组装站_Ready) || (false == StationMgr.GetInstance().GetStation("组装站").StationEnable);
            bool bPress = SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_保压站_Ready) || (false == StationMgr.GetInstance().GetStation("保压站").StationEnable);
            bool bReady = bFerrite && bPeel && bAssmbly && bPress;
            if (bReady)
            {
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_Ferrite上下料站_Ready, false,false);           
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_撕料站_Ready, false,false);
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_组装站_Ready, false,false);
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_保压站_Ready, false,false);
                ShowLog("收到各站Ready信号");
                return true;
            }
            else
            {
                return false;
            }          
        }

        public void InitSapceInfo()
        {
            ShareInfoSpace.arraySpaceInfo[0].nCub = 1;
            ShareInfoSpace.arraySpaceInfo[0].szIoInDetail = new string[] { "工位1真空检测1", "工位1真空检测2" };
            ShareInfoSpace.arraySpaceInfo[0].szIoOutDetail = new string[] { "工位1真空1真空吸", "工位1真空1真空破", "工位1真空2真空吸", "工位1真空2真空破", "工位1孔真空吸", "工位1孔真空破" };
            ShareInfoSpace.arraySpaceInfo[0].strCode = new string[] { "", ""};
            ShareInfoSpace.arraySpaceInfo[0].m_bRult = new bool[] {false, false };
            ShareInfoSpace.arraySpaceInfo[0].m_nPress = new int[] { 1, 1};
            ShareInfoSpace.arraySpaceInfo[0].m_bUnLoad = false;
            ShareInfoSpace.arraySpaceInfo[0].m_bBanCavity = new bool[] { true, true };

            ShareInfoSpace.arraySpaceInfo[1].nCub = 2;
            ShareInfoSpace.arraySpaceInfo[1].szIoInDetail = new string[] { "工位4真空检测1", "工位4真空检测2" };
            ShareInfoSpace.arraySpaceInfo[1].szIoOutDetail = new string[] { "工位4真空1真空吸", "工位4真空1真空破", "工位4真空2真空吸", "工位4真空2真空破", "工位4孔真空吸", "工位4孔真空破" };
            ShareInfoSpace.arraySpaceInfo[1].strCode = new string[] { "", ""};
            ShareInfoSpace.arraySpaceInfo[1].m_bRult = new bool[] { false, false };
            ShareInfoSpace.arraySpaceInfo[1].m_nPress = new int[] { 1, 1};
            ShareInfoSpace.arraySpaceInfo[1].m_bUnLoad = false;
            ShareInfoSpace.arraySpaceInfo[1].m_bBanCavity = new bool[] { true, true };

            ShareInfoSpace.arraySpaceInfo[2].nCub = 3;
            ShareInfoSpace.arraySpaceInfo[2].szIoInDetail = new string[] { "工位3真空检测1", "工位3真空检测2" };
            ShareInfoSpace.arraySpaceInfo[2].szIoOutDetail = new string[] { "工位3真空1真空吸", "工位3真空1真空破", "工位3真空2真空吸", "工位3真空2真空破", "工位3孔真空吸", "工位3孔真空破" };
            ShareInfoSpace.arraySpaceInfo[2].strCode = new string[] { "", ""};
            ShareInfoSpace.arraySpaceInfo[2].m_bRult = new bool[] { false, false };
            ShareInfoSpace.arraySpaceInfo[2].m_nPress = new int[] { 1, 1};
            ShareInfoSpace.arraySpaceInfo[2].m_bUnLoad = false;
            ShareInfoSpace.arraySpaceInfo[2].m_bBanCavity = new bool[] { true, true };


            ShareInfoSpace.arraySpaceInfo[3].nCub = 4;
            ShareInfoSpace.arraySpaceInfo[3].szIoInDetail = new string[] { "工位2真空检测1", "工位2真空检测2" };
            ShareInfoSpace.arraySpaceInfo[3].szIoOutDetail = new string[] { "工位2真空1真空吸", "工位2真空1真空破", "工位2真空2真空吸", "工位2真空2真空破", "工位2孔真空吸", "工位2孔真空破" };
            ShareInfoSpace.arraySpaceInfo[3].strCode = new string[] { "", ""};
            ShareInfoSpace.arraySpaceInfo[3].m_bRult = new bool[] { false, false };
            ShareInfoSpace.arraySpaceInfo[3].m_nPress = new int[] { 1, 1};
            ShareInfoSpace.arraySpaceInfo[3].m_bUnLoad = false;
            ShareInfoSpace.arraySpaceInfo[3].m_bBanCavity = new bool[] { true, true };
        }
        public void OffCKDIo()
        {
            IoMgr.GetInstance().WriteIoBit("CKD-START",false);
            IoMgr.GetInstance().WriteIoBit("CKD-ORG", false);
            IoMgr.GetInstance().WriteIoBit("CKD-ALM-RST", false);
            IoMgr.GetInstance().WriteIoBit("CKD-SERVO-ON",false);
        }     
        public void OffVacuum()
        {
            IoMgr.GetInstance().WriteIoBit("工位1真空1真空吸", false);
            IoMgr.GetInstance().WriteIoBit("工位1真空2真空吸", false);
            IoMgr.GetInstance().WriteIoBit("工位2真空1真空吸", false);
            IoMgr.GetInstance().WriteIoBit("工位2真空2真空吸", false);
        }
    }
}
