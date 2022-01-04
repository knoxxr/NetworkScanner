using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkScanner
{
    public class OUIInfo
    {
        Dictionary<string,string> _OUIInfo = new Dictionary<string,string>();
        public OUIInfo()
        {

        }

        public void LoadInfo()
        {
            string[] sContents = File.ReadAllLines("ouiinfo.ini", Encoding.Default);

            foreach(string s in sContents)
            {
                if(s.Contains("(hex)"))
                {
                    string filter= s.Replace("(hex)", ",");
                    string[] token = filter.Split(',');
                    _OUIInfo.Add(token[0].Trim(), token[1].Trim());   
                }
            }
        }

        public string GetVender(string mac)
        {
            if (IsValidMac(mac))
            {
                string oui = mac.Substring(0, 8);

                if (_OUIInfo.ContainsKey(oui))
                {
                    return _OUIInfo[oui];
                }
                else
                    return "";
            }

            return "";
        }

        public bool IsValidMac(string mac)
        {
            if (mac.Length == 0)
                return false;
            else
                return true;
        }


    }
}
