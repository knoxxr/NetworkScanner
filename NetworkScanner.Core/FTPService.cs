using System;
using System.IO;
using System.Net;
using System.Text;
using FluentFTP;

namespace NetworkScanner
{
    public class FTPService
    {
        // 플랫폼 종속적인 로깅에 직접 의존하지 않기 위한 선택적 오류 콜백.
        public static Action<string>? OnError { get; set; }

        private IPAddress hostIP;
        private int port;
        private string iD;
        private string pW;
        public IPAddress HostIP { get => hostIP; set => hostIP = value; }
        public int Port { get => port; set => port = value; }
        public string ID { get => iD; set => iD = value; }
        public string PW { get => pW; set => pW = value; }

        const int EuckrCodePage = 51949; // euc-kr 코드 번호

        public FTPService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private FtpClient CreateClient()
        {
            // Port가 지정되지 않은 경우(0) FluentFTP가 FTP 기본 포트(21)를 사용한다.
            return new FtpClient(HostIP?.ToString(), ID, PW, Port);
        }

        public void UploadFileList(string path, string filename)
        {
            try
            {
                byte[] data;
                using (StreamReader reader = new StreamReader(path + filename))
                {
                    data = Encoding.GetEncoding(EuckrCodePage).GetBytes(reader.ReadToEnd());
                }

                using FtpClient client = CreateClient();
                client.Connect();
                client.UploadBytes(data, $"/FTP/{filename}", FtpRemoteExists.Overwrite, createRemoteDir: true);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public void DownloadFileList(string path, string filename, string outputfilename)
        {
            try
            {
                using FtpClient client = CreateClient();
                client.Connect();
                client.DownloadBytes(out byte[] data, $"/FTP/{filename}");
                File.WriteAllBytes(outputfilename, data);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }
    }
}
