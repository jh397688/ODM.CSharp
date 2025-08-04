# ODM.CSharp

**ODM.CSharp**는 C#으로 ONVIF 프로토콜의 모든 기능을 구현하고, FFmpeg를 활용한 CCTV 실시간 라이브 뷰어 기능까지 제공하는 통합 소프트웨어  

---

## C# (.NET 8+)

---

## WPF

---

## Prism

---

## Onvif WSDL
### 1. Core ONVIF Services

| 서비스 이름                                                                                              | 버전 | WSDL 파일       | 설명                          |
|--------------------------------------------------------------------------------------------------------|------|-----------------|-----------------------------|
| [Device Management Service](https://www.onvif.org/ver10/device/wsdl/devicemgmt.wsdl)                   | 1.0  | devicemgmt.wsdl | 장치 정보 조회 및 네트워크·사용자 관리 |
| [Media Service](https://www.onvif.org/ver10/media/wsdl/media.wsdl)                                     | 1.0  | media.wsdl      | 스트림·스냅샷 URI 제공           |
| [Media2 Service](https://www.onvif.org/ver20/media/wsdl/media.wsdl)                                    | 2.0  | media.wsdl      | ver1.0 기능 + 멀티트랙·WebRTC 지원 |
| [Imaging Service](https://www.onvif.org/ver20/imaging/wsdl/imaging.wsdl)                               | 2.0  | imaging.wsdl    | 화이트밸런스·노출·백라이트 보정 등 영상 설정 |
| [PTZ Service](https://www.onvif.org/ver20/ptz/wsdl/ptz.wsdl)                                           | 2.0  | ptz.wsdl        | 팬·틸트·줌 제어 및 프리셋 관리    |
| [Events Service](https://www.onvif.org/ver10/events/wsdl/event.wsdl)                                   | 1.0  | event.wsdl      | 이벤트 구독·풀링 인터페이스 제공   |
| [Analytics Service](https://www.onvif.org/ver20/analytics/wsdl/analytics.wsdl)                         | 2.0  | analytics.wsdl  | 영상분석 모듈·룰 설정 및 관리     |

### 2. Recording & Streaming Services

| 서비스 이름                                                                                              | 버전 | WSDL 파일       | 설명                              |
|--------------------------------------------------------------------------------------------------------|------|-----------------|---------------------------------|
| [Receiver Service](https://www.onvif.org/ver10/receiver.wsdl)                                            | 1.0  | receiver.wsdl   | 수신 장치 구성 및 제어         |
| [Recording Control Service](https://www.onvif.org/ver10/recording.wsdl)                                | 1.0  | recording.wsdl  | 녹화 세션 시작/중지 및 관리          |
| [Recording Search Service](https://www.onvif.org/ver10/search.wsdl)                                    | 1.0  | search.wsdl     | 녹화물 검색 인터페이스              |
| [Replay (Playback) Service](https://www.onvif.org/ver10/replay.wsdl)                                   | 1.0  | replay.wsdl     | 녹화 영상 재생 제어                 |

### 3. Auxiliary & Add-On Services

| 서비스 이름                                                                                              | 버전 | WSDL 파일               | 설명                              |
|--------------------------------------------------------------------------------------------------------|------|-------------------------|---------------------------------|
| [Device I/O Service](https://www.onvif.org/ver10/deviceio.wsdl)                                         | 1.0  | deviceio.wsdl           | 디지털 입출력 제어                  |
| [Display Service](https://www.onvif.org/onvif/ver10/display.wsdl)                                       | 1.0  | display.wsdl            | OSD(화면 표시) 관리                |
| [Schedule Service](https://www.onvif.org/ver10/schedule/wsdl/schedule.wsdl)                             | 1.0  | schedule.wsdl           | 스케줄링 기반 동작 제어            |
| [Action Engine Service](https://www.onvif.org/ver10/actionengine.wsdl)                                  | 1.0  | actionengine.wsdl       | 이벤트 기반 액션 정의 및 실행        |
| [Advanced Security Service](https://www.onvif.org/ver10/advancedsecurity/wsdl/advancedsecurity.wsdl)    | 1.0  | advancedsecurity.wsdl   | TLS·인증서 관리 및 고급 보안 설정    |
| [Access Control Service](https://www.onvif.org/ver10/pacs/accesscontrol.wsdl)                           | 1.0  | accesscontrol.wsdl      | 출입통제 장치 제어 및 권한 관리     |
| [Access Rules Service](https://www.onvif.org/ver10/accessrules/wsdl/accessrules.wsdl)                   | 1.0  | accessrules.wsdl        | 출입 규칙(ACL) 정의 및 관리         |
| [Door Control Service](https://www.onvif.org/ver10/pacs/doorcontrol.wsdl)                               | 1.0  | doorcontrol.wsdl        | 도어락 개폐 제어 |
| [Provisioning Service](https://www.onvif.org/ver10/provisioning/wsdl/provisioning.wsdl)                 | 1.0  | provisioning.wsdl       | 초기 구성 및 프로비저닝 관리         |
| [Credential Service](https://www.onvif.org/ver10/credential/wsdl/credential.wsdl)                       | 1.0  | credential.wsdl         | 사용자 자격증명 관리                |
| [Authentication Behavior Service](https://www.onvif.org/ver10/authenticationbehavior/wsdl/authenticationbehavior.wsdl) | 1.0  | authenticationbehavior.wsdl | 인증 정책 및 동작 설정             |
| [Uplink Service](https://www.onvif.org/ver10/uplink/wsdl/uplink.wsdl)                                   | 1.0  | uplink.wsdl             | 외부 네트워크 연결 설정             |
| [Thermal Service](https://www.onvif.org/ver10/thermal/wsdl/thermal.wsdl)                                | 1.0  | thermal.wsdl            | 열화상 카메라 설정 및 정보 조회      |
---

## Native Socket

---

## FFmpeg.AutoGen

---

## WPF Extended Toolkit, ModernWPF	

---
