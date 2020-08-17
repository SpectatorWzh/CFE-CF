using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTool;
using AutoFrameDll;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace AutoFrame
{
    
    class SafeDoor : SingletonTemplate<SafeDoor>
    {
        private readonly object m_LoackSafe = new object();
        public delegate void DoorSafety();
        public DoorSafety SafeDoorEvent;
        public bool Enable=false;
        public override void ThreadMonitor()
        {
            Thread.Sleep(2000);
            while (true)
            {
                if(!IoMgr.GetInstance().ReadIoInBit("CKD-ALM1")|| !IoMgr.GetInstance().ReadIoInBit("CKD-ALM2"))
                {
                    安全门使能(false);
                    SafeDoorEvent();
                    WarningMgr.GetInstance().Warning("DD马达报警，请检查马达是否过载之后进行初始化处理");
                    MessageBox.Show("DD马达报警，请检查马达是否过载之后进行初始化处理");
                   
                    //Enable = false;
                }
                if (Enable == true&&SystemMgr.GetInstance().GetParamBool("Enable_SafetyDoor"))
                {
                    if (!IoMgr.GetInstance().ReadIoInBit("安全门"))
                    {
                        安全门使能(false);
                        SafeDoorEvent();
                        WarningMgr.GetInstance().Warning("安全门打开");                        
                       // Enable = false;
                    }

                }
                Thread.Sleep(50);
            }

        }

        public void 安全门使能(bool b_Value)
        {
            lock(m_LoackSafe)
            {
                Enable = b_Value;
            }
        }
    }
}
