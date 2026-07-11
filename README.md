# NetworkScanner

지정한 IP 대역을 스캔하여 각 호스트의 생존 여부(Ping), 열린 TCP 포트, MAC 주소/제조사, 호스트명을 조사하고 결과를 CSV로 저장·FTP 업로드할 수 있는 데스크톱 애플리케이션입니다. 알려진 예약 포트/백도어(트로이목마) 포트 목록과 대조해 의심스러운 열린 포트를 식별하는 기능과, 매시간 자동으로 스캔을 수행하는 스케줄링 기능을 제공합니다.

Windows 전용 **WPF** 버전과, Windows/macOS/Linux에서 모두 동작하는 **Avalonia** 버전 두 가지 UI를 제공하며, 둘 다 동일한 `NetworkScanner.Core` 스캔 엔진을 공유합니다.

## 다운로드

빌드 없이 바로 실행해보려면 **[Releases 페이지](../../releases/latest)** 에서 OS에 맞는 파일을 받으세요.

| OS | 파일 |
|---|---|
| Windows | `NetworkScanner-win-Setup.exe` (설치형) 또는 `NetworkScanner-win-Portable.zip` (압축 해제 후 바로 실행) |
| macOS | `NetworkScanner-osx-Setup.pkg` (설치형) 또는 `NetworkScanner-osx-Portable.zip`(압축 해제 후 `.app` 실행) |
| Linux | `NetworkScanner-linux-Setup.AppImage`(실행 권한 부여 후 바로 실행) 또는 `NetworkScanner-linux-Portable.zip` |

새 버전은 `v*.*.*` 형태의 git 태그를 push하면 `.github/workflows/release.yml`이 3개 OS에서 각각 패키징해 자동으로 Release에 올립니다.

## 목차

- [주요 기능](#주요-기능)
- [프로젝트 구조](#프로젝트-구조)
- [요구 사항](#요구-사항)
- [빌드 및 실행](#빌드-및-실행)
- [테스트](#테스트)
- [배포 패키징](#배포-패키징)
- [실행 시 필요한 설정/데이터 파일](#실행-시-필요한-설정데이터-파일)
- [알려진 제한 사항 (크로스플랫폼)](#알려진-제한-사항-크로스플랫폼)

## 주요 기능

- IP 대역 지정 스캔 (Ping 응답, 왕복 시간, 열린 포트, MAC 주소/제조사, 호스트명 조회)
- 예약 포트 / 위험(백도어) 포트 대조 검색
- 스캔 결과 CSV 저장·불러오기, FTP 업로드
- 매시간 단위 자동 스캔 스케줄링
- 스캔 도중 취소 가능

## 프로젝트 구조

| 프로젝트 | 설명 |
|---|---|
| `NetworkScanner/` | WPF UI (**Windows 전용**) |
| `NetworkScanner.Avalonia/` | Avalonia UI (**Windows/macOS/Linux 크로스플랫폼**) |
| `NetworkScanner.Core/` | UI에 의존하지 않는 공용 로직 라이브러리 (`net8.0`, 플랫폼 독립적) |
| `NetworkScanner.Tests/` | `NetworkScanner.Core`에 대한 xUnit 단위 테스트 |
| `OUIConvertor/` | IEEE OUI(제조사) 원본 텍스트를 `ouiinfo.ini`로 변환하는 별도 유틸리티 (WPF, Windows 전용, 독립 실행) |
| `SetupNetworkScanner/` | WPF 버전용 레거시 Visual Studio 설치 프로젝트(.vdproj) |

`NetworkScanner.Core`가 담당하는 것: 스캔 엔진(`ScanEngine`), IP 범위 계산(`IPRangeUtil`), 설정 파일 저장/로드(`AppSettingsStore`), FTP 업로드(`FTPService`, FluentFTP 기반), OUI(제조사) 조회(`OUIInfo`), ARP 조회(`ArpResolver`), 참조 포트 목록 로딩(`PortReferenceLoader`), 크로스플랫폼 로깅(`AppLogger`)과 자격 증명 보호(`CredentialProtector`).

### 왜 Core를 분리했는가

WPF 프로젝트(`UseWPF=true`)는 Windows Desktop 런타임에 의존하므로, 여기에 직접 의존하는 코드는 Windows에서만 실행할 수 있습니다. 스캔 로직·설정 저장·포트 참조 로딩 등 UI에 의존하지 않는 부분을 `NetworkScanner.Core`로 분리함으로써:

- 어떤 OS에서도 단위 테스트를 실행할 수 있고,
- WPF와 Avalonia 두 UI가 스캔 엔진(`ScanEngine`)을 그대로 공유해 로직이 중복되지 않으며,
- ARP 조회(`ArpResolver`)와 로깅(`AppLogger`), 자격 증명 보호(`CredentialProtector`)는 Windows/macOS/Linux 분기를 내부적으로 처리해 두 UI 모두 별도 코드 없이 사용할 수 있습니다.

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

## 테스트

```bash
dotnet test NetworkScanner.Tests/NetworkScanner.Tests.csproj
```

`NetworkScanner.Core`만 참조하므로 Windows가 아닌 환경에서도 정상적으로 실행됩니다.

## 배포 패키징

### 단일 실행 파일 (Avalonia, self-contained publish)

```bash
dotnet publish NetworkScanner.Avalonia/NetworkScanner.Avalonia.csproj -c Release -r win-x64   --self-contained true
dotnet publish NetworkScanner.Avalonia/NetworkScanner.Avalonia.csproj -c Release -r osx-arm64 --self-contained true
dotnet publish NetworkScanner.Avalonia/NetworkScanner.Avalonia.csproj -c Release -r linux-x64 --self-contained true
```

### 설치 패키지 ([Velopack](https://velopack.io))

`vpk` CLI로 위 publish 결과물을 OS별 설치 패키지로 감쌀 수 있습니다(Windows는 설치용 `.exe`, macOS는 `.app`/`.pkg`/`.zip`, Linux는 AppImage 계열 패키지). `vpk`는 **실행 중인 OS에 맞는 패키지만** 만들 수 있으므로(크로스 패키징 불가), 각 OS에서 직접 실행해야 합니다 — 아래는 macOS에서 실제 `.app`/`.pkg`를 만들어 검증한 명령입니다.

```bash
dotnet tool install -g vpk   # 최초 1회
export PATH="$PATH:$HOME/.dotnet/tools"

dotnet publish NetworkScanner.Avalonia/NetworkScanner.Avalonia.csproj -c Release -r osx-arm64 --self-contained true -o publish/osx-arm64

vpk pack --packId NetworkScanner --packVersion 1.0.0 \
  --packDir publish/osx-arm64 --mainExe NetworkScanner.Avalonia \
  --packTitle "Network Scanner" --packAuthors SMIC \
  --runtime osx-arm64 --outputDir release-packages/osx-arm64
```

Windows에서는 `--mainExe NetworkScanner.Avalonia.exe`와 `--icon NetworkScanner/img/Kyo-Tux-Delikate-Network.ico`를 추가로 지정하면 아이콘이 적용된 설치 파일이 만들어집니다.

CI에는 목적이 다른 두 워크플로우가 있습니다:

- `.github/workflows/build-and-test.yml` — 모든 push/PR에서 테스트·빌드·패키징이 여전히 되는지 확인하고, 결과물을 워크플로우 아티팩트로 남깁니다(90일 후 만료, GitHub 로그인 필요).
- `.github/workflows/release.yml` — `v*.*.*` 형태의 태그를 push하면 Windows/macOS/Linux 각 러너에서 publish + `vpk pack`을 수행하고, 결과 설치 패키지를 [Releases 페이지](../../releases)에 첨부해 누구나 바로 다운로드할 수 있게 합니다.

WPF 버전의 레거시 `.vdproj`(`SetupNetworkScanner/`)는 그대로 유지되지만, 신규 배포는 Avalonia 버전 + Velopack 패키지 사용을 권장합니다.

## 실행 시 필요한 설정/데이터 파일

애플리케이션 실행 디렉터리에 아래 파일들이 있어야 합니다(최초 실행 시 없으면 자동 생성되는 파일도 있음).

| 파일 | 용도 |
|---|---|
| `iprange.ini` | 스캔할 IP 대역 목록 |
| `setting.ini` | FTP 계정(비밀번호는 암호화되어 저장 — Windows는 DPAPI, macOS는 Keychain, Linux는 libsecret), 스케줄링 시간대, 포트 목록 등 설정 |
| `reservedports.ini` | 예약(잘 알려진) 포트 참조 목록 |
| `prohibitports.ini` | 백도어/트로이목마가 사용하는 것으로 알려진 위험 포트 목록 |
| `ouiinfo.ini` | MAC 주소 OUI → 제조사 매핑 데이터 (IEEE 공개 데이터를 `OUIConvertor`로 변환한 결과) |

`NetworkScanner.Avalonia`는 위 세 참조 데이터 파일(`ouiinfo.ini`/`reservedports.ini`/`prohibitports.ini`)과 아이콘 이미지를 `NetworkScanner/` 프로젝트의 것을 빌드 시 그대로 링크해 사용합니다(중복 보관하지 않음).

## 알려진 제한 사항 (크로스플랫폼)

- **ARP(MAC 주소) 조회**: Windows는 `SendARP` API를 사용하고, macOS/Linux는 시스템 `arp` 명령 출력을 파싱합니다. 유닉스 계열에서는 해당 IP로 먼저 ping을 보내 ARP 캐시가 채워진 뒤에만 조회가 정상 동작합니다(스캔 로직상 항상 ping 이후 조회하므로 일반적인 사용에는 문제 없음).
- **FTP 비밀번호 보호**: Windows는 DPAPI, macOS는 Keychain(`security` 명령), Linux는 libsecret(`secret-tool`)을 이용해 OS 자격 증명 저장소에 보관합니다. 해당 도구가 없는 환경(Linux에서 `secret-tool` 미설치 등)에서는 단순 Base64 난독화로 자동 폴백하며, 이 경우 실행 시 경고가 기록됩니다.
- **ICMP Ping 권한**: Linux/macOS 환경에 따라 일반 사용자 권한으로 ICMP ping이 제한될 수 있습니다. 권한 문제가 감지되면 해당 호스트는 응답 없음(Timeout)으로 처리되고, 스캔이 중단되지 않도록 안내 메시지가 한 번만 기록됩니다. 필요 시:
  - Linux: `sudo setcap cap_net_raw+ep <실행파일 경로>` 로 ping 전용 권한만 부여하거나, `sudo`로 실행
  - macOS: 별도 권한 부여 없이도 대부분 동작하지만, 제한되는 경우 `sudo`로 실행
- **미서명 설치 파일 경고 (macOS/Windows)**: Release의 설치 파일은 아직 Apple/Microsoft 코드서명·공증을 받지 않았습니다(각각 유료 개발자 인증서 필요). 따라서 처음 실행 시 다음과 같은 OS 경고가 뜨는 것이 정상이며, 악성코드가 아닙니다.
  - **macOS**: `"NetworkScanner-osx-Setup.pkg"을(를) 열지 않음` 경고가 뜨면 — Finder에서 파일을 **우클릭(Control+클릭) → 열기** → 경고창에서 다시 **열기**를 누르면 설치됩니다. 또는 터미널에서 `xattr -d com.apple.quarantine NetworkScanner-osx-Setup.pkg` 실행 후 다시 열면 됩니다.
  - **Windows**: "Windows에서 PC를 보호했습니다"(SmartScreen) 화면이 뜨면 — **추가 정보** 클릭 → **실행** 버튼을 누르면 설치가 진행됩니다.
