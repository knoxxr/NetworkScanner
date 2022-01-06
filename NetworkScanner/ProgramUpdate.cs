using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NetworkScanner
{
    internal class ProgramUpdate
    {
        public static string CheckCurVersion()
        {
            string remoteVersion;
            var filename = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(filename).FileVersion;

            return version;
        }

        public static string CheckLastestVersion()
        {
            string remoteVersion;
            var filename = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(filename).FileVersion;

            //download file from ftp
            string lastestver = "";

            FTPService ftp = new FTPService();
            ftp.DownloadFileList("", "/version/version.txt", "version.txt");

            //file open & read

            return lastestver;
        }



        private static void Update()
        {
            Process.Start("powershell.exe",
        @"-COMMAND DEL SELF_UPDATE_TUTORIAL.EXE
Invoke-WebRequest -Uri ""http://honsal.dyndns.info:8008/SELF_UPDATE_TUTORIAL.exe"" -OutFile ""SELF_UPDATE_TUTORIAL.EXE""");
            Environment.Exit(0);
        }
    }
}
