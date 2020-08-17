using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using AutoFrameDll;
using CommonTool;
using Communicate;
using System.Windows.Forms;

namespace AutoFrame
{
    class StationFerriteLoad : StationBase
    {
        enum POINT
        {
            MARK = 1,
        }

        public int nIndexFecth;
        public int nIndexUnLoad;
        public int nSpeedPerce;
        public bool bModeProduct;
        public bool bFetchOk;
        public int nFetchModeProduct;
        public int nCalibInde;
        public int nCalib;
        public bool m_bReady = false;
        public bool[] bRultSnap;
        public bool bUnLoad;
        public bool bClearTable = false;
        public TcpLink m_tcpRobotComm;
        public StationFerriteLoad(string strName) : base(strName)
        {
            io_in = new string[] {};
            io_out = new string[] { "2.17", "2.18", "2.19", "2.20" };
            bRultSnap = new bool[2] { false, false };
            m_tcpRobotComm = TcpMgr.GetInstance().GetTcpLink(0);
            nIndexFecth = 1;
            nIndexUnLoad = 1;
        }
        public override void InitSecurityState()
        {
            SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_Ferrite上下料站_Ready),false,false);
            SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_T1_1_处理结果_Result), false, false);
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.Double_T1_X, 0, false);
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.Double_T1_Y, 0, false);
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.Double_T1_U, 0, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_供料Tray盘站通知Ferrite机器人取料_Ok, false, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_下成品料站通知FerriteLoad下料_Ready, false, false);
            base.InitSecurityState();
        }
        public override bool IsReady()
        {
            return true;
        }
        public override void StationInit()
        {
            int nCommNum = 0;
            nCalib = 0;
            nIndexFecth = 1;
            nIndexUnLoad = 1;
            bUnLoad = false;
            nFetchModeProduct = 1;
            bFetchOk = false;
            bModeProduct = SystemMgr.GetInstance().GetParamBool("ProductOrTest");
            CongexVision.GetInstance().CongexVisionInit();
            //SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_Process_Step, 0, true);
           
            nCalib = SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_Calib_Index);
           
            WaitTimeDelay(300);
        LineRobot:
            nCommNum++;
            //RobotSevorOn();
            ShowLog("打开Ferrite上下料机器人通讯端");
            if (m_tcpRobotComm.IsOpen())
                m_tcpRobotComm.Close();

            try
            {
                m_tcpRobotComm.Open();
                //wait_recevie_cmd(m_tcpRobotComm, "Ready\r\n", 10000);
                string szBuffer;
                if(bModeProduct)
                {
                    nSpeedPerce = SystemMgr.GetInstance().GetParamInt("SpeedRobotFerrite");
                    szBuffer = String.Format("Init,1,{0},0,0,0,0",nSpeedPerce);
                    //szBuffer = "Init,1,0,0,0,0";
                }
                else
                {
                    nSpeedPerce = SystemMgr.GetInstance().GetParamInt("SpeedRobotFerrite");
                    szBuffer = String.Format("Init,0,{0},0,0,0,0",nSpeedPerce);
                    //szBuffer = "Init,0,0,0,0,0";
                }
                m_tcpRobotComm.WriteLine(szBuffer);
                wait_recevie_cmd(m_tcpRobotComm, "Ini_Ok\r\n", 15000);
            }
            catch (Exception)
            {
                if (nCommNum < 5)
                    goto LineRobot;
                else
                    WarningMgr.GetInstance().Error("Ferrite上下料站机器人启动失败");
            }
            SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_Ferrite上下料站_Ready), true, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_ferrite机械手通知ferrite上料站OK, true, false);
            ShowLog("初始化完成");
        }

        public override void StationProcess()
        {
            DateTime BeginTime;
            BeginTime = DateTime.Now;
            while (true)
            {
                CheckContinue(false);
                if (StationMgr.GetInstance().BAutoMode)
                    break;
                if (IoMgr.GetInstance().ReadIoInBit("启动"))
                    break;
                WaitTimeDelay(50);
            }
            //SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_Process_Step, 0, true);
            if (1 == nCalib)//
            {
                nCalibInde = SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_Calib_T1_Index);
                if (0 == nCalibInde || 1 == nCalibInde)
                {
                    CalibProcess(nCalibInde);
                }
                else
                {
                    CalibTrain(nCalibInde);
                }
                goto label_Over;
            }
            if(SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_ChooseMark)==3|| SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_ChooseMark) == 4)
            {
                WaitTimeDelay(50);
                goto label_Over;
            }
            string szBuff = "";
            bClearTable=ShareInfoSpace.GetRotateTableSignal();
            if (bFetchOk)
            {
                bFetchOk = false;
                goto label_FetchOk;
            }
            //  Try_Fetch_Doing:
            取Ferrite料();//取Ferrite料

            label_FetchOk:
            if (false==bClearTable)
            {
              TrySnap:
                bRultSnap[0] = CamSnap(Vision_Step.T1_1, 1, 1);
                m_tcpRobotComm.WriteLine("MoveToSnap2,0,0,0,0,0,0");
                while (true)
                {
                    CheckContinue(false);
                    if (m_tcpRobotComm.ReadLine(out szBuff) > 0)
                    {
                        szBuff = szBuff.Trim();
                        string[] szSplit = szBuff.Split(',');
                        if (0 == szSplit[0].CompareTo("MoveToSnap2"))
                            break;
                    }
                    WaitTimeDelay(50);
                }
                string[] szSplit2 = szBuff.Split(',');
                CongexVision.GetInstance().SetCoordinatePosition(Convert.ToDouble(szSplit2[1]), Convert.ToDouble(szSplit2[2]), Convert.ToDouble(szSplit2[4]), 1);
                bRultSnap[1] = CamSnap(Vision_Step.T1_2, 1, 1);
                //bRultSnap[1] = CamSnap((int)Vision_Step.T1_2, 1, 2);
                if (false == bRultSnap[0] && false == bRultSnap[1])
                {
                    m_tcpRobotComm.WriteLine("DropFerrite,0,0,0,0,0,0");
                    wait_recevie_cmd(m_tcpRobotComm, "DropFerrite_Ok\r\n", -1);
                    取Ferrite料();//取Ferrite料
                    goto TrySnap;
                }
                m_tcpRobotComm.WriteLine("PreFetch,0,0,0,0,0,0");
                wait_recevie_cmd(m_tcpRobotComm, "PreFetch_OK\r\n", -1);   
            }
            //GAO
            TimeSpan CTTime1 = DateTime.Now - BeginTime;
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.FerriteCT1, CTTime1.TotalSeconds);

          //  BeginTime = DateTime.Now;
            if (StationMgr.GetInstance().GetStation("转盘站").StationEnable)//转盘站是否禁用
            {
                WaitRegBit((int)SysBitReg.Bit_转盘站通知Ferrite上下料站_OK, true, -1);
            }
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知Ferrite上下料站_OK, false, false);

            BeginTime = DateTime.Now;//GAO
            SpaceInfo infoSpace = ShareInfoSpace.GetSpaceInfo(1); // ShareInfoSpace这个类里边的这个方法
            ShareInfoSpace.SetSpaceInfobRult(false, 1, 1);
            ShareInfoSpace.SetSpaceInfobRult(false, 1, 2);
            ShareInfoSpace.SetSpaceInfoUnloadbRult(false, 1);

            bUnLoad = infoSpace.m_bUnLoad;
            载具两真空吸破真空(ref infoSpace);
            //if(false==infoSpace.m_bRult[0] || false == infoSpace.m_bRult[1])
            //{
            //    S
            //}
            if (bClearTable)
            {
                bClearTable = false;
                if (bUnLoad)  //真值为下料
                {
                    double zOffset = SystemMgr.GetInstance().GetParamDouble("FerriteRobotFetchOffsetZ");
                    double zUnLoadOffset = SystemMgr.GetInstance().GetParamDouble("FerriteRobotUnLoadOffsetZ");
                    WaitRegBit((int)SysBitReg.Bit_下成品料站通知FerriteLoad下料_Ready, true, -1);
                    // szBuff = String.Format("RotateClear,{0},{1:F3},{2:F3},0,0,0", nIndexUnLoad, zOffset, zUnLoadOffset);
                    bool bwarning = false;
                    szBuff = String.Format("FetchProduct,{0:F3},0,0,0,0,0", SystemMgr.GetInstance().GetParamDouble("FerriteRobotFetchOffsetZ"));
                    m_tcpRobotComm.WriteLine(szBuff);
                    // wait_recevie_cmd(m_tcpRobotComm, "RotateClear_Ok\r\n", -1);    //清料模式中机械手最终停于下相机拍照处
                    while (true)
                    {
                        CheckContinue(false);
                        if (m_tcpRobotComm.ReadLine(out szBuff) > 0)
                        {
                            szBuff = szBuff.Trim();
                            string[] szSplit = szBuff.Split(',');
                            if (0 == szSplit[0].CompareTo("Fecth_Ok"))
                            {
                                break;
                            }

                            if (0 == szSplit[0].CompareTo("Fecth_Fail"))
                            {
                                bwarning = true;
                                break;
                            }
                        }
                        WaitTimeDelay(50);
                    }
                    if (bwarning)
                    {
                        StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Unload_Info, "吸取成品料失败，请清理,先点击暂停，注意安全！");
                        bwarning = false;
                    }

                    if (!bModeProduct)
                    {
                        载具1穴真空吸吸真空(ref infoSpace);
                        载具2穴真空吸吸真空(ref infoSpace);
                        载具孔真空吸吸真空(ref infoSpace);
                        WaitTimeDelay(20);
                        if (IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[0]) || IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[1]))
                        {
                            IoMgr.GetInstance().AlarmLight(LightState.黄灯开, 0);
                            IoMgr.GetInstance().AlarmLight(LightState.蜂鸣闪, 5000);
                            MessageBox.Show("转盘载具仍残留物料，请点击暂停后手动清除！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            IoMgr.GetInstance().AlarmLight(LightState.蜂鸣关);
                            IoMgr.GetInstance().AlarmLight(LightState.绿灯开);
                        }
                        载具两真空吸破真空(ref infoSpace);
                    }

                    szBuff = String.Format("RotateClear,{0},{1:F3},{2:F3},0,0,0", nIndexUnLoad, zOffset, zUnLoadOffset);
                    m_tcpRobotComm.WriteLine(szBuff);
                    wait_recevie_cmd(m_tcpRobotComm, "RotateClear_Ok\r\n", -1);
                    SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_Ferrite上下料站_Ready), true, false);
                    nIndexUnLoad = nIndexUnLoad + 1;
                    if (7 == nIndexUnLoad)
                    {
                        nIndexUnLoad = 1;
                        SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_下成品料站通知FerriteLoad下料_Ready, false, false);
                    }
                }
                else
                {
                    SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_Ferrite上下料站_Ready), true, false);
                }
                bFetchOk = true;
                goto label_Over;
            }

            bool bWarring = false;
           // szBuff = String.Format("FetchProduct,{0:3F},0,0,0,0,0", SystemMgr.GetInstance().GetParamDouble("FerriteRobotAssemOffsetZ"));
            szBuff = String.Format("FetchProduct,{0:F3},0,0,0,0,0", SystemMgr.GetInstance().GetParamDouble("FerriteRobotFetchOffsetZ"));
            m_tcpRobotComm.WriteLine(szBuff);    //取成品之后机械手停在取料点上方10mm处
            while (true)
            {
                CheckContinue(false);
                if (m_tcpRobotComm.ReadLine(out szBuff) > 0)
                {
                    szBuff = szBuff.Trim();
                    string[] szSplit = szBuff.Split(',');
                    if (0 == szSplit[0].CompareTo("Fecth_Ok"))
                    {
                        break;
                    }
                        
                    if (0 == szSplit[0].CompareTo("Fecth_Fail"))
                    {
                        bWarring = true;
                        break;
                    }
                }
                WaitTimeDelay(50);
            }
            if(bUnLoad)
            {
                if (bWarring)
                {
                    StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Unload_Info, "吸取成品料失败，请清理,先点击暂停，注意安全！");
                    bWarring = false;
                }
            }

            载具1穴真空吸吸真空(ref infoSpace);
            载具2穴真空吸吸真空(ref infoSpace);
            载具孔真空吸吸真空(ref infoSpace);

            if (!bModeProduct)
            {
                if (IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[0])|| IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[1]))
                {
                    IoMgr.GetInstance().AlarmLight(LightState.黄灯开, 0);
                    IoMgr.GetInstance().AlarmLight(LightState.蜂鸣闪, 5000);
                    MessageBox.Show("转盘载具仍残留物料，请点击暂停后手动清除！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    IoMgr.GetInstance().AlarmLight(LightState.蜂鸣关);
                    IoMgr.GetInstance().AlarmLight(LightState.绿灯开);
                }              
            }

            if (bRultSnap[0])
            {
                //bRultSnap[0] = false;
                double xOffset = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_T1_X);
                double yOffset = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_T1_Y);
                double uOffset = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_T1_U);
                double zOffset = SystemMgr.GetInstance().GetParamDouble("FerriteRobotAssemOffsetZ");
                // ShareInfoSpace.SetInfoCode((int)StationIndex.Feerite上下料站, nIndexLoad, StrChangeTrigge.GetInstance().GetRegStr((int)SysStrReg.Str_Code_Info));
                szBuff = String.Format("Assemble,{0},{1:F3},{2:F3},{3:F3},{4:F3},{5}", 1, xOffset, yOffset, zOffset, uOffset, nFetchModeProduct);//nIndexLoad:转盘的1、2、3、4
                
                m_tcpRobotComm.WriteLine(szBuff);
                nFetchModeProduct = 0;
                wait_recevie_cmd(m_tcpRobotComm, "Assemble_Ok\r\n", -1);
                ShareInfoSpace.SetSpaceInfobRult(true, 1, 1);
                
                if (!SystemMgr.GetInstance().GetParamBool("ProductOrTest"))
                {
                  //  载具1穴真空吸吸真空(ref infoSpace);
                    //WaitIo(infoSpace.szIoInDetail[0], true, 2000);
                    等待IO信号(infoSpace.szIoInDetail[0], true, 2000);
                }                   
            }
            if(bRultSnap[1])
            {
                double xOffset = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_T12_X);
                double yOffset = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_T12_Y);
                double uOffset = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_T12_U);
                double zOffset = SystemMgr.GetInstance().GetParamDouble("FerriteRobotAssemOffsetZ");
                //ShareInfoSpace.SetInfoCode((int)StationIndex.Feerite上下料站, nIndexLoad, StrChangeTrigge.GetInstance().GetRegStr((int)SysStrReg.Str_Code_Info));
                szBuff = String.Format("Assemble,{0},{1:F3},{2:F3},{3:F3},{4:F3},{5}", 2, xOffset, yOffset, zOffset, uOffset,nFetchModeProduct);//nIndexLoad:转盘的1、2、3、4               
                m_tcpRobotComm.WriteLine(szBuff);
                wait_recevie_cmd(m_tcpRobotComm, "Assemble_Ok\r\n", -1);
                ShareInfoSpace.SetSpaceInfobRult(true, 1, 2);
                if (0 == SystemMgr.GetInstance().GetParamInt("BanCavity"))
                {
                    ShareInfoSpace.SetCavityInfo(true, 1, 1);
                    ShareInfoSpace.SetCavityInfo(true, 1, 2);
                }
                else if (1 == SystemMgr.GetInstance().GetParamInt("BanCavity"))
                {
                    ShareInfoSpace.SetCavityInfo(false, 1, 1);
                    ShareInfoSpace.SetCavityInfo(true, 1, 2);
                }
                else
                {
                    ShareInfoSpace.SetCavityInfo(true, 1, 1);
                    ShareInfoSpace.SetCavityInfo(false, 1, 2);
                }
                if (!SystemMgr.GetInstance().GetParamBool("ProductOrTest"))
                {
                  //  载具2穴真空吸吸真空(ref infoSpace);
                    //WaitIo(infoSpace.szIoInDetail[1], true, 2000);
                    等待IO信号(infoSpace.szIoInDetail[1],true,2000);
                }
            }
            nFetchModeProduct = 1;
            ShareInfoSpace.SetSpaceInfoUnloadbRult(true, 1);//此工位的四个穴位放完，通知下一站此工位可以做
            //载具孔真空吸吸真空(ref infoSpace);
            //WaitTimeDelay(200);
            SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_Ferrite上下料站_Ready), true, false);
            if (bUnLoad)//判断此站是否下料，第一次不需要下料
            {
                bool bwarning = false;
                bUnLoad = false;
                WaitRegBit((int)SysBitReg.Bit_下成品料站通知FerriteLoad下料_Ready, true, -1);
                szBuff = String.Format("Unload,{0:F3},{1},{2},{3},0,0", SystemMgr.GetInstance().GetParamDouble("FerriteRobotUnLoadOffsetZ"), nIndexUnLoad,0,SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_Coil和Ferrite成品上下料盘));
                m_tcpRobotComm.WriteLine(szBuff);   //普通下料是从rotate2点去下料托盘处
                                                    //ShareInfoSpace.SetSpaceInfobRult(false, 1);
                                                    //wait_recevie_cmd(m_tcpRobotComm, "Unload_Ok\r\n", -1);

                while (true)
                {
                    CheckContinue(false);
                    if (m_tcpRobotComm.ReadLine(out szBuff) > 0)
                    {
                        szBuff = szBuff.Trim();
                        string[] szSplit = szBuff.Split(',');
                        if (0 == szSplit[0].CompareTo("Unload_Ok"))
                        {
                            break;
                        }

                        if (0 == szSplit[0].CompareTo("Unload_Fail"))
                        {
                            bwarning = true;
                            break;
                        }
                    }
                    WaitTimeDelay(50);
                }

                if (bwarning)
                {
                    bwarning = false;
                    StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Unload_Info, "Ferrite机械手吸嘴粘料，请点击暂停后手动拿走！！！");
                }

                nIndexUnLoad = nIndexUnLoad + 1;
                if (7 == nIndexUnLoad)
                {
                    nIndexUnLoad = 1;
                    SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_下成品料站通知FerriteLoad下料_Ready, false, false);
                }
            }
            else
            {
                szBuff = String.Format("Unload,{0:F3},{1},{2},{3},0,0", SystemMgr.GetInstance().GetParamDouble("FerriteRobotUnLoadOffsetZ"), nIndexUnLoad,1,SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_Coil和Ferrite成品上下料盘));
                m_tcpRobotComm.WriteLine(szBuff);
                wait_recevie_cmd(m_tcpRobotComm, "Unload_Ok\r\n", -1);
            }
            if (false == bRultSnap[0] || false == bRultSnap[1])
            {
                bRultSnap[0] = false;
                bRultSnap[1] = false;
                m_tcpRobotComm.WriteLine("DropFerrite,0,0,0,0,0,0");
                wait_recevie_cmd(m_tcpRobotComm, "DropFerrite_Ok\r\n", -1);
                //goto Try_Fetch_Doing;
            }
            label_Over:
            TimeSpan CTTime = DateTime.Now - BeginTime;
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.FerriteCT, CTTime.TotalSeconds);

            ;
        }

        public void 取Ferrite料()
        {
            string szBuff = "";
            WaitRegBit((int)SysBitReg.Bit_供料Tray盘站通知Ferrite机器人取料_Ok, true, -1);
            nSpeedPerce = SystemMgr.GetInstance().GetParamInt("SpeedRobotFerrite");
            szBuff = String.Format("Load,{0},{1:F3},{2},0,0,0", nIndexFecth, SystemMgr.GetInstance().GetParamDouble("FerriteRobotFechtOffsetZ"), nSpeedPerce);
            m_tcpRobotComm.WriteLine(szBuff);
            while (true)
            {
                CheckContinue(false);
                if (m_tcpRobotComm.ReadLine(out szBuff) > 0)
                {
                    szBuff = szBuff.Trim();
                    string[] szSplit = szBuff.Split(',');
                    if (0 == szSplit[0].CompareTo("Load"))
                        break;
                }
                WaitTimeDelay(50);
            }
            nIndexFecth = nIndexFecth + 1;
            if (9 == nIndexFecth)
            {
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_料盘空_Empy, true, false);
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_供料Tray盘站通知Ferrite机器人取料_Ok, false, false);
                nIndexFecth = 1;
            }

            string[] szSplit1 = szBuff.Split(',');
            //if (1 == Convert.ToInt32(szSplit1[4]))
            //    goto Try_Fetch_Doing;
            CongexVision.GetInstance().SetCoordinatePosition(Convert.ToDouble(szSplit1[1]), Convert.ToDouble(szSplit1[2]), Convert.ToDouble(szSplit1[4]), 0);
        }

        public void 载具两真空吸破真空(ref SpaceInfo infoSpace)
        {
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[0], false);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[1], true);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[2], false);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[3], true);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[4], false);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[5], true);
            WaitTimeDelay(200);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[1], false);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[3], false);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[5], false);
        }

        public void 载具1穴真空吸吸真空(ref SpaceInfo infoSpace)
        {
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[1], false);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[0], true);
        }
        public void 载具2穴真空吸吸真空(ref SpaceInfo infoSpace)
        {
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[3], false);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[2], true);
        }
        public void 载具孔真空吸吸真空(ref SpaceInfo infoSpace)
        {
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[5], false);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[4], true);
        }
        public void 载具两真空吸吸真空(ref SpaceInfo infoSpace)
        {
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[1], false);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[0], true);

            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[3], false);
            IoMgr.GetInstance().WriteIoBit(infoSpace.szIoOutDetail[2], true);
        }

        /// <summary>
        /// 标定流程
        /// </summary>
        public void CalibProcess(int nIndexCalib)
        {
            string str = "";
            string szBuffer = "";
            if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_T1_Apply,1,nIndexCalib))
            {
                goto lable_Calib_Over;
            }
            
            str = String.Format("CalibT1Start,{0},0,0,0,0,0", nIndexCalib);
            m_tcpRobotComm.WriteLine(str);
            wait_recevie_cmd(m_tcpRobotComm, "CalibT1Start_Ok\r\n", -1);
            StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Info, "确定开始标定");
            if (false == SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_Str_Erro_Result))
            {
                goto lable_Calib_Over;
            }
            for (int nNum = 1; nNum < 12; nNum++)
            {
                str = String.Format("Cali_T1,{0},{1},0,0,0,0", nNum, nIndexCalib);
                m_tcpRobotComm.WriteLine(str);
                while (true)
                {
                    CheckContinue(false);
                    if (m_tcpRobotComm.ReadLine(out szBuffer) > 0)
                    {
                        break;
                    }
                    WaitTimeDelay(50);
                }
                szBuffer = szBuffer.Trim();
                string[] szSplit = szBuffer.Split(',');
                CongexVision.GetInstance().SetCoordinatePosition(Convert.ToDouble(szSplit[1]), Convert.ToDouble(szSplit[2]), Convert.ToDouble(szSplit[4]),nIndexCalib);
                if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_T1,1,nIndexCalib))
                {
                    goto lable_Calib_Over;
                }
            }
            if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_End))
            {
                goto lable_Calib_Over;
            }
        lable_Calib_Over:
            m_tcpRobotComm.WriteLine("MoveToSafe,0,0,0,0,0,0");
            wait_recevie_cmd(m_tcpRobotComm, "MoveToSafe_Ok\r\n", -1);
            ;
        }
        public bool 等待IO信号(string strInName,bool bSignal,int nTimeOver)
        {
            bool bRult = false;
            int nTime = 0;
            while (true)
            {
                if (bSignal==IoMgr.GetInstance().ReadIoInBit(strInName))
                {
                    bRult = true;
                    break;
                }
                if(nTime> nTimeOver)
                {
                    break;
                }
                WaitTimeDelay(50);
                nTime = nTime + 50;  
            }
            return bRult;
        }
        public void RobotSevorOn()
        {
            IoMgr.GetInstance().WriteIoBit("FtDO-Stop", true);
            WaitTimeDelay(400);
            IoMgr.GetInstance().WriteIoBit("FtDO-Stop", false);
            WaitTimeDelay(400);
           // WaitIo("FtDI-Stop",true,-1);
            IoMgr.GetInstance().WriteIoBit("FtDO-Reset", true);
            WaitTimeDelay(400);
            IoMgr.GetInstance().WriteIoBit("FtDO-Reset", false);
            WaitTimeDelay(400);
            IoMgr.GetInstance().WriteIoBit("FtDO-Start", true);
            WaitTimeDelay(400);
            IoMgr.GetInstance().WriteIoBit("FtDO-Start", false);
            WaitTimeDelay(400);
        }

        public void CalibTrain(int nStep)
        {
            string str = "";
            string szBuff = "";
            str = String.Format("Train,{0},{1:F3},0,0,0,0", nStep,SystemMgr.GetInstance().GetParamDouble("FerriteRobotAssemOffsetZ"));
            m_tcpRobotComm.WriteLine(str);
            while (true)
            {
                CheckContinue(false);
                if (m_tcpRobotComm.ReadLine(out szBuff) > 0)
                {
                    szBuff = szBuff.Trim(); 
                    string[] szSplit = szBuff.Split(',');
                    if (0 == szSplit[0].CompareTo("Train"))
                        break;
                }
                WaitTimeDelay(50);
            }
            string[] szSplit1 = szBuff.Split(',');
            CongexVision.GetInstance().SetCoordinatePosition(Convert.ToDouble(szSplit1[1]), Convert.ToDouble(szSplit1[2]), Convert.ToDouble(szSplit1[3]),
                                                            Convert.ToDouble(szSplit1[4]), Convert.ToDouble(szSplit1[5]), Convert.ToDouble(szSplit1[6]));

            switch(nStep)
            {
                case 2:
                    CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_Train1);
                    break;
                case 3:
                    CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_Train2);
                    break;
                //case 3:
                //    CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_Train3);
                //    break;
                //case 4:
                //    CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_Train4);
                //    break;
            }
            m_tcpRobotComm.WriteLine("MoveToSafe,0,0,0,0,0,0");
            wait_recevie_cmd(m_tcpRobotComm, "MoveToSafe_Ok\r\n", -1);
        }

        public bool CamSnap(Vision_Step nStep, int nStaion, int nCub)
        {
            bool bRult = false;
            int nNumSnap = 0;
         TrySnap:
            if (!CongexVision.GetInstance().ProcessStep(nStep, nStaion))
            {
                CheckContinue(false);
                if (nNumSnap < 2)
                {
                    nNumSnap++;
                    goto TrySnap;
                }
                else
                {
                    if (SystemMgr.GetInstance().GetParamBool("CognexCheck"))//是否启用康耐视监控
                    {
                        if (Vision_Step.T1_1 == nStep)
                        {
                            StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Info, "T1_1连续三次拍照处理失败，是否重新拍？");//T2相机连续三次图像处理失败，会弹出一个提示窗体
                        }
                        else if (Vision_Step.T1_2 == nStep)
                        {
                            StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Info, "T1_2连续三次拍照处理失败，是否重新拍？");//T3相机连续三次图像处理失败，会弹出一个提示窗体
                        }
                        else
                        {
                            //StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Info, "T3_2连续三次拍照处理失败，是否重新拍？");//T3相机连续三次图像处理失败，会弹出一个提示窗体
                        }
                        if (SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_Str_Erro_Result))//T3相机连续三次图像处理失败，判断是否再次进行拍照
                        {
                            nNumSnap = 0;
                            goto TrySnap;
                        }
                    }
                    //else
                    //{
                    //    bSnapOk[nCub-1] = false;
                    //}
                }
            }
            else
            {
                bRult = true;
                //bSnapOk[nCub - 1] = true;
                nNumSnap = 0;
            }
            return bRult;
        }
        public override void StationDeinit()
        {
            base.StationDeinit();
        }
        public override void EmgStop()
        {
            Robot_ABB.Robot_Stop((int)controller_name.Load);
            base.EmgStop();
        }
    }
}
