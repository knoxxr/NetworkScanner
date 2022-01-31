using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OUIConvertor
{
    internal class WebOUIConvertor
    {
        public delegate void deleProgressChanged(int value);
        public event deleProgressChanged ProgressChanged;

        public delegate void deleProgressCompleted(int value);
        public event deleProgressCompleted ProgressCompleted;

        List<OUIInfo> OUTInfors = new List<OUIInfo>();
        string ConnectString = @"220.94.240.240:33060";

        string URLPath = @"http://standards-oui.ieee.org/oui/oui.txt";

        WebClient client = new WebClient();

        public struct OUIInfo
        {
            public string Mac;
            public string Organization;
            public string ComponyID;
            public string Compony_Organization;
            public string Address;
            public string Contury;
        }

        public WebOUIConvertor()
        {
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
        }

        private void Client_DownloadFileCompleted(object? sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (ProgressCompleted!= null)
            {
                ProgressCompleted(OUTInfors.Count);
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if(ProgressChanged!= null)
            {
                ProgressChanged(e.ProgressPercentage);
            }
        }

        public void Initialize()
        {
            GetRawOUI();
        }
        public void GetRawOUI()
        {
            OUTInfors.Clear();
            Stream stream = client.OpenRead(URLPath);
            using (StreamReader reader = new StreamReader(stream))
            {
                while (reader.EndOfStream == false)
                {
                    string readText = reader.ReadLine();

                    if (string.IsNullOrEmpty(readText)) continue;
                    if (readText.Contains("(hex)"))
                    {
                        string oui_organ = readText;
                        string company_organ = reader.ReadLine();
                        string addr = reader.ReadLine() + reader.ReadLine();
                        string contury = reader.ReadLine();
                        AddOUIInfo(oui_organ, company_organ, addr, contury);
                    }
                }
            }
            if (ProgressCompleted != null)
            {
                ProgressCompleted(OUTInfors.Count);
            }
        }

        public void AddOUIInfo(string oui_organ, string company_organ, string addr, string contury)
        {
            OUIInfo info = new OUIInfo();

            string[] tokens_01 = oui_organ.Split("(hex)");

            info.Mac = tokens_01[0].Trim();
            info.Organization = tokens_01[1].Trim();

            string[] tokens_02 = company_organ.Split("(base 16)");
            info.ComponyID = tokens_02[0].Trim();
            info.Compony_Organization = tokens_02[1].Trim();

            info.Address = addr.Replace("\t\t\t\t", " ").Trim();
            info.Contury = contury.Trim();

            OUTInfors.Add(info);
        }

        public void UpdateDatabase()
        {

        }
    }
}
