using System.Collections.Generic;

namespace NetworkScanner
{
    // 간단한 키 기반 다국어 지원. 기본 언어는 영어("en")이며 한국어("ko")를 지원한다.
    // 언어는 앱 시작 시 설정에서 읽어 고정되고(전환은 재시작 시 반영), UI/코드/리포트가 T(key)로 문자열을 얻는다.
    public static class Localization
    {
        public const string English = "en";
        public const string Korean = "ko";

        // 현재 언어. 기본값은 영어.
        public static string Current { get; set; } = English;

        public static bool IsKorean => Current == Korean;

        // key -> (en, ko)
        private static readonly Dictionary<string, (string En, string Ko)> Table = new()
        {
            // 공통/네비게이션
            ["app.title"] = ("Network Scanner", "Network Scanner"),
            ["nav.iplist"] = ("IP List", "IP 리스트"),
            ["nav.settings"] = ("Settings", "설정"),
            ["nav.portinfo"] = ("Ref. Ports", "참고. 포트정보"),
            ["nav.guide"] = ("User Guide", "사용 설명서"),
            ["guide.title"] = ("User Guide", "사용 설명서"),

            // IP 리스트 화면
            ["iplist.title"] = ("IP List", "IP List"),
            ["btn.new"] = ("New", "새로 만들기"),
            ["btn.open"] = ("Open", "파일 열기"),
            ["btn.save"] = ("Save", "파일 저장"),
            ["btn.report"] = ("Report (HTML/JSON)", "리포트(HTML/JSON)"),
            ["btn.scan"] = ("Scan", "스캔"),
            ["btn.scanning"] = ("Scanning...", "스캔 중..."),
            ["btn.cancel"] = ("Cancel", "취소"),
            ["label.search"] = ("Search:", "검색:"),
            ["search.watermark"] = ("IP / name / port / MAC / vendor / note", "IP/이름/포트/Mac/Vendor/비고"),
            ["filter.aliveonly"] = ("Alive only", "살아있는 것만"),
            ["legend.up"] = ("Up", "정상"),
            ["legend.warn"] = ("Warning (risky port)", "주의(위험포트)"),
            ["legend.down"] = ("Down", "없음"),

            // 상태 텍스트(그리드/리포트 공용)
            ["status.up"] = ("Up", "정상"),
            ["status.warn"] = ("Warning", "주의"),
            ["status.down"] = ("Down", "없음"),

            // 컬럼 헤더
            ["col.status"] = ("Status", "상태"),
            ["col.ip"] = ("IP", "IP"),
            ["col.name"] = ("Name", "이름"),
            ["col.type"] = ("Type", "종류"),
            ["col.os"] = ("OS (guess)", "OS추정"),
            ["col.ports"] = ("Open Ports", "열린 Port"),
            ["col.service"] = ("Service", "서비스"),
            ["col.mac"] = ("MAC Address", "Mac Address"),
            ["col.vendor"] = ("Vendor", "Vendor"),
            ["col.rtt"] = ("RTT (ms)", "Round Time(ms)"),
            ["col.note"] = ("Note", "비고"),
            ["col.created"] = ("Created", "생성일"),

            // 컨텍스트 메뉴
            ["menu.ping"] = ("Ping", "Ping 보내기"),
            ["menu.checkport"] = ("Check configured ports", "설정된 Port만 검색"),
            ["menu.checkreserved"] = ("Check all reserved ports", "예약 Port 전체 검색"),
            ["menu.checkprohibit"] = ("Check all risky ports", "위험 Port 전체 검색"),
            ["menu.wol"] = ("Wake-on-LAN", "Wake-on-LAN (원격 켜기)"),
            ["menu.remove"] = ("Remove", "삭제"),

            // 설정 화면
            ["settings.title"] = ("Settings", "설정 화면"),
            ["settings.save"] = ("Save settings", "설정 저장"),
            ["settings.iprange"] = ("IP scan ranges", "IP 검색 범위"),
            ["settings.startip"] = ("Start IP", "시작 IP"),
            ["settings.endip"] = ("End IP", "종료 IP"),
            ["settings.desc"] = ("Description", "설명"),
            ["settings.addrange"] = ("Add range", "대역 추가"),
            ["settings.removerange"] = ("Remove range", "대역 삭제"),
            ["settings.ports"] = ("Ports to scan", "검색할 Port"),
            ["settings.useportcheck"] = ("Enable port scan", "Port 검색 사용"),
            ["settings.portno"] = ("Port numbers", "Port 번호"),
            ["settings.scheduling"] = ("Scheduling", "스케줄링"),
            ["settings.usescheduling"] = ("Enable scheduling", "스케줄링 사용"),
            ["settings.filelocation"] = ("File save location", "파일저장위치"),
            ["settings.ftpsave"] = ("FTP upload", "FTP 저장"),
            ["settings.useftp"] = ("Enable FTP upload", "FTP 저장 사용"),
            ["settings.general"] = ("General", "일반"),
            ["settings.systemname"] = ("System name", "시스템 이름"),
            ["settings.loadlatest"] = ("Load latest file on startup", "시작시 최근 기록 부르기"),
            ["settings.continuous"] = ("Continuous monitoring", "연속 모니터링"),
            ["settings.interval"] = ("min interval", "분 간격"),
            ["settings.language"] = ("Language", "언어"),
            ["settings.lang.restart"] = ("(applies after restart)", "(재시작 후 적용)"),

            // 메시지/대화상자
            ["msg.deleteall"] = ("Delete all items?", "리스트를 모두 삭제할까요?"),
            ["msg.delete"] = ("Delete", "삭제"),
            ["dlg.confirm"] = ("Confirm", "확인"),
            ["msg.noresult.report"] = ("No results to export.", "리포트로 저장할 결과가 없습니다."),
            ["msg.report.saved.html"] = ("HTML report saved.", "HTML 리포트를 저장했습니다."),
            ["msg.report.saved.json"] = ("JSON report saved.", "JSON 리포트를 저장했습니다."),
            ["msg.wol.nomac"] = ("No MAC address; cannot send Wake-on-LAN.", "MAC 주소가 없어 Wake-on-LAN을 보낼 수 없습니다."),
            ["msg.wol.sent"] = ("Sent Wake-on-LAN magic packet:", "Wake-on-LAN 매직 패킷을 보냈습니다:"),
            ["msg.wol.failed"] = ("Failed to send Wake-on-LAN.", "Wake-on-LAN 전송에 실패했습니다."),
            ["msg.cidr.invalid"] = ("Invalid CIDR (e.g. 192.168.1.0/24)", "CIDR 형식이 올바르지 않습니다 (예: 192.168.1.0/24)"),
            ["msg.startip.invalid"] = ("Invalid start IP (or CIDR: 192.168.1.0/24)", "시작 IP 값 이상 (또는 CIDR 형식: 192.168.1.0/24)"),
            ["msg.endip.invalid"] = ("Invalid end IP", "종료 IP 값 이상"),
            ["msg.range.duplicate"] = ("This range is already registered.", "이미 등록된 대역입니다."),
            ["msg.range.saved"] = ("Saved IP scan range file.", "IP 검색 대역 파일을 저장하였습니다."),
            ["msg.settings.saved"] = ("Saved settings file.", "설정 파일을 저장하였습니다."),

            // 스캔 진행/결과 (ScanEngine)
            ["scan.norange"] = ("No IP scan range configured. Add one in Settings.", "설정된 IP 검색 대역이 없습니다. 설정 화면에서 대역을 추가해주세요."),
            ["scan.phase1.start"] = ("[1/2] Host discovery started", "[1/2] 호스트 검색 시작"),
            ["scan.phase1.progress"] = ("[1/2] Discovering", "[1/2] 호스트 검색"),
            ["scan.phase2.start"] = ("[2/2] Detail lookup started", "[2/2] 상세 조회 시작"),
            ["scan.phase2.progress"] = ("[2/2] Detail", "[2/2] 상세 조회"),
            ["scan.cancelled"] = ("Scan cancelled.", "스캔이 취소되었습니다."),
            ["scan.completed"] = ("Scan complete.", "스캔을 완료했습니다."),
            ["scan.alivecount"] = ("alive hosts", "살아있는 호스트"),
            ["scan.alreadyscanning"] = ("Already scanning.", "이미 스캐닝 중입니다."),
            ["scan.cancelrequested"] = ("Requested scan cancellation.", "스캔 취소를 요청했습니다."),

            // 변화 감지
            ["change.detected"] = ("changes detected", "건 감지"),
            ["change.securityalert"] = ("security alerts", "보안 경고"),
            ["change.dialogtitle"] = ("Security alert - scan changes", "보안 경고 - 스캔 변화 감지"),
            ["change.newhost"] = ("New/returning host:", "신규/재접속 호스트:"),
            ["change.offline"] = ("Went offline:", "오프라인 전환:"),
            ["change.macchanged"] = ("⚠ MAC changed:", "⚠ MAC 변경:"),
            ["change.newport"] = ("New open port:", "새 포트 열림:"),
            ["change.prohibited"] = ("⚠ Risky port found:", "⚠ 위험 포트 발견:"),
            ["change.newdevice"] = ("new device", "새 장비"),
            ["change.reconnect"] = ("reconnected", "재접속"),
            ["change.noresponse"] = ("no response", "응답 없음"),

            // 장비 종류 분류(DeviceClassifier)
            ["dev.apple"] = ("Apple device", "Apple 기기"),
            ["dev.samsung"] = ("Samsung device", "Samsung 기기"),
            ["dev.lg"] = ("LG device", "LG 기기"),
            ["dev.raspberrypi"] = ("Raspberry Pi", "라즈베리파이"),
            ["dev.pc"] = ("PC", "PC"),
            ["dev.pclaptop"] = ("PC/Laptop", "PC/노트북"),
            ["dev.pcnet"] = ("PC/Network", "PC/네트워크"),
            ["dev.printerpc"] = ("Printer/PC", "프린터/PC"),
            ["dev.printer"] = ("Printer", "프린터"),
            ["dev.network"] = ("Network device", "네트워크 장비"),
            ["dev.cctv"] = ("CCTV/IP camera", "CCTV/IP카메라"),
            ["dev.iot"] = ("IoT device", "IoT 장치"),
            ["dev.iotmedia"] = ("IoT/Media", "IoT/미디어"),
            ["dev.media"] = ("Media device", "미디어 기기"),
            ["dev.windowspc"] = ("Windows PC", "Windows PC"),
            ["dev.winshare"] = ("Windows/File share", "Windows/파일공유"),
            ["dev.server"] = ("Server/Linux", "서버/리눅스"),
            ["dev.web"] = ("Web service device", "웹 서비스 장비"),

            // 업데이트
            ["update.button"] = ("Update", "업데이트"),
            ["update.available"] = ("New version {0} is available. Click to install.", "새 버전 {0}이(가) 있습니다. 클릭하면 설치합니다."),
            ["update.downloading"] = ("Downloading...", "다운로드 중..."),
            ["update.failed"] = ("Update failed", "업데이트 실패"),

            // 참조 포트 정보 화면
            ["portinfo.title"] = ("(Reference) Common port numbers", "(참조) 주요 포트 번호"),
            ["portinfo.reserved"] = ("◎ Reserved ports", "◎ 예약 Port"),
            ["portinfo.backdoor"] = ("◎ Ports commonly used by backdoors and trojans", "◎ 백도어와 트로이 목마가 자주 사용하는 포트"),
        };

        public static string T(string key)
        {
            if (Table.TryGetValue(key, out var pair))
                return IsKorean ? pair.Ko : pair.En;
            return key; // 키가 없으면 키 자체를 반환(누락을 눈에 띄게)
        }
    }
}
