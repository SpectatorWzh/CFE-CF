using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;
using System.IO;
using AutoFrameDll;
using CommonTool;
using System.Data.SqlClient;
namespace AutoFrame
{
    enum FerriteMode
    {
        D32,
        D33,
        N84,
    }
    struct SpaceInfo
    {
        public int nCub;
        public string[] szIoOutDetail;
        public string[] szIoInDetail;
        public string[] strCode;
        public int[] m_nPress;
        public bool[] m_bRult;//用于表示各工位是否做
        public bool m_bUnLoad;
        public bool[] m_bBanCavity;
    }
    struct CodeInfo
    {
        public string szCode;
        public bool m_bRult;
    }
    class ShareInfoSpace
    {
        public static SpaceInfo[] arraySpaceInfo = new SpaceInfo[4];
        private static readonly object m_lock =new object();
        private static readonly object m_Clearlock = new object();
        private static readonly object m_Unloadlock = new object();
        private static readonly object m_Feedlock = new object();
        private static readonly object m_BanCavity = new object();
        private static readonly object m_UnloadFerritelock = new object();
        public static string strHomeName;
        public static int m_nHome = new int();
        public static int m_nHomeLimUp = new int();
        public static int m_nHomeLimDown = new int();
        public static int m_nRotateTimes = new int();
        public static bool _bClear=false;
        private static bool _AxisRun = false;
        private static readonly object m_RunLock = new object();

        private static readonly object m_PdcaLock = new object();
        public static CodeInfo[] arrayCodeInfo = new CodeInfo[12];

        private static bool _bFeedTryEmpty = false;
        private static bool _bUnloadTryEmpty = false;
        public static int _nFerriteStyle =0;
        public static int _nFerriteHomeStyle = 0;
        public static CFerriteStyle[] arrayFerriteStyle = new CFerriteStyle[3];
        public static DateTime StartTime;

        public static string[] m_strDataHead = new string[] { "SN", "Time", "Line", "Machine number", "Station number","Cavity number",/*"Ferrite roll number",*/ "Station", "Result" };
        public static string[] m_Pointhead = new string[] { "Time", "SN","Ferrite Code", "AssembleX", "AssembleY", "OffsetX", "OffsetY","Station Number","Cavity Number" ,"UseEnable"};
        public static bool GetUnloadTryState()
        {
            lock (m_UnloadFerritelock)
            {
                return _bUnloadTryEmpty;
            }
        }
        public static void SetUnloadTryState(bool bState)
        {
            lock (m_UnloadFerritelock)
            {
                _bUnloadTryEmpty = bState;
            }
        }
        public static bool GetFeedTryState()
        {
            lock(m_Feedlock)
            {
                return _bFeedTryEmpty;
            }
        }
        public static void SetFeedTryState(bool bState)
        {
            lock (m_Feedlock)
            {
                _bFeedTryEmpty = bState;
            }
        }
        public static void GetCoilIndex(out int nIdex, out string strCode, out int nNextIndex, int nCurrIndex)
        {
            int nRult = -1;
            int nNextRult = -1;
            if (nCurrIndex > 12)
            {
                nIdex = -1;
                nNextIndex = -1;
                strCode = "NG";
                return;
            }
            for(int i= nCurrIndex - 1;i<12;i++)
            {
                if(arrayCodeInfo[i].m_bRult)
                {
                    nRult = i + 1;
                    break;
                }
            }
            if(-1!= nRult)
            {
                for(int j=nRult;j<12;j++)
                {
                    if (arrayCodeInfo[j].m_bRult)
                    {
                        nNextRult = j + 1;
                        break;
                    }
                }
            }
            if(-1==nRult)
            {
                nIdex = -1;
                strCode = "NG";
                nNextIndex = -1;
                return;
            }
            nIdex = nRult;
            strCode = arrayCodeInfo[nIdex - 1].szCode;
            nNextIndex = nNextRult;
        }
        public static bool GetAxisStation()
        {
            bool bClear = false;
            lock (m_RunLock)
            {
                bClear=_AxisRun;
            }
            return bClear;
        }
         public static void SetAxisRunStation(bool bAxisRun)
        {
            lock (m_RunLock)
            {
                _AxisRun = bAxisRun;
            }
        }

        public static bool GetRotateTableSignal()
        {
            bool bClear = false;
            lock(m_Clearlock)
            {
                bClear = _bClear;
            }
            return bClear;
        }
        public static void SetRotateTableSignal(bool bClear)
        {
            lock (m_Clearlock)
            {
                _bClear = bClear;
            }
        }
        public static void UpdateSpaceInfo()
        {
            lock(m_lock)
            {
                SpaceInfo sInfo = arraySpaceInfo.ElementAt(3);
                arraySpaceInfo[3] = arraySpaceInfo[2];
                arraySpaceInfo[2] = arraySpaceInfo[1];
                arraySpaceInfo[1] = arraySpaceInfo[0];
                arraySpaceInfo[0] = sInfo;
            }        
        }

        public static SpaceInfo GetSpaceInfo(int nIndex)
        {
            lock (m_lock)
            {
                return arraySpaceInfo.ElementAt(nIndex-1);
            }
        }
        public static void SetSpaceInfoUnloadbRult(bool bVaule, int nStation)
        {
            lock (m_Unloadlock)
            {
                arraySpaceInfo[nStation-1].m_bUnLoad= bVaule;
            }
        }
        public static void SetSpaceInfobRult(bool bVaule,int nStation,int nCub)
        {
            lock (m_lock)
            {
                arraySpaceInfo[nStation - 1].m_bRult[nCub-1] = bVaule;
            }
        }

        /// <summary>
        /// 设置穴位屏蔽
        /// </summary>
        /// <returns></returns>
        public static void SetCavityInfo(bool bVaule, int nStation, int nCub)
        {
            lock(m_BanCavity)
            {
                arraySpaceInfo[nStation - 1].m_bBanCavity[nCub - 1] = bVaule;
            }
        }
        public static void SetInfoCode(int nIndex,int nCodeIndex,string strCode)
        {
            arraySpaceInfo[nIndex - 1].strCode[nCodeIndex-1] = "";
            arraySpaceInfo[nIndex - 1].strCode[nCodeIndex-1] = strCode;
        }

        /// <summary>
        /// 获取前站处理结果
        /// 返回m_bRult的结果
        /// </summary>
        /// <returns></returns>
        public static bool GetSpaceInfobRult(int nIndex)
        {
            lock (m_lock)
            {
                return arraySpaceInfo[nIndex-1].m_bRult[0] && arraySpaceInfo[nIndex - 1].m_bRult[1];
            }
        }

        public static void SetSpacePressRult(int nSpce,int nCub,int nRult)
        {
            lock (m_lock)
            {
                arraySpaceInfo[nSpce - 1].m_nPress[nCub-1]= nRult;
            }
        }

        public static void ReadFerriteStyleFromXml(XmlDocument doc)
        {
            arrayFerriteStyle[0] = new CFerriteStyle("D32");
            arrayFerriteStyle[1] = new CFerriteStyle("D33");
            arrayFerriteStyle[2] = new CFerriteStyle("N84");
            XmlNodeList xnl = doc.SelectNodes("/SystemCfg/" + "FerriteStyle");
            if (xnl.Count > 0)
            {
                xnl = xnl.Item(0).ChildNodes;
                if (xnl.Count > 0)
                {
                    foreach (XmlNode xNot in xnl)
                    {
                        XmlElement xe = (XmlElement)xNot;
                        //XmlElement xe = (XmlElement)xer.Item(0);
                        string szName = xe.GetAttribute("FerriteName").Trim();
                        string szMark = xe.GetAttribute("识别字符").Trim();
                        string szStart = xe.GetAttribute("识别字符起始位").Trim();
                        string szLegth = xe.GetAttribute("识别字符长度").Trim();
                        string szHome = xe.GetAttribute("厂家").Trim();

                        if (0 == szName.CompareTo("D32"))
                        {
                            FerriteInfo info = new FerriteInfo();
                            info.nStart = Convert.ToInt32(szStart);
                            info.nLegth = Convert.ToInt32(szLegth);
                            info.strMark = szMark;
                            info.strHome = szHome;
                            arrayFerriteStyle[(int)FerriteMode.D32].AddNewMarkStyle(info);
                        }
                        else if (0 == szName.CompareTo("D33"))
                        {
                            FerriteInfo info = new FerriteInfo();
                            info.nStart = Convert.ToInt32(szStart);
                            info.nLegth = Convert.ToInt32(szLegth);
                            info.strMark = szMark;
                            info.strHome = szHome;
                            arrayFerriteStyle[(int)FerriteMode.D33].AddNewMarkStyle(info);
                        }
                        else if (0 == szName.CompareTo("N84"))
                        {
                            FerriteInfo info = new FerriteInfo();
                            info.nStart = Convert.ToInt32(szStart);
                            info.nLegth = Convert.ToInt32(szLegth);
                            info.strMark = szMark;
                            info.strHome = szHome;
                            arrayFerriteStyle[(int)FerriteMode.N84].AddNewMarkStyle(info);
                        }
                    }
                    //                XmlElement xe = (XmlElement
                }
            }
        }

        public static void ReadCfgFromXml(XmlDocument doc)
        {
            XmlNodeList xnl = doc.SelectNodes("/SystemCfg/" + "TablePlat");
            if (xnl.Count > 0)
            {
                xnl = xnl.Item(0).ChildNodes;
                if (xnl.Count > 0)
                {
                    XmlElement xe = (XmlElement)xnl.Item(0);

                    strHomeName = xe.GetAttribute("名称").Trim();
                    string strHome = xe.GetAttribute("原点工位").Trim();
                    string strHomeLimUp = xe.GetAttribute("工位上限").Trim();
                    string strHomeLimDowm = xe.GetAttribute("工位下线").Trim();

                    m_nHome = Convert.ToInt32(strHome);
                    m_nHomeLimUp = Convert.ToInt32(strHomeLimUp);
                    m_nHomeLimDown = Convert.ToInt32(strHomeLimDowm);
                    if(m_nHome> m_nHomeLimUp || m_nHome< m_nHomeLimDown)
                    {
                        m_nHome = -1;
                        m_nRotateTimes = -1;
                        return;
                    }
                    m_nRotateTimes = m_nHomeLimUp+m_nHomeLimDown - m_nHome;
                    //m_nRotateTimes = m_nHome - m_nHomeLimDown;
                    //if(0==m_nRotateTimes)
                    //{
                    //    m_nRotateTimes = m_nHomeLimUp;
                    //}
                }
            }
        }

        public static void SaveCfgXML(XmlDocument doc)
        {
            XmlNode xnl = doc.SelectSingleNode("SystemCfg");

            XmlNode root = xnl.SelectSingleNode("TablePlat");
            if (root == null)
            {
                root = doc.CreateElement("TablePlat");

                xnl.AppendChild(root);
            }

            root.RemoveAll();

            XmlElement child = doc.CreateElement("TablePlat");

            child.SetAttribute("名称", strHomeName);
            child.SetAttribute("原点工位", Convert.ToString(m_nHome));
            child.SetAttribute("工位上限", Convert.ToString(m_nHomeLimUp));
            child.SetAttribute("工位下线", Convert.ToString(m_nHomeLimDown));
            root.AppendChild(child);
        }
        
        public static void SaveData(int nCub,int nIndexLoad,string szFrriCode,string szCoilCode)
        {
            CsvOperation cs = new CsvOperation("InfoCode");
            string fileName = string.Concat(new string[]
            {
                SingletonTemplate<SystemMgr>.GetInstance().GetDataPath(""),
                "\\",
                "InfoCode",
                "_",
                DateTime.Now.ToString("yyyyMMdd"),
                ".csv"
            });
            if (!File.Exists(fileName))
            {
                File.Copy(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Data.csv", fileName);

            }
            cs[0, 0] = DateTime.Now.ToShortDateString();
            cs[0, 1] = DateTime.Now.ToString("HH:mm:ss");
            cs[0, 2] = nCub.ToString();
            cs[0, 3] = nIndexLoad.ToString();
            cs[0, 4] = szFrriCode;
            cs[0, 5] = szCoilCode;
            cs.Save();
        }

        public static void SaveForceData(float force1, float force2, float force3, float force4)
        {
            CsvOperation cs = new CsvOperation("ForceData");
            string fileName = string.Concat(new string[]
            {
                SingletonTemplate<SystemMgr>.GetInstance().GetDataPath(""),
                "\\",
                "ForceData",
                "_",
                DateTime.Now.ToString("yyyyMMdd"),
                ".csv"
            });
            if (!File.Exists(fileName))
            {
                File.Copy(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Pressure.csv", fileName);

            }
            cs[0, 0] = DateTime.Now.ToShortDateString();
            cs[0, 1] = DateTime.Now.ToString("HH:mm:ss");
            cs[0, 2] = force1.ToString("F3");
            cs[0, 3] = force2.ToString("F3");
            cs[0, 4] = force3.ToString("F3");
            cs[0, 5] = force4.ToString("F3");
            cs.Save();
        }

        public static void CoilLink(out string szInfoCode, out string szInfo, string szCoilCode, string szId, string LineId, string szFerriteCode)
        {
            szInfoCode = "-998";
            szInfo = "链接失败";
            string strLinkPath = SystemMgr.GetInstance().GetParamString("LinkPath");
            try
            {
                string strConnection = String.Format("data source={0};initial catalog=MESDB; user id=dataquery; password=querydata", strLinkPath);
                SqlConnection conn = new SqlConnection(strConnection);
                conn.Open();
                string strSql = "declare @strmsgid varchar(70),@strmsgText varchar(70) set @strmsgid='' set @strmsgText=''  execute [dbo].[m_CheckP60AssemblyPPID_P] '" + szCoilCode + "','" + szId + "','" + LineId + "','" + szFerriteCode + "',@strmsgid output ,@strmsgText output select @strmsgid as strmsgid,@strmsgText as strmsgText";
                SqlCommand comm = new SqlCommand(strSql, conn);
                SqlDataReader DataReader = comm.ExecuteReader();
                while (DataReader.Read())
                {
                    szInfoCode = DataReader["strmsgid"].ToString();
                    szInfo = DataReader["strmsgText"].ToString();
                }
                DataReader.Close();
                conn.Close();
            }
            catch (Exception Err)
            {
                szInfoCode = "-999";
                szInfo = Err.ToString();
            }
        }

        public static bool 卡关(out string strMsg, string CoilCode, bool Npass = true)
        {
            lock (m_PdcaLock)
            {

            string strLinkPath = SystemMgr.GetInstance().GetParamString("LinkPath");


            strMsg = "-1";

            string strConnection =String.Format("data source={0};initial catalog=MESDB; user id=dataquery; password=querydata", strLinkPath);
            SqlConnection conn = new SqlConnection(strConnection);
            conn.Open();
            string strSql;
            string strStationID;
            if (Npass == true)
            {
                strStationID = SystemMgr.GetInstance().GetParamString("StationID");
                //strSql = "select* from m_TestResult_t with(nolock)where stationid = '" + SystemMgr.GetInstance().GetParamString("Station") + "' and result = 'PASS' and Ppid = '" + FerriteCode + "'";
                strSql = "select* from m_TestResult_t with(nolock)where stationid = '"+strStationID+"' and result = 'PASS' and ppid = '" + CoilCode + "'";
            }
            else
            {
                strStationID = SystemMgr.GetInstance().GetParamString("StationID");
                //strSql = "select* from m_TestResult_t with(nolock)where stationid = '" + SystemMgr.GetInstance().GetParamString("Station") + "'and Ppid = '" + FerriteCode + "'";
                strSql = "select* from m_TestResult_t with(nolock)where stationid =  '" + strStationID + "'and Ppid = '" + CoilCode + "'";
            }
            SqlCommand comm = new SqlCommand(strSql, conn);
            SqlDataReader DataReader = comm.ExecuteReader();
            while (DataReader.Read())
            {
                strMsg = DataReader[2].ToString();
            }
            DataReader.Close();
            conn.Close();
            if (strMsg == "PASS")
            {
                return true;
            }
            else
            {
                return false;
            }
            }
        }
    }
}
