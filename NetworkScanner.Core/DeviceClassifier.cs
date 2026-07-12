using System;
using System.Collections.Generic;

namespace NetworkScanner
{
    // 제조사(OUI 조회 결과)와 열린 포트를 힌트로 장비 종류를 대략 분류한다. 확실한 식별이 아니라 참고용 태그다.
    public static class DeviceClassifier
    {
        // 제조사명에 포함되면 해당 분류로 보는 키워드. 위쪽(더 구체적인 것)부터 검사한다.
        private static readonly (string keyword, string category)[] VendorRules =
        {
            ("apple", "Apple 기기"),
            ("samsung", "Samsung 기기"),
            ("lg electronics", "LG 기기"),
            ("raspberry", "라즈베리파이"),
            ("intel", "PC/노트북"),
            ("dell", "PC/노트북"),
            ("asustek", "PC/네트워크"),
            ("giga-byte", "PC"),
            ("micro-star", "PC"),
            ("hewlett", "프린터/PC"),
            ("canon", "프린터"),
            ("epson", "프린터"),
            ("brother", "프린터"),
            ("cisco", "네트워크 장비"),
            ("tp-link", "네트워크 장비"),
            ("netgear", "네트워크 장비"),
            ("ubiquiti", "네트워크 장비"),
            ("mikrotik", "네트워크 장비"),
            ("d-link", "네트워크 장비"),
            ("zyxel", "네트워크 장비"),
            ("hangzhou hikvision", "CCTV/IP카메라"),
            ("dahua", "CCTV/IP카메라"),
            ("espressif", "IoT 장치"),
            ("texas instrument", "IoT 장치"),
            ("google", "IoT/미디어"),
            ("amazon", "IoT/미디어"),
            ("sonos", "미디어 기기"),
        };

        public static string Classify(string? vendor, string? openPorts)
        {
            string v = (vendor ?? "").ToLowerInvariant();
            foreach (var (keyword, category) in VendorRules)
            {
                if (v.Contains(keyword)) return category;
            }

            // 제조사로 못 맞추면 열린 포트로 대략 추정한다.
            HashSet<int> ports = ParsePorts(openPorts);
            if (ports.Contains(9100) || ports.Contains(515) || ports.Contains(631)) return "프린터";
            if (ports.Contains(554)) return "CCTV/IP카메라";
            if (ports.Contains(3389)) return "Windows PC";
            if (ports.Contains(445) || ports.Contains(139)) return "Windows/파일공유";
            if (ports.Contains(22)) return "서버/리눅스";
            if (ports.Contains(80) || ports.Contains(443)) return "웹 서비스 장비";

            return "";
        }

        private static HashSet<int> ParsePorts(string? field)
        {
            var set = new HashSet<int>();
            if (string.IsNullOrEmpty(field)) return set;
            foreach (string token in field.Split('/', StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(token, out int port))
                    set.Add(port);
            return set;
        }
    }
}
