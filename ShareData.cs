using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFrameDll;
using System.Windows.Forms;
using CommonTool;
using System.Threading;

namespace AutoFrame
{
    class ShareData// : StationBase
    {
        public static double laserOffset1 = 0;
        public static double laserOffset2 = 0;

        private static  bool bStop = false;
        private static bool bRet = false;
        public static bool WaitIoSingle(int card, int index, bool value, int nTime = -1)
        {
            int count = 0;
            bool bRet = false;
            if (nTime == 0)
                nTime = SystemMgr.GetInstance().GetParamInt("IoTimeOut") * 1000;

            do
            {
                bRet = IoMgr.GetInstance().ReadIoInBit(card, index );
                if (bRet)
                    return true;

                System.Threading.Thread.Sleep(20);
                count++;
            }
            while (nTime == -1 || count * 20 < nTime);

                return false;
        }
        public static void WaitIo(int card, int index, bool value, int nTime = -1)
        {
            bStop = false;
            string str = "";
            while (!bStop)
            {
                if (WaitIoSingle(card, index, value, nTime))
                    break;
                else
                {
                    str = "Wait IO " + card + "." + index + " timeout, Do you want to continue waiting?";
                    if (MessageBox.Show(str, "Prompt", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning)
                        == DialogResult.Cancel)
                    {
                        bStop = true;
                    }
                }
            }
        }
        #region station_load_reverse
        /// <summary>
        /// 
        /// </summary>
        /// <returns>true or false</returns>
        public static bool LoadReverse(out string strRet)
        {
            strRet = "";
            bRet = false;

            IoMgr.GetInstance().WriteIoBit(5, 10, false);
            WaitIo(6, 3, true, 0);
            Thread.Sleep(500);
            //载具脱离关
            IoMgr.GetInstance().WriteIoBit(5, 6, false);
            WaitIo(5, 13, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            //解锁顶升
            IoMgr.GetInstance().WriteIoBit(5, 8, true);
            IoMgr.GetInstance().WriteIoBit(5, 9, false);
            WaitIo(5, 16, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            IoMgr.GetInstance().WriteIoBit(5, 10, true);
            WaitIo(6, 2, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            //载具脱离
            IoMgr.GetInstance().WriteIoBit(5, 6, true);
            WaitIo(5, 12, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);


            //如果在上面则松开下降
            if (IoMgr.GetInstance().ReadIoInBit(1, 15))
            {
                //松开
                IoMgr.GetInstance().WriteIoBit(1, 15, false);
                IoMgr.GetInstance().WriteIoBit(1, 16, true);
                WaitIo(1, 18, true, 0);
                if (bStop)
                    return bRet;
                Thread.Sleep(500);

                //下降
                IoMgr.GetInstance().WriteIoBit(1, 13, false);
                IoMgr.GetInstance().WriteIoBit(1, 14, true);
                WaitIo(1, 16, true, 0);
                if (bStop)
                    return bRet;
                Thread.Sleep(500);
            }

            //夹紧
            IoMgr.GetInstance().WriteIoBit(1, 15, true);
            IoMgr.GetInstance().WriteIoBit(1, 16, false);
            WaitIo(1, 17, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //上升
            IoMgr.GetInstance().WriteIoBit(1, 13, true);
            IoMgr.GetInstance().WriteIoBit(1, 14, false);
            WaitIo(1, 15, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            if (IoMgr.GetInstance().ReadIoInBit(1, 19) && !IoMgr.GetInstance().ReadIoInBit(1, 20))
            {
                //下翻

                //TODO string  5# IO BAD
                IoMgr.GetInstance().WriteIoBit("进料翻转气缸上", false);
                IoMgr.GetInstance().WriteIoBit("进料翻转气缸下", true);
                //IoMgr.GetInstance().WriteIoBit(1, 17, false);
                //IoMgr.GetInstance().WriteIoBit(1, 18, true);
                //     IoMgr.GetInstance().WriteIoBit(3, 9, false);
                WaitIo(1, 20, true, 0);
                if (bStop)
                    return bRet;
                Thread.Sleep(500);
            }
            else if (!IoMgr.GetInstance().ReadIoInBit(1, 19) && IoMgr.GetInstance().ReadIoInBit(1, 20))
            {
                //上翻
                //TODO string //TODO string  5# IO BAD
                IoMgr.GetInstance().WriteIoBit("进料翻转气缸上", true);
                IoMgr.GetInstance().WriteIoBit("进料翻转气缸下", false);
                //IoMgr.GetInstance().WriteIoBit(1, 17, true);
                //IoMgr.GetInstance().WriteIoBit(1, 18, false);
                //    IoMgr.GetInstance().WriteIoBit(3, 9, true);
                WaitIo(1, 19, true, 0);
                if (bStop)
                    return bRet;
                Thread.Sleep(500);
            }
            else
            {
                //
                strRet = "夹抓上下面状态未知！";
                return false;
            }

            //下降
            IoMgr.GetInstance().WriteIoBit(1, 13, false);
            IoMgr.GetInstance().WriteIoBit(1, 14, true);
            WaitIo(1, 16, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //松开
            IoMgr.GetInstance().WriteIoBit(1, 15, false);
            IoMgr.GetInstance().WriteIoBit(1, 16, true);
            WaitIo(1, 18, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //载具脱离关
            IoMgr.GetInstance().WriteIoBit(5, 6, false);
            WaitIo(5, 13, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            //解锁顶升
            IoMgr.GetInstance().WriteIoBit(5, 10, false);
            WaitIo(6, 3, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            IoMgr.GetInstance().WriteIoBit(5, 8, false);
            IoMgr.GetInstance().WriteIoBit(5, 9, true);
            WaitIo(6, 1, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //上升
            IoMgr.GetInstance().WriteIoBit(1, 13, true);
            IoMgr.GetInstance().WriteIoBit(1, 14, false);
            WaitIo(1, 15, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            return true;
        }
        #endregion

        #region station_unload_reverse
        public static bool UnloadReverse(out string strRet)
        {
            strRet = "";
            bRet = false;

            //解锁顶升
            IoMgr.GetInstance().WriteIoBit(5, 13, false);
            WaitIo(6, 7, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            //载具脱离关
            IoMgr.GetInstance().WriteIoBit(3, 7, false);
            IoMgr.GetInstance().WriteIoBit(3, 8, true);
            WaitIo(3, 16, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            IoMgr.GetInstance().WriteIoBit(5, 11, true);
            IoMgr.GetInstance().WriteIoBit(5, 12, false);
            WaitIo(6, 4, true, 0);
            if (bStop)
                return bRet;
            IoMgr.GetInstance().WriteIoBit(5, 13, true);
            WaitIo(6, 6, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            //载具脱离
            IoMgr.GetInstance().WriteIoBit(3, 7, true);
            IoMgr.GetInstance().WriteIoBit(3, 8, false);
            WaitIo(3, 15, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //如果在上面则松开下降
            if (IoMgr.GetInstance().ReadIoInBit(2, 1))
            {
                //松开
                IoMgr.GetInstance().WriteIoBit(2, 1, false);
                IoMgr.GetInstance().WriteIoBit(2, 2, true);
                WaitIo(2, 4, true, 0);
                if (bStop)
                    return bRet;
                Thread.Sleep(500);

                //下降
                IoMgr.GetInstance().WriteIoBit(1, 19, false);
                IoMgr.GetInstance().WriteIoBit(1, 20, true);
                WaitIo(2, 2, true, 0);
                if (bStop)
                    return bRet;
                Thread.Sleep(500);
            }

            //夹紧
            IoMgr.GetInstance().WriteIoBit(2, 1, true);
            IoMgr.GetInstance().WriteIoBit(2, 2, false);
            WaitIo(2, 3, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //上升
            IoMgr.GetInstance().WriteIoBit(1, 19, true);
            IoMgr.GetInstance().WriteIoBit(1, 20, false);
            WaitIo(2, 1, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            if (IoMgr.GetInstance().ReadIoInBit(2, 5) && !IoMgr.GetInstance().ReadIoInBit(2, 6))
            {
                //下翻
                IoMgr.GetInstance().WriteIoBit(2, 3, false);
                IoMgr.GetInstance().WriteIoBit(2, 4, true);
                WaitIo(2, 6, true, 0);
                if (bStop)
                    return bRet;
                Thread.Sleep(500);
            }
            else if (!IoMgr.GetInstance().ReadIoInBit(2, 5) && IoMgr.GetInstance().ReadIoInBit(2, 6))
            {
                //上翻
                IoMgr.GetInstance().WriteIoBit(2, 3, true);
                IoMgr.GetInstance().WriteIoBit(2, 4, false);
                WaitIo(2, 5, true, 0);
                if (bStop)
                    return bRet;
                Thread.Sleep(500);
            }
            else
            {
                //WarningMgr.GetInstance().Warning("夹抓上下面状态未知！");
                strRet = "夹抓上下面状态未知！";
                return false;
            }

            //下降
            IoMgr.GetInstance().WriteIoBit(1, 19, false);
            IoMgr.GetInstance().WriteIoBit(1, 20, true);
            WaitIo(2, 2, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //松开
            IoMgr.GetInstance().WriteIoBit(2, 1, false);
            IoMgr.GetInstance().WriteIoBit(2, 2, true);
            WaitIo(2, 4, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //上升
            IoMgr.GetInstance().WriteIoBit(1, 19, true);
            IoMgr.GetInstance().WriteIoBit(1, 20, false);
            WaitIo(2, 1, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //载具脱离关
            IoMgr.GetInstance().WriteIoBit(3, 7, false);
            IoMgr.GetInstance().WriteIoBit(3, 8, true);
            WaitIo(3, 16, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            //锁下降
            IoMgr.GetInstance().WriteIoBit(5, 13, false);
            WaitIo(6, 7, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            IoMgr.GetInstance().WriteIoBit(5, 11, false);
            IoMgr.GetInstance().WriteIoBit(5, 12, true);
            WaitIo(6, 5, true, 0);
            if (bStop)
                return bRet;

            return true;
        }
        #endregion

        #region station_cover
        /// <summary>
        /// 
        /// </summary>
        /// <returns>true or false</returns>
        public static bool Unload(out string strRet)
        {
            strRet = "";
            bRet = false;

            //判断夹爪状态
            if (IoMgr.GetInstance().ReadIoInBit(1, 13))
            {
                MessageBox.Show("夹爪发现盖板!");
                return false;
            }

            if (MessageBox.Show( "请确认抓盖板已回过原点，且在Unload位置！",  "Prompt", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
                       == DialogResult.Cancel)
            {
                return false;
            }
           
            //取盖板解锁伸缩气缸缩回
            IoMgr.GetInstance().WriteIoBit(5, 5, false);
            WaitIo(5, 11, true,0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //盖板气缸下降
            IoMgr.GetInstance().WriteIoBit(1, 9, false);
            IoMgr.GetInstance().WriteIoBit(1, 10, true);
            WaitIo(1, 12, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            IoMgr.GetInstance().WriteIoBit(5, 3, true);
            IoMgr.GetInstance().WriteIoBit(5, 4, false);
            WaitIo(5, 8, true, 0); //解锁气缸上升
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            IoMgr.GetInstance().WriteIoBit(5, 5, true);
            WaitIo(5, 10, true, 0);//解锁气缸伸出
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            MessageBox.Show("请确认盖板已经解锁！");

            //盖板夹爪夹紧
            IoMgr.GetInstance().WriteIoBit(1, 11, true);
            IoMgr.GetInstance().WriteIoBit(1, 12, false);
            WaitIo(1, 13, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            //取盖板气缸上升
            IoMgr.GetInstance().WriteIoBit(1, 9, true);
            IoMgr.GetInstance().WriteIoBit(1, 10, false);
            WaitIo(1, 11, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //取盖板锁定
            IoMgr.GetInstance().WriteIoBit(5, 5, false);
            WaitIo(5, 11, true, 0);//解锁气缸缩回
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            IoMgr.GetInstance().WriteIoBit(5, 3, false);
            IoMgr.GetInstance().WriteIoBit(5, 4, true);
            //等待锁定
            WaitIo(5, 9, true, 0); //解锁气缸下降
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns>true or false</returns>
        public static bool Load(out string strRet)
        {
            strRet = "";
            bRet = false;

            //判断夹爪状态
            //if (!IoMgr.GetInstance().ReadIoInBit(1, 13))
            //{
            //    MessageBox.Show("夹爪未发现盖板!");
            //    return false;
            //}

            if (MessageBox.Show("请确认抓盖板已回过原点，且在Load位置！", "Prompt", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
                       == DialogResult.Cancel)
            {
                return false;
            }

            IoMgr.GetInstance().WriteIoBit(5, 7, false);
            WaitIo(5, 15, true, 0);//解锁气缸缩
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            //放盖板解锁
            IoMgr.GetInstance().WriteIoBit(5, 6, true);
            WaitIo(5, 12, true, 0); //解锁气缸出
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            IoMgr.GetInstance().WriteIoBit(5, 7, true);
            WaitIo(5, 14, true, 0);//解锁气缸张
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //盖板气缸下降
            IoMgr.GetInstance().WriteIoBit(1, 9, false);
            IoMgr.GetInstance().WriteIoBit(1, 10, true);
            WaitIo(1, 12, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            //放盖板解锁
            IoMgr.GetInstance().WriteIoBit(5, 7, false);
            WaitIo(5, 15, true, 0);//解锁气缸缩
            if (bStop)
                return bRet;
            Thread.Sleep(500);
            IoMgr.GetInstance().WriteIoBit(5, 6, false);
            WaitIo(5, 13, true, 0); //解锁气缸回
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            MessageBox.Show("请确认盖板已盖好！");

            //盖板夹爪打开
            IoMgr.GetInstance().WriteIoBit(1, 11, false);
            IoMgr.GetInstance().WriteIoBit(1, 12, true);
            WaitIo(1, 14, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            //盖板气缸上升
            IoMgr.GetInstance().WriteIoBit(1, 9, true);
            IoMgr.GetInstance().WriteIoBit(1, 10, false);
            WaitIo(1, 11, true, 0);
            if (bStop)
                return bRet;
            Thread.Sleep(500);

            return true;
        }
        #endregion

        //public ShareData(string strName) : base(strName)
        // {
        //    ;
        //}
        //public ShareData()
        //    : this("manual")
        //{
        //    ;
        //}
    }
}
