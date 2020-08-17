using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFrameDll;
using CommonTool;
using Communicate;
using System.Windows.Forms;

namespace AutoFrame
{
   
    class StationPress : StationBase
    {
        enum POINT
        {
            MARK = 1,
        }
        ComLink m_pComm;
        bool b_DryRun=false;
        public StationPress(string strName) : base(strName)
        {
            m_pComm = ComMgr.GetInstance().GetComLink(0);
            io_in = new string[] { "2.1", "2.2", "2.3", "2.4", "2.5", "2.6", "2.7", "2.8" };
            io_out = new string[] { "2.3", "2.4", "2.5", "2.6", "2.7", "2.8", "2.9", "2.10" };
        }
        public override void InitSecurityState()
        {
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_保压站_Ready, false, false);
            base.InitSecurityState();
        }

       
        public override void StationInit()
        {
            CylPress1Up();
            WaitTimeDelay(200);
            WaitIo("保压气缸1上升到位", true, 0);
            CylPress2Up();
            WaitTimeDelay(200);
            WaitIo("保压气缸2上升到位", true, 0);
            b_DryRun = SystemMgr.GetInstance().GetParamBool("ProductOrTest");
            WaitTimeDelay(200);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_保压站_Ready, true, false);
            保压真空停();
            ShowLog("初始化完成");
            //base.StationInit();
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
        //    DateTime CTBegin = DateTime.Now;
            WaitRegBit((int)SysBitReg.Bit_转盘站通知保压站_OK, true, -1);
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_转盘站通知保压站_OK, false, false);
            DateTime CTBegin = DateTime.Now;//GAO
            SpaceInfo infoSpace = ShareInfoSpace.GetSpaceInfo(4);
            bool bPres = infoSpace.m_bRult[0] || infoSpace.m_bRult[1];
            if(infoSpace.m_bBanCavity[0]&&infoSpace.m_bBanCavity[1])
            {
                if (false == bPres/*!ShareInfoSpace.GetSpaceInfobRult(4)*/ || SystemMgr.GetInstance().GetParamBool("StationPressEnable"))
                {
                    WaitTimeDelay(100);
                    SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_保压站_Ready, true, false);
                    goto label_Over;
                }
            }
           

           //if (ShareInfoSpace.GetSpaceInfobRult(4))
            if(infoSpace.m_bRult[0])
            {
                CylPress1Down();
            }
            if(infoSpace.m_bRult[1])
            {
                CylPress2Down();
            }
            
            int nTime = 0;
            int presstime = SystemMgr.GetInstance().GetParamInt("PressTime")*1000;
            WaitTimeDelay(presstime);
            /*
            float force1, force2, force3, force4;
            ForceAcquisition.GetInstance().GetForceValue(out force1, out force2, out force3, out force4);
            ShareInfoSpace.SaveForceData(force1, force2, force3, force4);
            WaitTimeDelay(1000);
            ForceAcquisition.GetInstance().GetForceValue(out force1, out force2, out force3, out force4);
            ShareInfoSpace.SaveForceData(force1, force2, force3, force4);
            WaitTimeDelay(1000);
            ForceAcquisition.GetInstance().GetForceValue(out force1, out force2, out force3, out force4);
            ShareInfoSpace.SaveForceData(force1, force2, force3, force4);
            WaitTimeDelay(1000);
            ForceAcquisition.GetInstance().GetForceValue(out force1, out force2, out force3, out force4);
            ShareInfoSpace.SaveForceData(force1, force2, force3, force4);
            WaitTimeDelay(1000);
            ForceAcquisition.GetInstance().GetForceValue(out force1, out force2, out force3, out force4);
            ShareInfoSpace.SaveForceData(force1, force2, force3, force4);
            if(force1<43 || force1>47)
            {
                ShareInfoSpace.SetSpacePressRult(4, 1, 0);   //0为保压失败
            }
            
            if (force2 < 43 || force2 > 47)
            {
                ShareInfoSpace.SetSpacePressRult(4, 2, 0);
            }
          
            if (force3 < 43 || force3 > 47)
            {
                ShareInfoSpace.SetSpacePressRult(4, 3, 0);
            }
          
            if (force4 < 43 || force4 > 47)
            {
                ShareInfoSpace.SetSpacePressRult(4, 4, 0);
            }
           */
            //保压真空破();
            WaitTimeDelay(200);

            CylPress1Up();
            CylPress2Up();

            保压真空破();


            if (!b_DryRun)
            {
                //if (ShareInfoSpace.GetSpaceInfobRult(4))
                if(infoSpace.m_bRult[0])
                {
                    if (!IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[0]))
                    {
                        IoMgr.GetInstance().AlarmLight(LightState.黄灯开, 0);
                        IoMgr.GetInstance().AlarmLight(LightState.蜂鸣闪, 5000);
                        MessageBox.Show("1号保压头黏住产品，请点击暂停后手动清除产品！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        IoMgr.GetInstance().AlarmLight(LightState.蜂鸣关);
                        IoMgr.GetInstance().AlarmLight(LightState.绿灯开);
                    }
                }
                if(infoSpace.m_bRult[1])
                {
                    if (!IoMgr.GetInstance().ReadIoInBit(infoSpace.szIoInDetail[1]))
                    {
                        IoMgr.GetInstance().AlarmLight(LightState.黄灯开, 0);
                        IoMgr.GetInstance().AlarmLight(LightState.蜂鸣闪, 5000);
                        MessageBox.Show("2号保压头黏住产品，请点击暂停后手动清除产品！", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        IoMgr.GetInstance().AlarmLight(LightState.蜂鸣关);
                        IoMgr.GetInstance().AlarmLight(LightState.绿灯开);
                    }
                }
            }
           
           

            
            WaitIo("保压气缸1上升到位", true, 0);
            WaitIo("保压气缸2上升到位", true, 0);
            保压真空停();
            SystemMgr.GetInstance().WriteRegBit((int)SysBitReg.Bit_保压站_Ready, true, false);
            label_Over:
            TimeSpan CTTime = DateTime.Now - CTBegin;
            SystemMgr.GetInstance().WriteRegDouble((int)SysDoubleReg.PressureCT, CTTime.TotalSeconds);
            ;
            
        }
        public void 保压真空破()
        {
            IoMgr.GetInstance().WriteIoBit("保压头真空停", false);
            IoMgr.GetInstance().WriteIoBit("保压头真空破", true);  
        }

        public void 保压真空停()
        {
            IoMgr.GetInstance().WriteIoBit("保压头真空破", false);
            IoMgr.GetInstance().WriteIoBit("保压头真空停", true);
            WaitTimeDelay(100);
            IoMgr.GetInstance().WriteIoBit("保压头真空停", false);
        }
        /// <summary>
        /// 保压气缸1升
        /// </summary>
        public void CylPress1Up()
        {
            IoMgr.GetInstance().WriteIoBit("保压气缸1降", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("保压气缸1升", true);
        }

        /// <summary>
        /// 保压气缸1降
        /// </summary>
        public void CylPress1Down()
        {
            IoMgr.GetInstance().WriteIoBit("保压气缸1升", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("保压气缸1降", true);
        }

        /// <summary>
        /// 保压气缸2升
        /// </summary>
        public void CylPress2Up()
        {
            IoMgr.GetInstance().WriteIoBit("保压气缸2降", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("保压气缸2升", true);
        }

        /// <summary>
        /// 保压气缸2降
        /// </summary>
        public void CylPress2Down()
        {
            IoMgr.GetInstance().WriteIoBit("保压气缸2升", false);
            WaitTimeDelay(50);
            IoMgr.GetInstance().WriteIoBit("保压气缸2降", true);
        }
        public override void StationDeinit()
        {
            IoMgr.GetInstance().WriteIoBit("保压气缸1降", false);
            IoMgr.GetInstance().WriteIoBit("保压气缸1升", true);            
            IoMgr.GetInstance().WriteIoBit("保压气缸2降", false);
            IoMgr.GetInstance().WriteIoBit("保压气缸2升", true);
            base.StationDeinit();
        }
        public void PressCheck()
        {

        }
    }
}
