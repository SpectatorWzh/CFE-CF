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
    class StationAssmbly : StationBase
    {
        enum POINT
        {
            MARK = 1,
        }

        public int T4SnapNum;
        public int T3SnapNum;
        public int nMark;
        public int nCalibInt;
        public int nIndexFetch;
        public int nIndexLoad;
        public int nNextFerrite;
        public int nSpeedPerce;
        public bool bModeProduct;
        public bool bFetchCoiloOk;
        public bool bTwoAssm;
        public bool m_bReady;
        public bool[] bSnapOk;
        double fXoffset;
        double fYoffset;
        double fZoffset;
        double fZDownoffset;
        public TcpLink m_tcpRobotComm;
        public StationAssmbly(string strName) : base(strName)
        {
            nIndexFetch = 1;
            nIndexLoad = 1;
            bFetchCoiloOk = false;
            io_in = new string[] { "2.13", "2.14", "2.15", "2.16" };
            io_out = new string[] { "2.16", "2.17", "2.18", "2.19", "2.20" };
            bSnapOk = new bool[] { false, false };
            m_tcpRobotComm = TcpMgr.GetInstance().GetTcpLink(2);
        }

        public override void InitSecurityState()
        {
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_组装站_Ready, false, false);
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.Double_Rult_X, 0, false);
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.Double_Rult_Y, 0, false);
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.Double_Rult_U, 0, false);

            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.Double_Offset_X, 0.000, false);
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.Double_Offset_Y, 0.000, false);
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.Double_Offset_U, 0.000, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_上传数据OK, false, false);
            base.InitSecurityState();
        }
   
        public override void StationInit()
        {
            T4SnapNum = 0;
            T3SnapNum = 0;
            nIndexFetch = 1;
            nIndexLoad = 1;
            bFetchCoiloOk = false;
            bTwoAssm = false;
            int nCommNum = 0;
            nNextFerrite = 0;
            fZoffset = 0;
            CongexVision.GetInstance().CongexVisionInit();
            bModeProduct = SystemMgr.GetInstance().GetParamBool("ProductOrTest");
            nCalibInt=SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_Calib_Index);
            nMark = SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_ChooseMark);

            WaitTimeDelay(200);
            WaitTimeDelay(300);
        LineRobot:
            nCommNum++;
           // RobotSevorOn();
            ShowLog("打开Coil组装机器人通讯端");
            if (m_tcpRobotComm.IsOpen())
                m_tcpRobotComm.Close();

            try
            {
                m_tcpRobotComm.Open();
                //wait_recevie_cmd(m_tcpRobotComm, "Ready\r\n", 10000);
                string szBuffer;
                if (bModeProduct)
                {
                    szBuffer = "Init,1,0,0,0,0,0,0";
                }
                else
                {
                    szBuffer = "Init,0,0,0,0,0,0,0";
                }
                m_tcpRobotComm.WriteLine(szBuffer);
                wait_recevie_cmd(m_tcpRobotComm, "InitOk\r\n", 15000);
            }
            catch (Exception)
            {
                if (nCommNum < 5)
                    goto LineRobot;
                else
                    WarningMgr.GetInstance().Error("组装机器人启动失败");
            }
            SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_组装站_Ready), true, false);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_组装机械手通知Coil上料站OK, true, false);
            ShowLog("初始化完成");
        }

        public override void StationProcess()
        {
            DateTime CTBegin = DateTime.Now;
            while(true)
            {
                CheckContinue(false);
                if (StationMgr.GetInstance().BAutoMode)
                    break;
                if (IoMgr.GetInstance().ReadIoInBit("启动"))
                    break;
                WaitTimeDelay(50);
            }
            if (2 == nCalibInt)
            {
                CalibProcess();
                goto label_Over;
            }
            if (1 == nMark || 2 == nMark)
            {
                MarkProcess();
                goto label_Over;
            }
            if(3 == nMark || 4 == nMark)
            {
                WaitTimeDelay(50);
                goto label_Over;
            }
            string szBuffer = "";
            SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_Process_Step, 0, true);
            nSpeedPerce = SystemMgr.GetInstance().GetParamInt("SpeedRobotAssembly");
            if (bFetchCoiloOk)//当转盘工位第四个穴位拍照失败，无法组装Coil，机器人上有片料，就不需要吸料了
            {
                bFetchCoiloOk = false;
                goto label_FetchOk;
            }
            从Coil料盘中取Coil();
            //GAO
            TimeSpan CTTime1 = DateTime.Now - CTBegin;
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.AssmCT1, CTTime1.TotalSeconds);

            label_FetchOk:
           // CTBegin = DateTime.Now;
            if (StationMgr.GetInstance().GetStation("转盘站").StationEnable)
            {
                WaitRegBit((int)SysBitReg.Bit_转盘站通知组装站_OK, true, -1);
            }
            CTBegin = DateTime.Now;
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知组装站_OK, false, false);
            SpaceInfo infoSpac = ShareInfoSpace.GetSpaceInfo((int)StationIndex.Coil组装站);
            SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_工位_Number, infoSpac.nCub);
            if (!ShareInfoSpace.GetSpaceInfobRult(3)||SystemMgr.GetInstance().GetParamBool("StationAssmblyEnable"))//判断此工位是否组装Coil
            {
                infoSpac.m_bRult[0] = false;
                infoSpac.m_bRult[1] = false;   //防止只取到一片料时保压依旧下压（peel里边一个为TRUE，一个为FALSE）
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_组装站_Ready, true, false);
                bFetchCoiloOk = true;
                goto label_Over;
            }
            int nNumSnap = 0;
            //if(!SystemMgr.GetInstance().GetParamBool("ProductOrTest"))
            //{
            //    IoMgr.GetInstance().WriteIoBit("日光灯", false);
            //}  
            if(infoSpac.m_bBanCavity[0])
            {
                if (infoSpac.m_bRult[0])
                {
                    StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_FerriteCodeFirst_Info, infoSpac.strCode[0], false);
                    //T3CamSnap:

                    bSnapOk[0] = CamSnap(Vision_Step.T3_1, infoSpac.nCub, 1);

                    //if (!SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_T3处理结果))     //GAO
                    //{                                          //0为无料，1为成功，2为有料拍失败   1,2为TRUE
                    //    CheckContinue(false);
                    //    if (T3SnapNum < 2)
                    //    {
                    //        T3SnapNum++;
                    //        goto T3CamSnap;
                    //    }
                    //}
                    //T3SnapNum = 0;

                    if (!bSnapOk[0])
                    {
                        ShareInfoSpace.SetSpaceInfobRult(false, 3, 1);
                    }
                }
                else
                {
                    bSnapOk[0] = false;
                }
            }
            else
            {
                ShareInfoSpace.SetSpaceInfobRult(false, 3, 1);
                bSnapOk[0] = false;
            }
            


            if(infoSpac.m_bBanCavity[1])
            {
                if (infoSpac.m_bRult[1])
                {
                    StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_FerriteCodeSecond_Info, infoSpac.strCode[1], false);
                    //T4CamSnap:
                    bSnapOk[1] = CamSnap(Vision_Step.T3_2, infoSpac.nCub, 2);

                    //if (!SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_T4处理结果))
                    //{
                    //    CheckContinue(false);
                    //    if (T4SnapNum < 2)
                    //    {
                    //        T4SnapNum++;
                    //        goto T4CamSnap;
                    //    }
                    //}
                    //T4SnapNum = 0;

                    if (!bSnapOk[1])
                    {
                        ShareInfoSpace.SetSpaceInfobRult(false, 3, 2);
                    }
                }
                else
                {
                    bSnapOk[1] = false;
                }
            }
            else
            {
                ShareInfoSpace.SetSpaceInfobRult(false, 3, 2);
                bSnapOk[1] = false;
            }
           
            SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_Process_Step, 20, true);
            //俩个均无料NG 00 01 02 10 11 12 20 21 22
            //1个均无料NG，
            //俩个均无料NG
            //俩个均无料NG
            //俩个均无料NG
            if (false==bSnapOk[0] && false==bSnapOk[1])
            {
                bFetchCoiloOk = true;
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_组装站_Ready, true, false);
                goto label_Over;
            } 
            //有料NG
            else
            {
                if(bSnapOk[0] && bSnapOk[1])
                {
                    bTwoAssm = true;
                    nIndexLoad = 1;
                }
                else
                {
                    bTwoAssm = false;
                    if (bSnapOk[0])
                    {
                        nIndexLoad = 1;
                    }
                    else
                    {
                        nIndexLoad = 2;
                    }
                }
            }
        TrySnapT2:
            WaitTimeDelay(200);
            if (!CongexVision.GetInstance().ProcessStep(Vision_Step.T2_1,infoSpac.nCub,nIndexLoad))
            {
                CheckContinue(false);
                if (nNumSnap < 2)
                {
                    nNumSnap++;
                    goto TrySnapT2;
                }
                else
                {
                    if (SystemMgr.GetInstance().GetParamBool("CognexCheck"))//是否启用康耐视监控
                    {
                        StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Info, "T2_1连续三次拍照处理失败，是否重新拍？");
                        if (SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_Str_Erro_Result))
                        {
                            nNumSnap = 0;
                            goto TrySnapT2;
                        }
                        else
                        {
                            nNumSnap = 0;
                        }
                    }
                    m_tcpRobotComm.WriteLine("DropCoil,0,0,0,0,0,0,0");
                    wait_recevie_cmd(m_tcpRobotComm, "DropCoil_OK\r\n", -1);
                    从Coil料盘中取Coil();
                    goto TrySnapT2;
                }
            }
            else
            {
                nNumSnap = 0;
            }
            SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_Process_Step, 60, true);
            double xOffset;
            double yOffset;
            if (Math.Abs(SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Offset_X))<=0.1)
            {
                 xOffset = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Rult_X) - SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Offset_X);
            }
            else
            {
                 xOffset = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Rult_X);
            }

            if (Math.Abs(SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Offset_Y)) <= 0.1)
            {
                 yOffset = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Rult_Y) - SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Offset_Y);
            }
            else
            {
                 yOffset = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Rult_Y);
            }
       
            double zOffset = SystemMgr.GetInstance().GetParamDouble("CoilRobotAssemOffsetZ");
            double aOffset = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Rult_U)- SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Offset_U);

            
            if(1== nIndexLoad)
            {
                if(SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_T3处理结果))
                {
                    szBuffer = String.Format("Assemble,{0},{1:F3},{2:F3},{3:F3},{4:F3},0,0", nIndexLoad, xOffset, yOffset, zOffset, aOffset);
                }
                else
                {
                    szBuffer = String.Format("Assemble,{0},0,0,0,0,1,0", nIndexLoad);
                }
            }
            else
            {
                if (SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_T4处理结果))
                {
                    szBuffer = String.Format("Assemble,{0},{1:F3},{2:F3},{3:F3},{4:F3},0,0", nIndexLoad, xOffset, yOffset, zOffset, aOffset);
                }
                else
                {
                    szBuffer = String.Format("Assemble,{0},0,0,0,0,1,0", nIndexLoad);
                }
            }
            
            m_tcpRobotComm.WriteLine(szBuffer);
            //link开始
            SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_穴位_Number, nIndexLoad);
            
            if (!SystemMgr.GetInstance().GetParamBool("ProductOrTest"))
            {
                if (SystemMgr.GetInstance().GetParamBool("UpLoadTrueOrFalse"))   //判断是否LINK且上传二维码数据
                {
                    string strInfoCode = "", strMsg = "";
                    StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_FerriteCodeVision_Info, infoSpac.strCode[nIndexLoad - 1]);

                    SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_组装点保存, true);
                    for (int i = 0; i < 3 && strInfoCode != "0" && strInfoCode != "3"; i++)
                    {
                        ShareInfoSpace.CoilLink(out strInfoCode, out strMsg, StrChangeTrigge.GetInstance().GetRegStr((int)SysStrReg.Str_CoilCode_Info), SystemMgr.GetInstance().GetParamString("WorkOrder"), SystemMgr.GetInstance().GetParamString("LineName"), infoSpac.strCode[nIndexLoad - 1]);

                        StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_LinkCode_Save, strInfoCode); //本地保存用Link返回代码与信息
                        StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_LinkMsg_Save, strMsg);
                        StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Upload_Msg, strMsg);
                        StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Upload_Code, strInfoCode);
                    }

                    if (SystemMgr.GetInstance().GetParamBool("UpLoadData"))
                    {
                        SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_Assem_Finished, true);
                        ShowLog("等待上次数据上传完毕");
                        WaitRegBit((int)SysBitReg.Bit_上传数据OK, true);
                        SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_上传数据OK, false, false);
                    }
                    else
                    {
                        SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_Assem_Finished, true, false);
                    }

                    if (strInfoCode != "0" && strInfoCode != "3" && strInfoCode != "1")
                    {
                        MessageBox.Show("Coil Link失败，请检测网络是否连接，按启动键继续");
                        WaitIo("启动", true);
                    }
                    else
                    {
                        if (strInfoCode != "0")
                        {
                            IoMgr.GetInstance().AlarmLight(LightState.黄灯开, 0);
                            IoMgr.GetInstance().AlarmLight(LightState.蜂鸣闪, 5000);
                            MessageBox.Show("Link失败，请确认Ferrite与Coil是否正确！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            IoMgr.GetInstance().AlarmLight(LightState.蜂鸣关);
                            IoMgr.GetInstance().AlarmLight(LightState.绿灯开);
                        }
                    }
                }
            }

            //link结束

            while (true)
            {
                CheckContinue(false);
                if (m_tcpRobotComm.ReadLine(out szBuffer) > 0)
                {
                    szBuffer = szBuffer.Trim();
                    break;
                }
                WaitTimeDelay(50);
            }
            if (0==szBuffer.CompareTo("Assemble_Ok"))
            {
                //ShareInfoSpace.SetSpaceInfobRult(true, 3, 1);     
            }
            else
            {
                //ShareInfoSpace.SetSpaceInfobRult(false, 1, 1);
                m_tcpRobotComm.WriteLine("DropCoil,0,0,0,0,0,0,0");
                wait_recevie_cmd(m_tcpRobotComm, "DropCoil_OK\r\n", -1);
            }
            //m_tcpRobotComm.WriteLine(szBuffer);
            //wait_recevie_cmd(m_tcpRobotComm, "Assemble_Ok\r\n",-1);
            if(bTwoAssm)
            {
                bTwoAssm = false;
                nIndexLoad = nIndexLoad + 1;
                从Coil料盘中取Coil();
                goto TrySnapT2;
            }
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_组装站_Ready, true, false);
            SystemMgr.GetInstance().WriteRegInt((int)SysIntReg.Int_Process_Step, 100, true);
            label_Over:
            TimeSpan CTTime = DateTime.Now - CTBegin;
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.AssmCT, CTTime.TotalSeconds);

            ;
        }

        /// <summary>
        /// 机器人启动
        /// </summary>
        public void RobotSevorOn()
        {
            IoMgr.GetInstance().WriteIoBit("CiDO-Stop", true);
            WaitTimeDelay(400);
            IoMgr.GetInstance().WriteIoBit("CiDO-Stop", false);
            WaitTimeDelay(400);
            IoMgr.GetInstance().WriteIoBit("CiDO-Reset", true);
            WaitTimeDelay(400);
            IoMgr.GetInstance().WriteIoBit("CiDO-Reset", false);
            IoMgr.GetInstance().WriteIoBit("CiDO-Start", true);
            WaitTimeDelay(400);
            IoMgr.GetInstance().WriteIoBit("CiDO-Start", false);
            WaitTimeDelay(400);
        }

        public void MarkProcess()
        {
            string szBuffer = "";
            int nIndex = SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_ChooseMark);   //1为检测，2为训练
            int nSpeed = SystemMgr.GetInstance().GetParamInt("SpeedRobotAssembly");
            int Point = SystemMgr.GetInstance().GetParamInt("GetPoint");
            if(Point<1||Point>12)
            {
                MessageBox.Show("取料点号设置错误！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
                goto lable_Mark_Over;
            }
            switch(nIndex)
            {
                case 1:
                    for(int i=1;i<13; i++)
                    {
                        int nSpeed1 = SystemMgr.GetInstance().GetParamInt("SpeedRobotAssembly");
                        if (SystemMgr.GetInstance().GetParamBool("OneOrSome"))
                        {
                            string szBufferMark = String.Format("GetCoil,{0},0,{1},0,0,0,0", nSpeed1, i);
                            m_tcpRobotComm.WriteLine(szBufferMark);
                            while (true)
                            {
                                CheckContinue(false);
                                if (m_tcpRobotComm.ReadLine(out szBufferMark) > 0)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            string szBufferMark = String.Format("GetCoil,{0},0,{1},0,0,0,0", nSpeed1,Point);
                            m_tcpRobotComm.WriteLine(szBufferMark);
                            while (true)
                            {
                                CheckContinue(false);
                                if (m_tcpRobotComm.ReadLine(out szBufferMark) > 0)
                                {
                                    break;
                                }
                            }
                        }                                  
                      
                        WaitTimeDelay(2000);

                        CongexVision.GetInstance().ProcessStep(Vision_Step.Mark_Check_T2);
                        double x = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Rult_CheckX);
                        double y = SystemMgr.GetInstance().GetRegDouble((int)SysDoubleReg.Double_Rult_CheckY);
                        if(Math.Abs(x)>SystemMgr.GetInstance().GetParamDouble("CheckValue")||Math.Abs(y)> SystemMgr.GetInstance().GetParamDouble("CheckValue"))
                        {
                            MessageBox.Show("x= " + x + ", y="+ y + "; "+ i + "号取料点检测失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            MessageBox.Show("x= " + x + ", y= " + y + "; " + i + "号取料点检测成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        m_tcpRobotComm.WriteLine("MoveToSafe,0,0,0,0,0,0,0");
                        wait_recevie_cmd(m_tcpRobotComm, "MoveToSafe_Ok\r\n", -1);
                    }
                    goto lable_Mark_Over;
                    break;

                case 2:
                    szBuffer = String.Format("GetCoil,{0},0,3,2,0,0,0", nSpeed);
                    m_tcpRobotComm.WriteLine(szBuffer);
                    while (true)
                    {
                        CheckContinue(false);
                        if (m_tcpRobotComm.ReadLine(out szBuffer) > 0)
                        {
                            break;
                        }
                    }
                    WaitTimeDelay(500);
                    if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Mark_Train_T2))
                    {
                        MessageBox.Show("T2训练失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show("T2训练成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    break;
            }
            m_tcpRobotComm.WriteLine("MoveToSafe,0,0,0,0,0,0,0");
            wait_recevie_cmd(m_tcpRobotComm, "MoveToSafe_Ok\r\n", -1);
            lable_Mark_Over:
            ;
        }

        public void CalibProcess()
        {
            string szBuffer = "";
            //WaitTimeDelay(1000);
            int nIndex = SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_Calib_T2T3_Index);
            int nSpeed = SystemMgr.GetInstance().GetParamInt("SpeedRobotAssembly");
            switch(nIndex)
            {
                case 1:
                    if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_T3T4_Apply))
                    {
                        goto lable_Calib_Over;
                    }
                    break;
                case 2:
                    if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_T3T5_Apply))
                    {
                        goto lable_Calib_Over;
                    }
                    break;
                default:
                    goto lable_Calib_Over;
                    break;
            }
            szBuffer = String.Format("CalibT2T3Start,{0},{1},{2:F3},0,0,0,0", nIndex, nSpeed,SystemMgr.GetInstance().GetParamDouble("CoilRobotAssemOffsetZ"));
            m_tcpRobotComm.WriteLine(szBuffer);
            // wait_recevie_cmd(m_tcpRobotComm, "CalibT2T3Start,0,0,0,0,0", -1);
            while (true)
            {
                CheckContinue(false);
                if (m_tcpRobotComm.ReadLine(out szBuffer) > 0)
                {
                    break;
                }
            }
            szBuffer = szBuffer.Trim();
            string[] szSplit = szBuffer.Split(',');     
            switch(nIndex)
            {
                case 1:
                    CongexVision.GetInstance().SetCoordinatePosition(Convert.ToDouble(szSplit[1]), Convert.ToDouble(szSplit[2]), Convert.ToDouble(szSplit[4]), 3);
                    if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_T3))
                    {
                        goto lable_Calib_Over;
                    }
                    break;
                case 2:
                    CongexVision.GetInstance().SetCoordinatePosition(Convert.ToDouble(szSplit[1]), Convert.ToDouble(szSplit[2]), Convert.ToDouble(szSplit[4]), 4);
                    if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_T4))
                    {
                        goto lable_Calib_Over;
                    }
                    break;
                default:
                    goto lable_Calib_Over;
                    break;
            }
            
            szBuffer = String.Format("Cali_GetBoard,{0},{1:F3},0,0,0,0,0", nIndex,SystemMgr.GetInstance().GetParamDouble("CoilRobotAssemOffsetZ"));
            m_tcpRobotComm.WriteLine(szBuffer);
            wait_recevie_cmd(m_tcpRobotComm, "Cali_SuckOk\r\n", -1); 
            IoMgr.GetInstance().WriteIoBit("工位2真空1真空吸", false);
            IoMgr.GetInstance().WriteIoBit("工位2真空2真空吸", false);
            WaitTimeDelay(1000);
            m_tcpRobotComm.WriteLine("Cali_SuckOk,0,0,0,0,0,0,0");
            wait_recevie_cmd(m_tcpRobotComm, "Cali_GetBoard_OK\r\n", -1);

            for (int nNum = 1; nNum < 12; nNum++)
            {
                szBuffer = String.Format("Cali_Move2CamDownSnap,{0},0,0,0,0,0,0", nNum);
                m_tcpRobotComm.WriteLine(szBuffer);
                while (true)
                {
                    CheckContinue(false);
                    if (m_tcpRobotComm.ReadLine(out szBuffer) > 0)
                    {
                        break;
                    }
                }
                szBuffer = szBuffer.Trim();
                string[] szSplit1 = szBuffer.Split(',');
                if(0!=szSplit1[0].CompareTo("Cali_Move2CamDownSnap"))
                {
                    WarningMgr.GetInstance().Warning("Calib返回错误，需重新标定");
                    goto lable_Calib_Over;
                }
                CongexVision.GetInstance().SetCoordinatePosition(Convert.ToDouble(szSplit1[1]), Convert.ToDouble(szSplit1[2]), Convert.ToDouble(szSplit1[4]),2);
                if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_T2))
                {
                    goto lable_Calib_Over;
                }
            }
            if (!CongexVision.GetInstance().ProcessStep(Vision_Step.Cali_End))
            {
                goto lable_Calib_Over;
            }
        lable_Calib_Over:
            m_tcpRobotComm.WriteLine("MoveToSafe,0,0,0,0,0,0,0");
            wait_recevie_cmd(m_tcpRobotComm, "MoveToSafe_Ok\r\n", -1);
            ;
        }
        public override void StationDeinit()
        {
            base.StationDeinit();
        }

        public void 获取取料序号(out int nIndex,out string strCode, out int nNextIndex, int nCurrIndex)
        {
            ShareInfoSpace.GetCoilIndex(out nIndex, out strCode, out nNextIndex,nCurrIndex);
            if (-1 == nIndex)
            {
                nIndex = 13;
                strCode = "";
            }
            if (-1== nNextIndex)
            {
                //nIndex = 13;
                //strCode = "";
                nNextIndex = 1;
            }
        }

        public bool CamSnap(Vision_Step nStep,int nStaion,int nCub)
        {
            bool bRult = false;
            int nNumSnap = 0;
        TrySnap:
            if (!CongexVision.GetInstance().ProcessStep(nStep, nStaion))   //如果有料失败也是为TRUE
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
                        if(Vision_Step.T2_1== nStep)
                        {
                            StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Info, "T2_1连续三次拍照处理失败，是否重新拍？");//T2相机连续三次图像处理失败，会弹出一个提示窗体
                        }
                        else if(Vision_Step.T3_1==nStep)
                        {
                            StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Info, "T3_1连续三次拍照处理失败，是否重新拍？");//T3相机连续三次图像处理失败，会弹出一个提示窗体
                        }
                        else
                        {
                            StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Info, "T3_2连续三次拍照处理失败，是否重新拍？");//T3相机连续三次图像处理失败，会弹出一个提示窗体
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
                if (Vision_Step.T3_1 == nStep && SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_T3处理结果) == false && SystemMgr.GetInstance().GetParamBool("CognexCheck"))
                {
                    StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Info, "右边穴位拍照处理失败，请点击暂停后拿走物料！");
                    bRult = false;
                }

                if (Vision_Step.T3_2 == nStep && SystemMgr.GetInstance().GetRegBit((int)SysBitReg.Bit_T4处理结果) == false && SystemMgr.GetInstance().GetParamBool("CognexCheck"))
                {
                    StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_Erro_Info, "左边穴位拍照处理失败，请点击暂停后拿走物料！");
                    bRult = false;
                }
                //bSnapOk[nCub - 1] = true;
                nNumSnap = 0;
            }
            return bRult;
        }

        public override void EmgStop()
        {
            Robot_ABB.Robot_Stop((int)controller_name.Assemble);
            base.EmgStop();
        }
        public void 从Coil料盘中取Coil()
        {
            string strCod = "";      
        Label_FetchCoil:
            int nCurrIndex = nIndexFetch;
            int nNextCoil = 1;
            WaitRegBit((int)SysBitReg.Bit_Coil料盘准备就绪_Ok, true, -1);
            获取取料序号(out nIndexFetch,out strCod,out nNextCoil,nCurrIndex);
            StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_CoilCode_Info, strCod, true);
            if (13 == nIndexFetch/* || nNextCoil==1*/)
            {
                nIndexFetch = 1;
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_Coil料盘准备就绪_Ok, false, false);
                goto Label_FetchCoil;
            }
            int nUpOrDown = SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_Coil上下料盘_Coil);
            fXoffset= SystemMgr.GetInstance().GetParamDouble("CoilRobotFetchOffsetX");
            fYoffset = SystemMgr.GetInstance().GetParamDouble("CoilRobotFetchOffsetY");
            if (0== nUpOrDown)
            {
                fZoffset = SystemMgr.GetInstance().GetParamDouble("CoilRobotFetchOffsetZ");
            }
            else
            {
                fZoffset = SystemMgr.GetInstance().GetParamDouble("CoilRobotFetchDownOffsetZ");
            }
            string szBuffer = String.Format("GetCoil,{0},{1},{2},{3},{4:F3},{5:F3},{6:F3}", nSpeedPerce,nUpOrDown,nIndexFetch,nNextCoil, fXoffset, fYoffset,fZoffset);//0:上层 1：下层
            m_tcpRobotComm.WriteLine(szBuffer);
            while (true)
            {
                CheckContinue(false);
                if (m_tcpRobotComm.ReadLine(out szBuffer) > 0)
                {
                    szBuffer = szBuffer.Trim();
                    string[] szSplit = szBuffer.Split(',');
                    if (0 == szSplit[0].CompareTo("GetCoil"))
                        break;
                }
                WaitTimeDelay(50);
            }
            string[] szSplit1 = szBuffer.Split(',');
            CongexVision.GetInstance().SetCoordinatePosition(Convert.ToDouble(szSplit1[1]), Convert.ToDouble(szSplit1[2]), Convert.ToDouble(szSplit1[4]),2);
            //StrChangeTrigge.GetInstance().WriteRegStr((int)SysStrReg.Str_CoilCode_Info, strCod, false);
            nIndexFetch = nIndexFetch + 1;
            if (13 == nIndexFetch || nNextCoil == 1)//Coi料盘有上下两层，每层有12片料
            {
                nIndexFetch = 1;
                SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_Coil料盘准备就绪_Ok, false, false);
            }
        }
    }
}
