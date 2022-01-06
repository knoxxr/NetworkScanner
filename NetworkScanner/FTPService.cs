using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NetworkScanner
{
    public class FTPService
    {
        private IPAddress hostIP;
        private int port;
        private string iD;
        private string pW;

        public IPAddress HostIP { get => hostIP; set => hostIP = value; }
        public int Port { get => port; set => port = value; }
        public string ID { get => iD; set => iD = value; }
        public string PW { get => pW; set => pW = value; }

        int euckrCodePage = 51949; // euc-kr 코드 번호
        public FTPService()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }
        public FtpWebResponse Connect(String url, string method, Action<FtpWebRequest> action = null)
        {
            var request = WebRequest.Create(url) as FtpWebRequest;
            request.UseBinary = true;
            request.Method = method;
            request.Credentials = new NetworkCredential(ID, PW);
            if (action != null)
            {
                action(request);
            }
            return request.GetResponse() as FtpWebResponse;
        }

        public void UploadFileList( string path, string filename)
        {
            string ftpPath = String.Format("ftp://{0}/FTP/{1}", HostIP, filename);
            string inputFile = filename;

            FtpWebRequest req = (FtpWebRequest)WebRequest.Create(ftpPath);
            req.Method = WebRequestMethods.Ftp.UploadFile;
            req.Credentials = new NetworkCredential(ID, PW);

            byte[] data;
            using (StreamReader reader = new StreamReader(path+inputFile))
            {
                data = Encoding.GetEncoding(euckrCodePage).GetBytes(reader.ReadToEnd());
            }

            req.ContentLength = data.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
            }

            using (FtpWebResponse resp = (FtpWebResponse)req.GetResponse())
            {
                Console.WriteLine("Upload: {0}", resp.StatusDescription);
            }
        }
    }
}
