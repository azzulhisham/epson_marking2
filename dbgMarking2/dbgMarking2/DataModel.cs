﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading;
using System.IO;
using System.Data;


namespace WindowsFormsApplication1
{
    public class SpecFile
    {
        public string a01_Freq { get; set; }
        public string a02_Plant { get; set; }
        public string a03_ProdCode { get; set; }
        public string a04_Version { get; set; }
        public string a05_DateFormat { get; set; }
        public string a06_Parameter { get; set; }
        public string a07_Format { get; set; }
    }

    public class MarkingRec
    {
        public string a01_IMI { get; set; }
        public string a02_MData1 { get; set; }
        public string a03_MData2 { get; set; }
        public string a04_LotNo { get; set; }

        public DateTime a05_RecDate { get; set; }
        public string a06_Remark { get; set; }
        public string a07_Note { get; set; }
    }

    public class DataModel
    {
        static string GetConnString()
        {
            //string sConnStr =
            //        "Server=" + @"172.16.59.254\SQLEXPRESS" + "; " +
            //        "DataBase=" + "Marking" + "; " +
            //        "user id=" + "vb-sql" + ";" +
            //        "password=" + "Anyn0m0us";

            string sConnStr =
"Server=" + @"DESKTOP-TLVFD7V\SQLEXPRESS" + "; " +
"DataBase=" + "Marking" + "; " +
"user id=" + "sa" + ";" +
"password=" + "Az@HoePinc0615";

            return sConnStr;
        }

        public int Ms_SqlQry(string Qry, List<MarkingRec> rec, bool extendQuery = false)
        {
            int _ret = 0;
            string sConnStr = GetConnString();

            SqlConnection dbConnection = new SqlConnection(sConnStr);
            string _qry = Qry;


            try
            {
                dbConnection.Open();
                SqlCommand _qrycmd = new SqlCommand(_qry, dbConnection);
                //_qrycmd.ExecuteNonQuery();

                SqlDataReader Reader = _qrycmd.ExecuteReader();

                if (Reader.HasRows)
                {
                    while (Reader.Read())
                    {
                        _ret++;

                        rec.Add(new MarkingRec
                        {
                            a01_IMI = Reader.GetString(0),
                            a02_MData1 = Reader.GetString(1),
                            a03_MData2 = Reader.GetString(2),
                            a04_LotNo = Reader.GetString(3),
                            a05_RecDate = extendQuery ? Reader.GetDateTime(4) : DateTime.Now,
                            a06_Remark = extendQuery ? Reader.GetString(5) : string.Empty
                        });
                    }
                }

            }
            catch (Exception Ex)
            {
                string msg = Ex.Message;
                _ret = -1;
            }
            finally
            {
                dbConnection.Close();
            }

            return _ret;
        }

        public int Ms_SqlQry(string Qry)
        {
            int _ret = 0;
            string sConnStr = GetConnString();

            SqlConnection dbConnection = new SqlConnection(sConnStr);
            string _qry = Qry;


            try
            {
                dbConnection.Open();
                SqlCommand _qrycmd = new SqlCommand(_qry, dbConnection);
                //_qrycmd.ExecuteNonQuery();

                SqlDataReader Reader = _qrycmd.ExecuteReader();
                _ret = Reader.RecordsAffected;
            }
            catch (Exception Ex)
            {
                string msg = Ex.Message;
                _ret = -1;
            }
            finally
            {
                dbConnection.Close();
            }

            return _ret;
        }

    }
}
