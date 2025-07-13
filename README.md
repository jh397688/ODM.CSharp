# ODM.CSharp

**ODM.CSharp**는 C#으로 ONVIF 프로토콜의 모든 기능을 구현하고, FFmpeg를 활용한 CCTV 실시간 라이브 뷰어 기능까지 제공하는 통합 소프트웨어  

---

## 프로젝트 소개

- **목표**  
  - ONVIF 표준의 모든 기능(디바이스 관리, 미디어 제어, PTZ 등)을 C#에서 직접 구현  
  - FFmpeg 기반의 CCTV 영상 실시간 디코딩 및 뷰잉  
  - WPF 최신 UI로 누구나 쉽게 사용할 수 있는 데스크톱 어플리케이션 제공

- **제작 동기**  
  - 국내외 CCTV/네트워크 카메라의 ONVIF 연동과 관제 솔루션을 .NET 생태계에서 쉽게 구현하기 위함  
  - ONVIF와 미디어 처리의 실질적 구현 예제 제공 및 기술 공유

---

## 사용 기술 및 주요 라이브러리

| 분류          | 라이브러리/프레임워크      | 설명                                  |
|:--------------|:--------------------------|:--------------------------------------|
| 언어/런타임   | C# (.NET 8+)              | 메인 프로그래밍 언어                  |
| UI 플랫폼     | WPF                       | 데스크톱 UI 프레임워크                |
| Pattern          | Prism                     | WPF MVVM 구조 지원                    |
| Onvif 통신         | Onvif WSDL                | ONVIF 표준 WSDL 기반 통신             |
| RTSP Stream         | Native Socket/Parser | 자체 소켓 및 파싱을 통한 스트리밍             |
| 디코딩        | FFmpeg.AutoGen            | H.264 등 다양한 CCTV 스트림 디코딩, DirectX 를 통한 하드웨어 가속    |
| UI       | WPF Extended Toolkit, ModernWPF      | WPF 컨트롤 확장                       |

---

## 주요 기능

- ONVIF 기반의 디바이스 검색, 관리, 실시간 상태 조회
- RTSP 스트림(라이브/스냅샷) 디코딩 및 재생
- PTZ(팬/틸트/줌) 등 카메라 제어
- WPF 기반의 반응형 UI
- 다양한 CCTV/네트워크 카메라 호환성

---
