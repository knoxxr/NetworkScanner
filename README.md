# NetworkScanner

지정한 IP 대역을 스캔하여 각 호스트의 생존 여부(Ping), 열린 TCP 포트, MAC 주소/제조사, 호스트명을 조사하고 결과를 CSV로 저장·FTP 업로드할 수 있는 데스크톱 애플리케이션입니다. 알려진 예약 포트/백도어(트로이목마) 포트 목록과 대조해 의심스러운 열린 포트를 식별하는 기능과, 매시간 자동으로 스캔을 수행하는 스케줄링 기능을 제공합니다.

Windows 전용 WPF 버전과, Windows/macOS/Linux에서 모두 동작하는 Avalonia 버전 두 가지 UI를 제공하며, 둘 다 동일한 `NetworkScanner.Core` 스캔 엔진을 공유합니다.

## 프로젝트 구조

| 프로젝트 | 설명 |
|---|---|
| `NetworkScanner/` | WPF UI (Windows 전용) |
| `NetworkScanner.Avalonia/` | Avalonia UI (Windows/macOS/Linux 크로스플랫폼) |
| `NetworkScanner.Core/` | UI에 의존하지 않는 공용 로직 라이브러리 (`net8.0`, 플랫폼 독립적) — 스캔 엔진(`ScanEngine`), IP 범위 계산, 설정 저장/로드, OUI(제조사) 조회, ARP 조회, 참조 포트 목록 로딩 |
| `NetworkScanner.Tests/` | `NetworkScanner.Core`에 대한 xUnit 단위 테스트 |
| `OUIConvertor/` | IEEE OUI(제조사) 원본 텍스트를 `ouiinfo.ini`로 변환하는 별도 유틸리티 (WPF, Windows 전용) |
| `SetupNetworkScanner/` | WPF 버전용 Visual Studio 설치 프로젝트(.vdproj, Windows 전용 빌드 도구 필요) |

### 왜 Core를 분리했는가

WPF 프로젝트(`UseWPF=true`)는 Windows Desktop 런타임에 의존하므로, 여기에 직접 의존하는 코드는 Windows에서만 실행할 수 있습니다. 스캔 로직·설정 저장·포트 참조 로딩 등 UI에 의존하지 않는 부분을 `NetworkScanner.Core`로 분리함으로써:

- 어떤 OS에서도 단위 테스트를 실행할 수 있고,
- WPF와 Avalonia 두 UI가 스캔 엔진(`ScanEngine`)을 그대로 공유해 로직이 중복되지 않으며,
- ARP 조회(`ArpResolver`)와 로깅(`AppLogger`)은 Windows/비-Windows 분기를 내부적으로 처리해 두 UI 모두 별도 코드 없이 사용할 수 있습니다.

## 요구 사항

- .NET 8 SDK
- `NetworkScanner`(WPF)와 `SetupNetworkScanner`(vdproj)는 **Windows에서만 빌드/실행 가능**합니다.
- `NetworkScanner.Avalonia`, `NetworkScanner.Core`, `NetworkScanner.Tests`는 Windows/macOS/Linux 어디서나 빌드 및 실행 가능합니다.

## 빌드 및 실행

```bash
# 전체 솔루션 빌드 (Windows — WPF 포함)
dotnet build NetworkScanner.sln

# macOS/Linux에서 WPF 프로젝트를 포함해 컴파일만 확인하는 경우
# (실행은 불가, 컴파일 검증 목적)
dotnet build NetworkScanner.sln -p:EnableWindowsTargeting=true

# Avalonia 앱 실행 (Windows/macOS/Linux 어디서나)
cd NetworkScanner.Avalonia
dotnet run
```

### 배포용 단일 실행 파일 만들기 (Avalonia)

```bash
dotnet publish NetworkScanner.Avalonia/NetworkScanner.Avalonia.csproj -c Release -r win-x64   --self-contained true
dotnet publish NetworkScanner.Avalonia/NetworkScanner.Avalonia.csproj -c Release -r osx-arm64 --self-contained true
dotnet publish NetworkScanner.Avalonia/NetworkScanner.Avalonia.csproj -c Release -r linux-x64 --self-contained true
```

`.github/workflows/build-and-test.yml`이 위 세 플랫폼에 대한 빌드를 GitHub Actions에서 자동으로 수행하고 아티팩트로 업로드합니다.

## 테스트

```bash
dotnet test NetworkScanner.Tests/NetworkScanner.Tests.csproj
```

`NetworkScanner.Core`만 참조하므로 Windows가 아닌 환경에서도 정상적으로 실행됩니다.

## 실행 시 필요한 설정/데이터 파일

애플리케이션 실행 디렉터리에 아래 파일들이 있어야 합니다(최초 실행 시 없으면 자동 생성되는 파일도 있음).

| 파일 | 용도 |
|---|---|
| `iprange.ini` | 스캔할 IP 대역 목록 |
| `setting.ini` | FTP 계정(비밀번호는 암호화되어 저장 — Windows는 DPAPI, 그 외 OS는 임시 난독화), 스케줄링 시간대, 포트 목록 등 설정 |
| `reservedports.ini` | 예약(잘 알려진) 포트 참조 목록 |
| `prohibitports.ini` | 백도어/트로이목마가 사용하는 것으로 알려진 위험 포트 목록 |
| `ouiinfo.ini` | MAC 주소 OUI → 제조사 매핑 데이터 (IEEE 공개 데이터를 `OUIConvertor`로 변환한 결과) |

`NetworkScanner.Avalonia`는 위 세 참조 데이터 파일(`ouiinfo.ini`/`reservedports.ini`/`prohibitports.ini`)을 `NetworkScanner/` 프로젝트의 것을 빌드 시 그대로 복사해 사용합니다(중복 보관하지 않음).

## 주요 기능

- IP 대역 지정 스캔 (Ping 응답, 왕복 시간, 열린 포트, MAC 주소/제조사, 호스트명 조회)
- 예약 포트 / 위험(백도어) 포트 대조 검색
- 스캔 결과 CSV 저장·불러오기, FTP 업로드
- 매시간 단위 자동 스캔 스케줄링
- 스캔 도중 취소 가능

## 알려진 제한 사항 (크로스플랫폼)

- **ARP(MAC 주소) 조회**: Windows는 `SendARP` API를 사용하고, macOS/Linux는 시스템 `arp` 명령 출력을 파싱합니다. 유닉스 계열에서는 해당 IP로 먼저 ping을 보내 ARP 캐시가 채워진 뒤에만 조회가 정상 동작합니다(스캔 로직상 항상 ping 이후 조회하므로 일반적인 사용에는 문제 없음).
- **FTP 비밀번호 보호**: Windows는 DPAPI로 실제 암호화되지만, macOS/Linux는 아직 OS 키체인(Keychain/libsecret) 연동 전이라 단순 Base64 난독화만 적용됩니다. 향후 개선 예정입니다.
- **ICMP 권한**: Linux/macOS 환경에 따라 일반 사용자 권한으로 ICMP ping이 제한될 수 있습니다(배포 시 문서화 필요).
