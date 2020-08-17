using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFrameDll;
using Communicate;
using System.Threading;
using CommonTool;
using System.Windows.Forms;

namespace AutoFrame
{
    public class StationEx : StationBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="strName">站位名称</param>
        public StationEx(string strName) : base(strName)
        {

        }

        /// <summary>
        /// 通过标签名称等待结果
        /// </summary>
        /// <param name="sTagName">标签名称</param>
        /// <param name="sValue">等待结果</param>
        /// <param name="nTimeOut">超时时间，-1:表示一直等，0:表示参数OpcTimeOut中配置的时间</param>
        /// <param name="bShowDialog">超时时是否弹出对话框</param>
        /// <param name="bPause">超时时是否暂停</param>
        /// <returns>是否超时，超时返回false，不超时返回true</returns>
        public virtual bool WaitOpcByTag(string sTagName,string sValue, int nTimeOut = -1, bool bShowDialog = true, bool bPause = true)
        {
            bool bRet = true;
            string strTagDesc = OpcMgr.GetInstance().GetOpcTagDesc(sTagName);

            base.ShowLog(String.Format("等待OPC {0}:{1} = {2}", sTagName,strTagDesc,sValue));

            if (SystemMgr.GetInstance().IsSimulateRunMode())
            {
                Thread.Sleep(100);
            }
            else
            {
                DateTime timeStart = DateTime.Now;
                string sRead = "";
                int nTime = nTimeOut;
                while (true)
                {
                    this.CheckContinue(false);

                    sRead = OpcMgr.GetInstance().ReadDataByTag(sTagName);

                    if (sRead == sValue)
                    {
                        break;
                    }

                    if (nTimeOut == 0)
                    {
                        nTime = SystemMgr.GetInstance().GetParamInt("OpcTimeOut");
                    }

                    if (nTimeOut != -1)
                    {
                        TimeSpan span = DateTime.Now - timeStart;

                        if (span.TotalMilliseconds > nTime)
                        {
                            if (bShowDialog)
                            {
                                string sMsg = String.Format("等待OPC {0}:{1} = {2} 超时", sTagName, strTagDesc, sValue);

                                DialogResult result = this.ShowMessage(sMsg + ",是否继续等待");
                                if (result == DialogResult.No)
                                {
                                    //超时,退出
                                    bRet = false;
                                    base.ShowLog(sMsg + "，退出流程");
                                    throw new StationBase.StationException(string.Format("90001,ERR-XYT,{0}", sMsg));

                                    break;
                                }
                                else if (result == DialogResult.Yes)
                                {
                                    //超时，继续等待
                                    base.ShowLog(sMsg + "，重置超时");
                                    timeStart = DateTime.Now;
                                }
                                else
                                {
                                    //等待超时，忽略
                                    base.ShowLog(sMsg + "，忽略此超时");
                                    bRet = false;
                                    break;
                                }
                                    
                            }

                            if (bPause && StationMgr.GetInstance().AllowPause())
                            {
                                StationMgr.GetInstance().PauseAllStation();
                            }
                        }
                    }

                    Thread.Sleep(100);
                }

            }

            return bRet;
        }

        /// <summary>
        /// 通过标签描述等待结果
        /// </summary>
        /// <param name="sDesc">标签描述</param>
        /// <param name="sValue">等待结果</param>
        /// <param name="nTimeOut">超时时间，-1:表示一直等，0:表示参数OpcTimeOut中配置的时间</param>
        /// <param name="bShowDialog">超时时是否弹出对话框</param>
        /// <param name="bPause">超时时是否暂停</param>
        /// <returns>是否超时，超时返回false，不超时返回true</returns>
        public virtual bool WaitOpcByDesc(string sDesc, string sValue, int nTimeOut = -1, bool bShowDialog = true, bool bPause = true)
        {
            bool bRet = true;
            string sTagName = OpcMgr.GetInstance().GetOpcTagName(sDesc);

            bRet = WaitOpcByTag(sTagName, sValue, nTimeOut, bShowDialog, bPause);

            return bRet;
        }
    }
}
