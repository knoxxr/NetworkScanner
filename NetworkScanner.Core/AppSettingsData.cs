namespace NetworkScanner
{
    // UCSetting(WPF)/SettingsView(Avalonia)가 화면에 표시·편집하는 설정값을 담는 순수 데이터 모델.
    // 영속화는 AppSettingsStore가 담당한다.
    public class AppSettingsData
    {
        public bool UseScheduling { get; set; }

        // index 0 => "hr01"(1시) ... index 23 => "hr24"(24시, 즉 자정 0시)
        public bool[] HourEnabled { get; set; } = new bool[24];

        public bool UseFTP { get; set; }
        public string FtpIp { get; set; } = "";
        public string FtpId { get; set; } = "";
        public string FtpPassword { get; set; } = ""; // 메모리상 평문. 저장 시에만 CredentialProtector로 보호한다.
        public string FtpPort { get; set; } = "";
        public string SystemName { get; set; } = "";
        public string PortList { get; set; } = "22/80/8080";
        public bool UsePortChecking { get; set; }
        public bool LoadLatestFileOnStartup { get; set; } = true;

        // 연속 모니터링: 켜면 지정한 분(分) 간격마다 자동으로 전체 대역을 재스캔한다.
        public bool ContinuousMonitoring { get; set; }
        public int MonitorIntervalMinutes { get; set; } = 10;

        // UI 언어("en"/"ko"). 기본값 영어. 변경은 재시작 시 반영된다.
        public string Language { get; set; } = Localization.English;

        public bool IsInScheduleHour(int clockHour)
        {
            int label = clockHour == 0 ? 24 : clockHour;
            if (label < 1 || label > 24) return false;
            return HourEnabled[label - 1];
        }
    }
}
