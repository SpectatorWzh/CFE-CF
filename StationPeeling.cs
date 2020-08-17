using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFrameDll;
using CommonTool;
using Communicate;
using System.Windows.Forms;

namespace AutoFrame
{
    class StationPeeling : StationBase
    {
        enum POINT
        {
            MARK = 1,
        }

        public int BanCavity;
        public bool bModeProduct;
        bool m_bReady = false;
        public TcpLink m_tcpRobotComm;
        private TcpLink _pCodeLink1;
        private TcpLink _pCodeLink2;
        bool[] bCodeOk;
        string[] _szCode;
        public StationPeeling(string strName) : base(strName)
        {
            io_in = new string[] { "2.9", "2.10", "2.11", "2.12" };
            io_out = new string[] { "2.11", "2.12", "2.13", "2.14", "2.15", "3.32" };
            _szCode = new string[] { "CodeNg", "CodeNg" };
            m_tcpRobotComm = TcpMgr.GetInstance().GetTcpLink(1);
            _pCodeLink1 = TcpMgr.GetInstance().GetTcpLink(8);
            _pCodeLink2 = TcpMgr.GetInstance().GetTcpLink(9);
            bCodeOk=new bool[]{ false,false};
        }

        public override void InitSecurityState()
        {
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_撕料站_Ready, false, false);
            base.InitSecurityState();
        }
       
        public override void StationInit()
        {
            int OenNum = 0;
            int nCommNum = 0;
            BanCavity = 0;
            bModeProduct = SystemMgr.GetInstance().GetParamBool("ProductOrTest");
            WaitTimeDelay(300);
            LineRobot:
            nCommNum++;
            //RobotSevorOn();
            IoMgr.GetInstance().WriteIoBit("撕膜吹气停", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("撕膜吹气", true);//gao

            ShowLog("打开撕料机器人通讯端");
            if (m_tcpRobotComm.IsOpen())
                m_tcpRobotComm.Close();

            try
            {
                m_tcpRobotComm.Open();
                //wait_recevie_cmd(m_tcpRobotComm, "Ready\r\n", 10000);
                string szBuffer;
                if (bModeProduct)
                {
                    szBuffer = "Init,1,0,0,0,0";
                }
                else
                {
                    szBuffer = "Init,0,0,0,0,0";
                }
                m_tcpRobotComm.WriteLine(szBuffer);
                wait_recevie_cmd(m_tcpRobotComm, "InitOk\r\n", 15000);
            }
            catch (Exception)
            {
                if (nCommNum < 5)
                    goto LineRobot;
                else
                    WarningMgr.GetInstance().Error("撕料机器人启动失败");
            }

            //CodeOpen:
            //OenNum++;
            //try
            //{
            //    if (!_pCodeLink1.IsOpen())
            //    {
            //        _pCodeLink1.Open();
            //    }
            //    if (!_pCodeLink2.IsOpen())
            //    {
            //        _pCodeLink2.Open();
            //    }
            //}
            //catch (Exception)
            //{
            //    if (nCommNum < 5)
            //        goto CodeOpen;
            //    else
            //        WarningMgr.GetInstance().Error("撕膜扫码枪启动失败");
            //}

            SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_撕料站_Ready), true, false);
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

            if (SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_ChooseMark) == 3 || SystemMgr.GetInstance().GetRegInt((int)SysIntReg.Int_ChooseMark) == 4)
            {
                WaitTimeDelay(50);
                goto label_Over;
            }

            int nSpeedPerce = SystemMgr.GetInstance().GetParamInt("SpeedRobotPeeling");
            string szBuffer ="";

         //   DateTime CTBegin = DateTime.Now;
            WaitRegBit((int)SysBitReg.Bit_转盘站通知撕料站_OK, true, -1);      
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知撕料站_OK, false, false);

            DateTime CTBegin = DateTime.Now;//GAO

            SpaceInfo infoSpace = ShareInfoSpace.GetSpaceInfo(2);
            if (false==ShareInfoSpace.GetSpaceInfobRult(2)||SystemMgr.GetInstance().GetParamBool("StationPeelingEnable"))
            {
                SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_撕料站_Ready), true, false);
                goto label_Over;
            }
            szBuffer = String.Format("MoveCodePos,{0},0,0,0,0",nSpeedPerce);
            m_tcpRobotComm.WriteLine(szBuffer);
            wait_recevie_cmd(m_tcpRobotComm, "MoveCodePos_Ok\r\n", -1);

            if (SystemMgr.GetInstance().GetParamBool("CognexCodeEnable"))
            {
                if(infoSpace.m_bBanCavity[0])
                {
                    if (infoSpace.m_bRult[0])
                    {
                        if (!IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[0]))
                        {
                            ShowLog("1穴吸真空失败");
                        }
                        ShareInfoSpace.SetSpaceInfobRult(GetFerriteCode(_pCodeLink1, 2, 1) && IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[0]), 2, 1);
                    }
                }
                else
                {
                    ShareInfoSpace.SetSpaceInfobRult(true, 2, 1);
                }

                if (infoSpace.m_bBanCavity[1])
                {
                    if (infoSpace.m_bRult[1])
                    {
                        if (!IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[1]))
                        {
                            ShowLog("2穴吸真空失败");
                        }
                        ShareInfoSpace.SetSpaceInfobRult(GetFerriteCode(_pCodeLink2, 2, 2) && IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[1]), 2, 2);
                    }
                }
                else
                {
                    ShareInfoSpace.SetSpaceInfobRult(true, 2, 2);
                }
                ShowLog(_szCode[0]);
                ShowLog(_szCode[1]);
            }     
            if (false == ShareInfoSpace.GetSpaceInfobRult(2))
            {
                if(IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[0])&& IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[1]))
                {
                    ShowLog("扫码失败");
                }
                SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_撕料站_Ready), true, false);
                goto label_Over;
            }
            else
            {
                ShowLog("撕膜成功");
            }
            
            if(infoSpace.m_bBanCavity[0]&&infoSpace.m_bBanCavity[1])
            {
                BanCavity = 0;
            }
            else
            {
                if(infoSpace.m_bBanCavity[0])
                {
                    BanCavity = 1;
                }
                else
                {
                    BanCavity = 2;
                }
            }
            szBuffer = String.Format("Peel,{0},{1},{2},0,0", nSpeedPerce, BanCavity,SystemMgr.GetInstance().GetParamInt("FerriteCodeTest"));
            m_tcpRobotComm.WriteLine(szBuffer);
            wait_recevie_cmd(m_tcpRobotComm, "PeelOk\r\n", -1);

            SystemMgr.GetInstance().WriteRegBit((int)(SysBitReg.Bit_撕料站_Ready), true, false);
            
            TimeSpan CTTime = DateTime.Now - CTBegin;
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.PeelingCT, CTTime.TotalSeconds);
            label_Over:
            ;
        }
        public override void StationDeinit()
        {
            IoMgr.GetInstance().WriteIoBit("撕膜吹气", false);//gao
            Thread.Sleep(50);
            IoMgr.GetInstance().WriteIoBit("撕膜吹气停", true);
            Thread.Sleep(50);
            IoMgr.GetInstance().WriteIoBit("撕膜吹气停", false);
            base.StationDeinit();
        }
        public void RobotSevorOn()
        {
            IoMgr.GetInstance().WriteIoBit("ADO-Stop", true);
            WaitTimeDelay(400);
            IoMgr.GetInstance().WriteIoBit("ADO-Stop", false);
            WaitTimeDelay(400);
            //WaitIo("FtDI-Stop", true, -1);
            IoMgr.GetInstance().WriteIoBit("ADO-Reset", true);
            WaitTimeDelay(400);
            IoMgr.GetInstance().WriteIoBit("ADO-Reset", false);
            IoMgr.GetInstance().WriteIoBit("ADO-Start", true);
            WaitTimeDelay(400);
            IoMgr.GetInstance().WriteIoBit("ADO-Start", false);
            WaitTimeDelay(400);
        }

        public override void EmgStop()
        {
            Robot_ABB.Robot_Stop((int)controller_name.Peel);
            base.EmgStop();
        }
        public bool GetFerriteCode(TcpLink pCodeLink,int nStation,int nCub)
        {
            string strCode;
            bool bRult = false;
            _szCode[nCub - 1] = "";
            if (pCodeLink.Open())
            {
                // pCodeLink.ReadLine(out strCode);
                pCodeLink.ClearBuffer();
                pCodeLink.WriteLine("GetSN");
                strCode = "";
                if (pCodeLink.ReadLine(out strCode) > 0)
                {
                    strCode = strCode.Trim();
                    if (strCode.Length == SystemMgr.GetInstance().GetParamInt("FerriteLegth"))
                    {
                        if(0==ShareInfoSpace._nFerriteStyle)
                        {
                            ShareInfoSpace.SetInfoCode(nStation, nCub, strCode);
                            bRult = true;
                            _szCode[nCub - 1] = strCode;
                        }
                        else
                        {
                            if(ShareInfoSpace.arrayFerriteStyle[ShareInfoSpace._nFerriteStyle-1].IsSubjection(strCode))
                            {
                                if(0==ShareInfoSpace._nFerriteHomeStyle)
                                {
                                    ShareInfoSpace.SetInfoCode(nStation, nCub, strCode);
                                    bRult = true;
                                    _szCode[nCub - 1] = strCode;
                                }
                                else if(1==ShareInfoSpace._nFerriteHomeStyle)
                                {
                                    string strFir = strCode.Substring(0, 1);
                                    if(0==strFir.CompareTo("L"))
                                    {
                                        ShareInfoSpace.SetInfoCode(nStation, nCub, strCode);
                                        bRult = true;
                                        _szCode[nCub - 1] = strCode;
                                    }
                                    else
                                    {
                                        ShareInfoSpace.SetSpaceInfobRult(false, nStation, nCub);
                                        ShareInfoSpace.SetInfoCode(nStation, nCub, "CodeNG");
                                        _szCode[nCub - 1] = "厂家不正确，请查看";
                                    }
                                }
                                else if(2 == ShareInfoSpace._nFerriteHomeStyle)
                                {
                                    string strFir = strCode.Substring(0, 1);
                                    if (0 == strFir.CompareTo("M"))
                                    {
                                        ShareInfoSpace.SetInfoCode(nStation, nCub, strCode);
                                        bRult = true;
                                        _szCode[nCub - 1] = strCode;
                                    }
                                    else
                                    {
                                        ShareInfoSpace.SetSpaceInfobRult(false, nStation, nCub);
                                        ShareInfoSpace.SetInfoCode(nStation, nCub, "CodeNG");
                                        _szCode[nCub - 1] = "厂家不正确，请查看";
                                    }
                                }
                            }
                            else
                            {
                                ShareInfoSpace.SetSpaceInfobRult(false, nStation, nCub);
                                ShareInfoSpace.SetInfoCode(nStation, nCub, "CodeNG");
                                string strErr = "";
                                strErr = String.Format("第{0}穴不是{1}，请查验。", nCub, ShareInfoSpace.arrayFerriteStyle[ShareInfoSpace._nFerriteStyle - 1].NameStyleFerrite);
                                //ShowLog(strErr);
                                MessageBox.Show(strCode + strErr);
                                WarningMgr.GetInstance().Warning(strErr);
                                _szCode[nCub - 1] = strErr;
                            }
                        }
                    }
                    else
                    {
                        ShareInfoSpace.SetSpaceInfobRult(false, nStation, nCub);
                        ShareInfoSpace.SetInfoCode(nStation, nCub, "CodeNG");
                        _szCode[nCub - 1] = "CodeNG";
                    }
                }
                else
                {
                    ShareInfoSpace.SetSpaceInfobRult(false, nStation, nCub);
                    ShareInfoSpace.SetInfoCode(nStation, nCub, "CodeNG");
                    _szCode[nCub - 1] = "CodeNG";
                }
                pCodeLink.WriteLine("-");
            }
            else
            {
                ShareInfoSpace.SetSpaceInfobRult(false, nStation, nCub);
                ShareInfoSpace.SetInfoCode(nStation, nCub, "CodeNG");
                _szCode[nCub - 1] = "CodeNG";
            }
            return bRult;
        }
    }
}
