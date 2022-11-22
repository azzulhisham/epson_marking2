using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.IO;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;


namespace Marking2
{
    /// <summary>
    /// Service generate Upper Marking Code for Automotive PX. 
    /// </summary>
    [WebService(Namespace = "http://az_zulhisham.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class MarkingCode : System.Web.Services.WebService
    {
        //public const string _IMI_Path = @"D:\MachineNet\MacDB\Marking\ALineIMI";
        public const string _IMI_Path = @"C:\tmp\Test";

        [WebMethod(EnableSession = true)]
        public string AboutMe()
        {
            return "Service generate Upper Marking Code for Automotive PX.";
        }

        [WebMethod(EnableSession=true)]
        public string GetMarkingCode(string LotNo, string SpecFile, string MarkingDate)
        {
            string[] serialCode1 = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L", "M", "N", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
            string[] serialCode2 = { "P", "Q", "R", "S", "2", "3", "4", "5", "6", "7", "8", "9" };
            string ret = "-----";

            string firstSeq = serialCode1[0] + serialCode2[0];
            string sfPath = string.Format("{0}.dat",Path.Combine(_IMI_Path, SpecFile));


            if (File.Exists(sfPath))
            {
                SpecFile sf = new SpecFile();
                DataModel dm = new DataModel();
                List<MarkingRec> mr = new List<MarkingRec>();

                //MarkingDate = '2022-11-18 16:00:00'
                DateTime _today = new DateTime(Convert.ToInt32(MarkingDate.Substring(0, 4)),
                                               Convert.ToInt32(MarkingDate.Substring(5, 2)),
                                               Convert.ToInt32(MarkingDate.Substring(8, 2)),
                                               Convert.ToInt32(MarkingDate.Substring(11, 2)),
                                               Convert.ToInt32(MarkingDate.Substring(14, 2)),
                                               Convert.ToInt32(MarkingDate.Substring(17, 2)));

                string[] fc = File.ReadAllLines(sfPath);

                if (fc.Length > 0)
                {
                    foreach (string item in fc)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            string[] items = item.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            switch (items[1].Trim().ToUpper())
                            {
                                case "L001":
                                    sf.a01_Freq = items[2].Replace(".", "").Trim().ToUpper();
                                    break;

                                case "L002":
                                    sf.a02_Plant = items[2].Trim().ToUpper();
                                    break;

                                case "L003":
                                    sf.a03_ProdCode = items[2].Trim().ToUpper();
                                    break;

                                case "L004":
                                    sf.a04_Version = items[2].Trim().ToUpper();
                                    break;

                                case "L005":
                                    sf.a05_DateFormat = items[2].Trim().ToUpper();
                                    break;

                                case "L006":
                                    sf.a06_Parameter = items[2].Trim().ToUpper();
                                    break;

                                case "L007":
                                    sf.a07_Format = items[2].Trim().ToUpper();
                                    break;

                                default:
                                    break;
                            }

                        }
                    }

                    if (sf.a02_Plant.EndsWith("@@"))
                    {
                        string qry = string.Format("SELECT IMI_No, Mdata1, Mdata2, Lot_No, RecDate, Remark " +
                                                    "FROM SequenceRec " +
                                                    "WHERE Lot_No=\'{0}\' " +
                                                    "ORDER BY RecDate DESC",
                                                    LotNo
                                                    );

                        int cnt = dm.Ms_SqlQry(qry, mr, true);

                        if (cnt > 0 && mr.FirstOrDefault().a06_Remark != "1")
                        {
                            if (mr.FirstOrDefault().a05_RecDate.Date != _today.Date)
                            {
                                //get new marking sequence
                                cnt = 0;
                            }
                        }
                        else
                        {
                            if (cnt > 0)
                            {
                                _today = mr.FirstOrDefault().a05_RecDate;
                            }
                        }

                        if (cnt > 0)
                        {
                            ret = mr[0].a02_MData1.Trim();

                            Regex regex = new Regex("[^a-zA-Z0-9_.!]");
                            Match match = regex.Match(ret);

                            if (match.Length > 0)
                            {
                                ret = "-----";
                                qry = string.Format("DELETE " +
                                                    "FROM SequenceRec " +
                                                    "WHERE IMI_No=\'{0}\' AND Mdata1 like \'%{1}\'",
                                                    SpecFile, match.Value
                                                    );

                                int delCnt = dm.Ms_SqlQry(qry);


                                qry = string.Format("SELECT IMI_No, Mdata1, Mdata2, Lot_No " +
                                                            "FROM SequenceRec " +
                                                            "WHERE IMI_No=\'{0}\' AND cast(datediff(dd,0,recdate) as datetime)=\'{1}\' and not mdata1 like '%@' " +
                                                            "ORDER BY recdate DESC",
                                                            SpecFile,
                                                            string.Format("{0:D4}-{1:D2}-{2:D2}", _today.Year, _today.Month, _today.Day));

                                if (mr.Count > 0) mr.Clear();
                                cnt = dm.Ms_SqlQry(qry, mr);

                                if (cnt > 0)
                                {
                                    int fr = sf.a02_Plant.Count(n => n == '#');
                                    int SerialChar = sf.a02_Plant.Length - fr;

                                    string serialCode = mr[0].a02_MData1.Substring(mr[0].a02_MData1.Length - SerialChar);
                                    string lastSerialCode = mr[mr.Count - 1].a02_MData1.Substring(mr[mr.Count - 1].a02_MData1.Length - SerialChar);

                                    int sc1no = Array.IndexOf(serialCode1, serialCode.Substring(0, 1));
                                    int sc2no = Array.IndexOf(serialCode2, serialCode.Substring(1, 1));

                                    //added by Zulhisham Tan on 30/05/2021 - To ensure the number of lot is equal to the sequence number, remove lot at cut-off time
                                    qry = "SELECT IMI_No, Mdata1, Mdata2, Lot_No " +
                                                            "FROM SequenceRec " +
                                                            "WHERE IMI_No=\'{0}\' AND cast(datediff(dd,0,recdate) as datetime)=\'{1}\' and not mdata1 like '%@' and not Lot_No in ({2}) " +
                                                            "ORDER BY recdate DESC";

                                    string lotException = "";

                                    while (lastSerialCode.ToUpper() != firstSeq)
                                    {
                                        if (string.IsNullOrEmpty(lotException))
                                        {
                                            lotException += "\'" + mr[mr.Count - 1].a04_LotNo + "\'";
                                        }
                                        else
                                        {
                                            lotException += ", \'" + mr[mr.Count - 1].a04_LotNo + "\'";
                                        }

                                        string qryExcld = string.Format(qry,
                                                            SpecFile,
                                                            string.Format("{0:D4}-{1:D2}-{2:D2}", _today.Year, _today.Month, _today.Day),
                                                            lotException);

                                        if (mr.Count > 0) mr.Clear();
                                        cnt = dm.Ms_SqlQry(qryExcld, mr);

                                        if (cnt <= 0)
                                        {
                                            break;
                                        }

                                        lastSerialCode = mr[mr.Count - 1].a02_MData1.Substring(mr[mr.Count - 1].a02_MData1.Length - SerialChar);

                                        if (lastSerialCode.ToUpper() == firstSeq)
                                        {
                                            break;
                                        }

                                        serialCode = mr[0].a02_MData1.Substring(mr[0].a02_MData1.Length - SerialChar);

                                        sc1no = Array.IndexOf(serialCode1, serialCode.Substring(0, 1));
                                        sc2no = Array.IndexOf(serialCode2, serialCode.Substring(1, 1));
                                    }

                                    if (cnt <= 0)
                                    {
                                        int serialCodeNo = 0;
                                        int serialCode1Idx = (serialCodeNo % serialCode1.Length);
                                        int serialCode2Idx = (int)(serialCodeNo / serialCode1.Length);

                                        if (serialCode1Idx < 0)
                                        {
                                            serialCode1Idx = 0;
                                        }

                                        if (serialCode2Idx >= serialCode2.Length)
                                        {
                                            serialCode2Idx = 0;
                                        }

                                        ret = sf.a01_Freq.Substring(0, fr) +
                                                serialCode1[serialCode1Idx] +
                                                serialCode2[serialCode2Idx];
                                    }
                                    else
                                    {
                                        //once confirm started with first sequence then reconfirm every sequence
                                        //it going to ignore jumping sequence
                                        int seqCnt = 0;
                                        int seqFail = 0;

                                        for (int i = mr.Count - 1; i >= 0; i--)
                                        {
                                            int serialCode1IdxTmp = (seqCnt % serialCode1.Length);
                                            int serialCode2IdxTmp = (int)(seqCnt / serialCode1.Length);

                                            string dataSerialCode = mr[i].a02_MData1.Substring(mr[i].a02_MData1.Length - SerialChar);
                                            string expectedSerialCode = serialCode1[serialCode1IdxTmp] + serialCode2[serialCode2IdxTmp];

                                            if (dataSerialCode == expectedSerialCode)
                                            {
                                                sc1no = Array.IndexOf(serialCode1, dataSerialCode.Substring(0, 1));
                                                sc2no = Array.IndexOf(serialCode2, dataSerialCode.Substring(1, 1));

                                                seqCnt += 1;
                                            }
                                            else
                                            {
                                                //allow jump 1 sequence : 1P, 2P, 3P, 5P, 4P, 6P
                                                //otherwise return fail
                                                int _sc1no = Array.IndexOf(serialCode1, dataSerialCode.Substring(0, 1));
                                                int _sc2no = Array.IndexOf(serialCode2, dataSerialCode.Substring(1, 1));

                                                int _seqCnt = _sc2no * serialCode1.Length + _sc1no;
                                                seqFail += seqCnt - _seqCnt;

                                                if (seqFail <= 1 && seqFail >= -1)
                                                {
                                                    sc1no = Array.IndexOf(serialCode1, expectedSerialCode.Substring(0, 1));
                                                    sc2no = Array.IndexOf(serialCode2, expectedSerialCode.Substring(1, 1));

                                                    seqCnt += 1;
                                                }
                                                else
                                                {
                                                    seqFail = 0;
                                                }

                                            }
                                        }

                                        int serialCodeNo = (sc2no * serialCode1.Length) + sc1no + 1;
                                        int serialCode1Idx = (serialCodeNo % serialCode1.Length);
                                        int serialCode2Idx = (int)(serialCodeNo / serialCode1.Length);

                                        if (serialCode1Idx < 0)
                                        {
                                            serialCode1Idx = 0;
                                        }

                                        if (serialCode2Idx >= serialCode2.Length)
                                        {
                                            serialCode2Idx = 0;
                                        }

                                        ret = sf.a01_Freq.Substring(0, fr) +
                                                serialCode1[serialCode1Idx] +
                                                serialCode2[serialCode2Idx];

                                        if (seqFail != 0)
                                        {
                                            ret = "-----";
                                        }
                                    }
                                }
                                else
                                {
                                    int serialCodeNo = 0;
                                    int fr = sf.a02_Plant.Count(n => n == '#');

                                    int serialCode1Idx = (serialCodeNo % serialCode1.Length);
                                    int serialCode2Idx = (int)(serialCodeNo / serialCode1.Length);

                                    if (serialCode1Idx < 0)
                                    {
                                        serialCode1Idx = 0;
                                    }

                                    if (serialCode2Idx >= serialCode2.Length)
                                    {
                                        serialCode2Idx = 0;
                                    }

                                    ret = sf.a01_Freq.Substring(0, fr) +
                                            serialCode1[serialCode1Idx] +
                                            serialCode2[serialCode2Idx];
                                }
                            }
                        }
                        else
                        {
                            qry = string.Format("SELECT IMI_No, Mdata1, Mdata2, Lot_No " +
                                                        "FROM SequenceRec " +
                                                        "WHERE IMI_No=\'{0}\' AND cast(datediff(dd,0,recdate) as datetime)=\'{1}\' and not mdata1 like '%@' " +
                                                        "ORDER BY recdate DESC", 
                                                        SpecFile,
                                                        string.Format("{0:D4}-{1:D2}-{2:D2}", _today.Year, _today.Month, _today.Day));

                            if (mr.Count > 0) mr.Clear();
                            cnt = dm.Ms_SqlQry(qry, mr);

                            if (cnt > 0)
                            {
                                int fr = sf.a02_Plant.Count(n => n == '#');
                                int SerialChar = sf.a02_Plant.Length - fr;

                                string serialCode = mr[0].a02_MData1.Substring(mr[0].a02_MData1.Length - SerialChar);
                                string lastSerialCode = mr[mr.Count - 1].a02_MData1.Substring(mr[mr.Count - 1].a02_MData1.Length - SerialChar);

                                int sc1no = Array.IndexOf(serialCode1, serialCode.Substring(0, 1));
                                int sc2no = Array.IndexOf(serialCode2, serialCode.Substring(1, 1));

                                //added by Zulhisham Tan on 30/05/2021 - To ensure the number of lot is equal to the sequence number, remove lot at cut-off time
                                qry = "SELECT IMI_No, Mdata1, Mdata2, Lot_No " +
                                                        "FROM SequenceRec " +
                                                        "WHERE IMI_No=\'{0}\' AND cast(datediff(dd,0,recdate) as datetime)=\'{1}\' and not mdata1 like '%@' and not Lot_No in ({2}) " +
                                                        "ORDER BY recdate DESC";

                                string lotException = "";

                                while (lastSerialCode.ToUpper() != firstSeq)
                                {
                                    if (string.IsNullOrEmpty(lotException))
                                    {
                                        lotException += "\'" + mr[mr.Count - 1].a04_LotNo + "\'";
                                    }
                                    else
                                    {
                                        lotException += ", \'" + mr[mr.Count - 1].a04_LotNo + "\'";
                                    }

                                    string qryExcld = string.Format(qry,
                                                        SpecFile,
                                                        string.Format("{0:D4}-{1:D2}-{2:D2}", _today.Year, _today.Month, _today.Day),
                                                        lotException);

                                    if (mr.Count > 0) mr.Clear();
                                    cnt = dm.Ms_SqlQry(qryExcld, mr);

                                    if (cnt <= 0)
                                    {
                                        break;
                                    }

                                    lastSerialCode = mr[mr.Count - 1].a02_MData1.Substring(mr[mr.Count - 1].a02_MData1.Length - SerialChar);

                                    if (lastSerialCode.ToUpper() == firstSeq)
                                    {
                                        break;
                                    }

                                    serialCode = mr[0].a02_MData1.Substring(mr[0].a02_MData1.Length - SerialChar);

                                    sc1no = Array.IndexOf(serialCode1, serialCode.Substring(0, 1));
                                    sc2no = Array.IndexOf(serialCode2, serialCode.Substring(1, 1));
                                }

                                if (cnt <= 0)
                                {
                                    int serialCodeNo = 0;

                                    int serialCode1Idx = (serialCodeNo % serialCode1.Length);
                                    int serialCode2Idx = (int)(serialCodeNo / serialCode1.Length);

                                    if (serialCode1Idx < 0)
                                    {
                                        serialCode1Idx = 0;
                                    }

                                    if (serialCode2Idx >= serialCode2.Length)
                                    {
                                        serialCode2Idx = 0;
                                    }

                                    ret = sf.a01_Freq.Substring(0, fr) +
                                            serialCode1[serialCode1Idx] +
                                            serialCode2[serialCode2Idx];
                                }
                                else
                                {
                                    //once confirm started with first sequence then reconfirm every sequence
                                    //it going to ignore jumping sequence
                                    int seqCnt = 0;
                                    int seqFail = 0;

                                    for (int i = mr.Count - 1; i >= 0; i--)
                                    {
                                        int serialCode1IdxTmp = (seqCnt % serialCode1.Length);
                                        int serialCode2IdxTmp = (int)(seqCnt / serialCode1.Length);

                                        string dataSerialCode = mr[i].a02_MData1.Substring(mr[i].a02_MData1.Length - SerialChar);
                                        string expectedSerialCode = serialCode1[serialCode1IdxTmp] + serialCode2[serialCode2IdxTmp];

                                        if (dataSerialCode == expectedSerialCode)
                                        {
                                            sc1no = Array.IndexOf(serialCode1, dataSerialCode.Substring(0, 1));
                                            sc2no = Array.IndexOf(serialCode2, dataSerialCode.Substring(1, 1));

                                            seqCnt += 1;
                                        }
                                        else
                                        {
                                            //allow jump 1 sequence : 1P, 2P, 3P, 5P, 4P, 6P
                                            //otherwise return fail
                                            int _sc1no = Array.IndexOf(serialCode1, dataSerialCode.Substring(0, 1));
                                            int _sc2no = Array.IndexOf(serialCode2, dataSerialCode.Substring(1, 1));

                                            int _seqCnt = _sc2no * serialCode1.Length + _sc1no;
                                            seqFail += seqCnt - _seqCnt;

                                            if (seqFail <= 1 && seqFail >= -1)
                                            {
                                                sc1no = Array.IndexOf(serialCode1, expectedSerialCode.Substring(0, 1));
                                                sc2no = Array.IndexOf(serialCode2, expectedSerialCode.Substring(1, 1));

                                                seqCnt += 1;
                                            }
                                            else
                                            {
                                                seqFail = 0;
                                            }

                                        }
                                    }

                                    int serialCodeNo = (sc2no * serialCode1.Length) + sc1no + 1;
                                    int serialCode1Idx = (serialCodeNo % serialCode1.Length);
                                    int serialCode2Idx = (int)(serialCodeNo / serialCode1.Length);

                                    if (serialCode1Idx < 0)
                                    {
                                        serialCode1Idx = 0;
                                    }

                                    if (serialCode2Idx >= serialCode2.Length)
                                    {
                                        serialCode2Idx = 0;
                                    }

                                    ret = sf.a01_Freq.Substring(0, fr) +
                                            serialCode1[serialCode1Idx] +
                                            serialCode2[serialCode2Idx];

                                    if (seqFail != 0)
                                    {
                                        ret = "-----";
                                    }
                                }
                            }
                            else
                            {
                                int serialCodeNo = 0;
                                int fr = sf.a02_Plant.Count(n => n == '#');

                                int serialCode1Idx = (serialCodeNo % serialCode1.Length);
                                int serialCode2Idx = (int)(serialCodeNo / serialCode1.Length);

                                if (serialCode1Idx < 0)
                                {
                                    serialCode1Idx = 0;
                                }

                                if (serialCode2Idx >= serialCode2.Length)
                                {
                                    serialCode2Idx = 0;
                                }

                                ret = sf.a01_Freq.Substring(0, fr) +
                                        serialCode1[serialCode1Idx] +
                                        serialCode2[serialCode2Idx];
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(ret) && ret != "-----")
                {
                    string qry = "IF NOT EXISTS (SELECT * FROM SequenceRec WHERE Lot_No='{0}' AND cast(datediff(dd,0,RecDate) as datetime)='{3}') " +
                                    "INSERT INTO SequenceRec VALUES ('{0}', '{1}', '{4}', '{2}', '-', '0', '-')";

                    qry = string.Format(qry, LotNo, SpecFile, ret,
                        string.Format("{0:D4}-{1:D2}-{2:D2}", _today.Year, _today.Month, _today.Day),
                        string.Format("{0:D4}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", _today.Year, _today.Month, _today.Day, _today.Hour, _today.Minute, _today.Second));
                    dm.Ms_SqlQry(qry);
                }
            }

            return ret;
        }

        [WebMethod(EnableSession = true)]
        public string SetSequenceMarked(string LotNo, string SpecFile, string MarkingCode1)
        {
            DataModel dm = new DataModel();

            string qry = "IF EXISTS (SELECT * FROM SequenceRec WHERE Lot_No='{0}' AND MData1='{1}') " +
                "UPDATE SequenceRec SET Remark='1'  WHERE Lot_No='{0}' AND MData1='{1}'";

            qry = string.Format(qry, LotNo, MarkingCode1);
            int result = dm.Ms_SqlQry(qry);

            return result.ToString();
        }

        [WebMethod(EnableSession = true)]
        public string GetMarkingSequenceFC(string format, string seqNo, string option1, string option2)
        {
            string[] serialCode1 = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L", "M", "N", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
            string[] serialCode2 = { "P", "Q", "R", "S", "2", "3", "4", "5", "6", "7", "8", "9" };

            int seqNumber = 0;
            bool convertSequenceNum = int.TryParse(seqNo, out seqNumber);

            int maxSeq = serialCode1.Length * serialCode2.Length;

            if (!string.IsNullOrEmpty(option1) && !string.IsNullOrEmpty(option2))
            {
                maxSeq = option1.Length * option2.Length;
            }

            if (!string.IsNullOrEmpty(option1) && string.IsNullOrEmpty(option2))
            {
                maxSeq = option1.Length;
            }

            if (string.IsNullOrEmpty(option1) && !string.IsNullOrEmpty(option2))
            {
                maxSeq = option2.Length;
            }

            if (convertSequenceNum && seqNumber > 0 && seqNumber <= maxSeq)
            {
                seqNumber -= 1;

                try
                {
                    if (string.IsNullOrEmpty(option1) && string.IsNullOrEmpty(option2))
                    {
                        int serialCode1Idx = (seqNumber % serialCode1.Length);
                        int serialCode2Idx = (int)(seqNumber / serialCode1.Length);

                        return string.Format(format, serialCode1[serialCode1Idx], serialCode2[serialCode2Idx]);
                    }
                    else if (!string.IsNullOrEmpty(option1) && !string.IsNullOrEmpty(option2))
                    {
                        int serialCode1Idx = (seqNumber % option1.Length);
                        int serialCode2Idx = (int)(seqNumber / option2.Length);

                        return string.Format(format, option1.Substring(serialCode1Idx, 1), option2.Substring(serialCode2Idx, 1));
                    }
                    else if (!string.IsNullOrEmpty(option1) && string.IsNullOrEmpty(option2))
                    {
                        return string.Format(format, option1.Substring(seqNumber, 1));
                    }
                    else if (string.IsNullOrEmpty(option1) && !string.IsNullOrEmpty(option2))
                    {
                        return string.Format(format, option2.Substring(seqNumber, 1));
                    }

                    return "!!!";

                }
                catch (Exception ex)
                {
                    return "!!!";
                }
            }
            else
            {
                return "!!!";
            }
        }
    }
}