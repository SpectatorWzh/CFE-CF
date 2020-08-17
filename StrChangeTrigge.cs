using CommonTool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoFrame
{
    class StrChangeTrigge : SingletonTemplate<StrChangeTrigge>
    {
        private string[] strArray;
        public delegate void StrChangedHandler(int nIndex, string str);
        public StrChangedHandler StrChangedEvent;
        private object m_lock=new object();
        public StrChangeTrigge()
        {
            strArray = new string[1024];
            for (int i = 0; i < strArray.Length;i++)
            {
                strArray[i] = "";
            }
            strArray.Initialize();
        }
        public void WriteRegStr(int nIndex, string str, bool bNotify = true)
        {
            strArray[nIndex] = str;
            if (bNotify && StrChangedEvent != null)
            {
                this.StrChangedEvent(nIndex, str);
            }
            //lock(m_lock)
            //{

            //}
        }
        public string GetRegStr(int nIndex)
        {
            return strArray[nIndex];
            //lock (m_lock)
            //{

            //}
        }
    }
}
