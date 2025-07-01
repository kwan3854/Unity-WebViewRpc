[English](README.md) | [Korean](README_ko.md)

[![openupm](https://img.shields.io/npm/v/com.kwanjoong.webviewrpc?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.kwanjoong.webviewrpc/)
[![License](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE.md)
# Unity WebView RPC
유니티 클라이언트(C#)과 웹뷰(HTML, JS)간의 통신에 protobuf를 사용한 grpc와 유사하게 사용할 수 있도록 추상화 레이어를 제공합니다.
기존의 통신방식인 `javascript bridge` 방식을 확장하여 RPC(Remote Procedure Call) 방식과 유사하게 사용할 수 있도록 구현하였습니다.
특정 웹뷰 라이브러리에 종속되지 않도록 Bridge 인터페이스를 제공하여, 어떤 웹뷰 라이브러리를 사용하더라도 동일한 코드로 통신할 수 있도록 설계하였습니다.

## v2.0의 새로운 기능

WebView RPC v2.0은 안정성, 성능, 메모리 관리를 위한 중요한 개선 사항을 도입했습니다.

### 주요 변경 사항
- **고유한 요청 ID (Breaking Change)**: `RequestId`가 증가하는 숫자 대신 UUID를 사용하도록 변경되어 여러 웹뷰 인스턴스 사용 시 발생할 수 있는 충돌을 방지합니다.
- **정확해진 청킹 (Breaking Change)**: `maxChunkSize` 설정이 이제 모든 오버헤드를 포함한 최종 Base64 인코딩 메시지 크기를 정확하게 나타냅니다. `getEffectivePayloadSize()`와 같은 새로운 유틸리티 메서드로 청킹을 정밀하게 설정할 수 있습니다.
- **개선된 타임아웃 처리**: 요청이 무한정 대기하던 심각한 버그를 수정했습니다. 이제 불완전한 메시지는 올바르게 타임아웃 처리됩니다.
- **리소스 관리**: RPC 클라이언트, 서버, 브릿지에 `dispose()` 메서드를 추가하여 이벤트 리스너로 인한 메모리 누수를 방지합니다.

`app-webview-rpc` 라이브러리의 전체 변경 내역은 [CHANGELOG.md](webview_rpc_runtime_library/CHANGELOG.md)를 참조하세요.

## 구조도
WebView RPC는 기존의 `javascript bridge` 방식과 비교하여 작업 흐름이 단순해졌습니다.
```mermaid
flowchart LR
    subgraph 기존 방식
        direction LR
        A[Unity C#] <--> B[데이터 클래스<br>및 메서드<br>수동 구현]
        B <--> C[JSON 파서]
        C <--> D[Javascript Bridge]
        D <--> E[JSON 파서]
        E <--> F[데이터 클래스<br>및 메서드<br>수동 구현]
        F <--> G[Javascript 웹뷰]
    end

    subgraph WebView RPC 방식
        direction LR
        H[Unity C#<br>메서드 직접 호출] <--> I[마법의 공간]
        I <--> J[JavaScript<br>메서드 직접 호출]
    end
```

### 내부 구현
WebView RPC는 내부적으로는 다음과 같은 구조로 구현되어 있습니다.
```mermaid
flowchart LR
    A[Unity C# 코드<br>직접 호출] <--> B[protobuf로<br>생성된 C# 코드] <--> C[protobuf<br>시리얼/디시리얼라이저] <--> D[Base64<br>시리얼/디시리얼라이저] <--> E[JavaScript Bridge] <--> F[Base64<br>시리얼/디시리얼라이저] <--> G[protobuf<br>시리얼/디시리얼라이저] <--> H[protobuf로<br>생성된 JavaScript 코드] <--> I[JavaScript<br>코드 직접 호출]
```
1. Unity C# 코드 직접 호출
    - Unity 쪽에서 RPC 인터페이스 함수를 일반 메서드처럼 호출
2. protobuf로 생성된 C# 코드
    - Proto 정의 파일을 바탕으로 자동 생성된 C# 래퍼/스텁
    - RPC 메서드 및 데이터 구조가 protobuf 기반
3. Base64 시리얼라이저 + JavaScript Bridge
    - 실제 바이트 데이터를 Base64로 변환하여 WebView(브라우저)로 전송
    - JavaScript 측에서도 동일 포맷으로 수신
4. protobuf로 생성된 JavaScript 코드
    - 동일한 Proto 정의를 사용해 자동 생성된 JS 코드
    - C#에서 넘어온 직렬화 데이터를 역직렬화한 뒤, JS 메서드를 직접 호출

이처럼 WebView RPC를 사용하면 C#과 JavaScript 간 상호 호출이 단순 함수 호출처럼 동작하며, 반복되는 JSON 파싱/Bridge 처리를 크게 줄일 수 있습니다.
프로젝트 규모가 커질수록 이 구조가 훨씬 간결하고 유지보수하기 쉬워집니다.

## 샘플 프로젝트 제공
### Unity Example
Clone this whole repository.
> [!NOTE]
> 샘플을 실행하기 위해서는 [Viewplex Webview Asset(유료)](https://assetstore.unity.com/publishers/40309?locale=ko-KR&srsltid=AfmBOoqtnjTpJ-pw_5iGoS88XRtGX-tY2eJmP86PYoYCOhxrvz1OXRaJ)이 필요합니다.
### Javascript Example
[webview_rpc_sample](https://github.com/kwan3854/Unity-WebViewRpc/tree/main/webview_rpc_sample)
```bash
# 1. Move to sample directory
cd webview_rpc_sample

# 2. Install dependencies
npm install

# 3. Build project
npm run build
```

## 설치
### 유니티 프로젝트에 WebView RPC 추가하기
1. Nuget 패키지 매니저를 통해 `Protobuf` 패키지를 설치합니다.
2. 패키지 매니저를 통해 설치하거나 OpenUpm을 통해 WebViewRpc 패키지를 설치합니다.
- `Packages/manifest.json`
   ```json
  {
    "dependencies": {
      "com.kwanjoong.webviewrpc": "https://github.com/kwan3854/Unity-WebViewRpc.git?path=/Packages/WebviewRpc"
    }
  }
  ```
- 또는 Package Manager에서
  1. `Window` → `Package Manager` → `Add package from git URL...`
  2. `https://github.com/kwan3854/Unity-WebViewRpc.git?path=/Packages/WebviewRpc` 입력
- 또는 OpenUpm에서
   ```bash
   openupm add com.kwanjoong.webviewrpc
   ```
### 자바스크립트 라이브러리 추가하기
WebView RPC는 [npm 패키지](https://www.npmjs.com/package/app-webview-rpc)로 배포되고 있습니다.
#### Install
```bash
npm install app-webview-rpc@2.0.11
```

#### Usage
```javascript
import { VuplexBridge, WebViewRpcClient, WebViewRpcServer } from 'app-webview-rpc';

// RPC client
const bridge = new VuplexBridge();
const rpcClient = new WebViewRpcClient(bridge);

// RPC server
const rpcServer = new WebViewRpcServer(bridge);
```

### protobuf 파일 생성기 설치
#### protobuf 파일을 C#과 javascript로 변환하기 위해 `protoc` 컴파일러를 사용합니다.
**Mac**
```bash
brew install protobuf
protoc --version  # Ensure compiler version is 3+
```
**Windows**
```bash
winget install protobuf
protoc --version # Ensure compiler version is 3+
```

**Linux**
```bash
apt install -y protobuf-compiler
protoc --version  # Ensure compiler version is 3+
```

#### WebView RPC 코드 생성기 설치
[WebViewRPC 코드 생성기 레포지토리의 Release 페이지](https://github.com/kwan3854/ProtocGenWebviewRpc)에서 최신 릴리즈를 다운로드 받습니다.
- Windows: `protoc-gen-webviewrpc.exe`
- Mac: `protoc-gen-webviewrpc`
- Linux: 제공하지 않습니다.(직접 빌드 필요)


## 대용량 메시지를 위한 청킹(분할 전송) 기능

특정 안드로이드 WebView와 같이 JavaScript 브리지 통신에 약 1KB의 메시지 크기 제한이 있는 환경에 대응하기 위해, WebView RPC는 청킹(Chunking) 기능을 포함하고 있습니다. 이 기능을 사용하면 대용량 메시지를 작은 조각으로 나누어 전송하고 수신 측에서 재조립할 수 있습니다.

### 동작 방식

청킹이 활성화되면, `MaxChunkSize`를 초과하는 모든 메시지는 자동으로 더 작은 부분으로 분할됩니다. 각 청크는 개별적으로 전송된 후 수신자에 의해 재조립됩니다. 이 과정은 라이브러리 내부에서 투명하게 처리됩니다.

청킹 시스템은 다음 사항들을 자동으로 고려합니다:
- **RPC 엔벨로프 오버헤드**: 메타데이터(requestId, method, chunkInfo)를 위한 약 150바이트
- **Base64 인코딩 오버헤드**: 약 34% 크기 증가
- **실효 페이로드 크기**: 모든 오버헤드를 제외한 실제 사용 가능한 데이터 크기

### 설정 옵션

WebView RPC는 `WebViewRpcConfiguration`을 통해 여러 설정 옵션을 제공합니다:

#### 1. 청킹 활성화/비활성화

청킹 기능의 활성화 여부를 제어합니다. 비활성화하면 큰 메시지가 그대로 전송되며, 크기 제한이 있는 환경에서는 실패할 수 있습니다.

**Unity (C#)**
```csharp
WebViewRpcConfiguration.EnableChunking = true;  // 기본값: true
```

**JavaScript**
```javascript
WebViewRpcConfiguration.enableChunking = true;  // 기본값: true
```

#### 2. 최대 청크 크기

최종 Base64 인코딩된 메시지의 최대 크기를 설정합니다. 라이브러리는 인코딩 오버헤드를 고려하여 실효 페이로드 크기를 자동으로 계산합니다.

**Unity (C#)**
```csharp
// 최대 청크 크기를 900바이트로 설정 (1KB 제한 환경에 권장)
WebViewRpcConfiguration.MaxChunkSize = 900;  // 기본값: 256KB
// 최소: ~335바이트 (동적으로 계산됨)
// 최대: 10MB
```

**JavaScript**
```javascript
// 최대 청크 크기를 900바이트로 설정
WebViewRpcConfiguration.maxChunkSize = 900;  // 기본값: 256KB
```

#### 3. 청크 타임아웃

불완전한 청크 세트를 정리하기 전까지 대기하는 시간을 설정합니다.

**Unity (C#)**
```csharp
WebViewRpcConfiguration.ChunkTimeoutSeconds = 30;  // 기본값: 30초
// 최소: 5초
// 최대: 300초 (5분)
```

**JavaScript**
```javascript
WebViewRpcConfiguration.chunkTimeoutSeconds = 30;  // 기본값: 30초
```

#### 4. 최대 동시 청크 세트

메모리 고갈을 방지하기 위해 동시에 처리할 수 있는 청크 메시지의 수를 제한합니다.

**Unity (C#)**
```csharp
WebViewRpcConfiguration.MaxConcurrentChunkSets = 100;  // 기본값: 100
// 최소: 10
// 최대: 1000
```

**JavaScript**
```javascript
WebViewRpcConfiguration.maxConcurrentChunkSets = 100;  // 기본값: 100
```

### 유틸리티 메서드

WebView RPC는 청크 크기를 이해하고 최적화하는 데 도움이 되는 유틸리티 메서드를 제공합니다:

**Unity (C#)**
```csharp
// 오버헤드를 제외한 실제 사용 가능한 페이로드 크기 확인
int effectiveSize = WebViewRpcConfiguration.GetEffectivePayloadSize();
Console.WriteLine($"실효 페이로드 크기: {effectiveSize} 바이트");

// 최소 안전 청크 크기 확인
int minSize = WebViewRpcConfiguration.GetMinimumSafeChunkSize();
Console.WriteLine($"최소 청크 크기: {minSize} 바이트");

// 현재 설정 유효성 검증
bool isValid = WebViewRpcConfiguration.IsChunkSizeValid();
if (!isValid)
{
    Console.WriteLine("경고: 청크 크기가 너무 작아 의미 있는 페이로드를 담을 수 없습니다");
}
```

**JavaScript**
```javascript
// 오버헤드를 제외한 실제 사용 가능한 페이로드 크기 확인
const effectiveSize = WebViewRpcConfiguration.getEffectivePayloadSize();
console.log(`실효 페이로드 크기: ${effectiveSize} 바이트`);

// 최소 안전 청크 크기 확인
const minSize = WebViewRpcConfiguration.getMinimumSafeChunkSize();
console.log(`최소 청크 크기: ${minSize} 바이트`);

// 현재 설정 유효성 검증
const isValid = WebViewRpcConfiguration.isChunkSizeValid();
if (!isValid) {
    console.warn("청크 크기가 너무 작아 의미 있는 페이로드를 담을 수 없습니다");
}
```

### 모범 사례

1. **크기 설정**: 1KB 제한이 있는 안드로이드 WebView의 경우 `MaxChunkSize = 900` 사용을 권장
2. **일관된 설정**: Unity와 JavaScript가 동일한 설정 값을 사용하도록 보장
3. **성능 모니터링**: 작은 실효 페이로드 크기에 대한 경고가 표시되면 `MaxChunkSize` 증가 고려
4. **타임아웃 조정**: 네트워크 상태와 메시지 크기에 따라 `ChunkTimeoutSeconds` 조정
5. **테스트**: 예상되는 최대 크기의 메시지로 청킹이 원활하게 작동하는지 테스트

### 예시: 전체 설정

#### 시나리오 1: 동일한 설정 (권장)
양방향 통신에서 일관성을 위해 양쪽을 동일하게 설정하는 경우:

**Unity (C#)**
```csharp
void Start()
{
    // 가장 제한적인 환경(Android WebView 1KB)에 맞춤
    WebViewRpcConfiguration.EnableChunking = true;
    WebViewRpcConfiguration.MaxChunkSize = 900;
    WebViewRpcConfiguration.ChunkTimeoutSeconds = 60;
    WebViewRpcConfiguration.MaxConcurrentChunkSets = 50;
    
    // 설정 검증
    int effectiveSize = WebViewRpcConfiguration.GetEffectivePayloadSize();
    Debug.Log($"송신 시 청크당 실효 페이로드: {effectiveSize} 바이트");
    
    // RPC 초기화
    var bridge = new ViewplexWebViewBridge(webViewPrefab);
    var server = new WebViewRPC.WebViewRpcServer(bridge);
}
```

**JavaScript**
```javascript
// Unity와 동일하게 설정 (양방향 일관성)
WebViewRpcConfiguration.enableChunking = true;
WebViewRpcConfiguration.maxChunkSize = 900;
WebViewRpcConfiguration.chunkTimeoutSeconds = 60;
WebViewRpcConfiguration.maxConcurrentChunkSets = 50;

const effectiveSize = WebViewRpcConfiguration.getEffectivePayloadSize();
console.log(`송신 시 청크당 실효 페이로드: ${effectiveSize} 바이트`);

const bridge = new VuplexBridge();
const rpcServer = new WebViewRpcServer(bridge);
```

#### 시나리오 2: 서로 다른 청크 크기 설정
같은 플랫폼 제약 내에서 각 측이 다른 청크 크기를 사용하는 경우:

**Android WebView 환경 예시 (양방향 1KB 제한)**
```csharp
// Unity (C#)
void Start()
{
    WebViewRpcConfiguration.EnableChunking = true;
    WebViewRpcConfiguration.MaxChunkSize = 990;  // Unity는 990바이트 청크 사용
    WebViewRpcConfiguration.ChunkTimeoutSeconds = 60;
    WebViewRpcConfiguration.MaxConcurrentChunkSets = 50;
    
    Debug.Log("Unity: 990바이트 청크로 송신, 모든 크기 수신 가능");
}
```

```javascript
// JavaScript
WebViewRpcConfiguration.enableChunking = true;
WebViewRpcConfiguration.maxChunkSize = 800;  // JS는 800바이트 청크 사용
WebViewRpcConfiguration.chunkTimeoutSeconds = 60;
WebViewRpcConfiguration.maxConcurrentChunkSets = 50;

console.log("JS: 800바이트 청크로 송신, 모든 크기 수신 가능");
```

**일반 브라우저 환경 예시 (큰 메시지 지원)**
```csharp
// Unity (C#)
WebViewRpcConfiguration.MaxChunkSize = 1024 * 1024;  // 1MB 청크
```

```javascript
// JavaScript  
WebViewRpcConfiguration.maxChunkSize = 256 * 1024;  // 256KB 청크
```

> 동일한 플랫폼 제약 하에서도 각 측이 다른 청크 크기를 사용할 수 있지만, 실용적 이점은 거의 없습니다.

> [!NOTE]
> **청킹 설정의 독립성과 플랫폼 제약**
> 
> **플랫폼/브릿지 제약은 양방향 모두에 동일하게 적용됩니다**:
> - Android WebView 환경: Unity ↔ JavaScript 양방향 모두 ~1KB 제한
> - iOS WKWebView 환경: Unity ↔ JavaScript 양방향 모두 더 큰 메시지 지원
> - 일반 브라우저 환경: 대부분 큰 메시지 지원
> 
> **기술적 유연성**:
> - **송신 측**: 플랫폼 제약 내에서 자유롭게 MaxChunkSize 설정 가능
> - **수신 측**: 어떤 크기의 청크든 받아서 조립 가능 (MaxChunkSize는 수신에 영향 없음)
> 
> **예시 - Android WebView 환경 (1KB 제한)**:
> - Unity 측: MaxChunkSize = 990 바이트로 설정하여 송신
> - JavaScript 측: MaxChunkSize = 800 바이트로 설정하여 송신
> - 양쪽 모두 상대방이 보낸 청크를 문제없이 수신/조립
> 
> **권장사항**:
> 1. **필수**: 사용 중인 플랫폼/브릿지의 메시지 크기 제한 확인
> 2. **선택적 통일**: 유지보수와 디버깅을 위해 양쪽을 동일하게 설정
> 3. **수신 설정**: ChunkTimeoutSeconds와 MaxConcurrentChunkSets는 수신 측 동작에 영향


## 빠른 시작
HelloWorld 라는 간단한 RPC 서비스를 구현하고, 유니티 클라이언트와 웹뷰 클라이언트 간의 통신을 확인해보겠습니다.

HelloWorld 서비스는 `HelloRequest` 메시지를 받아 `HelloResponse` 메시지를 반환하는 간단한 RPC 서비스입니다.

우선, C#측이 서버 역할을 하고, javascript 쪽이 클라이언트 역할을 하는 예시를 살펴봅시다.

### protobuf 정의 하기
- protobuf는 서비스의 요청과 응답 형삭을 정의하는데 사용됩니다.
- 유니티 클라이언트와 웹뷰가 통신할 사항이 생겼을 때 회의를 통해 protobuf를 정의합시다.
- 아래의 예시는 `HelloWorld.proto` 파일로, `HelloRequest`, `HelloResponse` 메시지와 `HelloService` 서비스를 정의하였습니다.
- RPC를 요청하는 클라이언트 측(이번예시에서는 javascript)이 SayHello 메서드를 호출하면, 서버 측(이번예시에서는 C#)에서는 SayHello 메서드를 구현하여 요청을 처리하고 응답을 반환합니다.

**HelloWorld.proto**
```protobuf
syntax = "proto3";

package helloworld;

// (C#에서 생성될 때 네임스페이스로 쓰일 수 있음)
option csharp_namespace = "HelloWorld";

// 요청 메시지
message HelloRequest {
  string name = 1;
}

// 응답 메시지
message HelloResponse {
  string greeting = 1;
}

// 간단한 서비스 예시
service HelloService {
  // [단방향] Request -> Response
  rpc SayHello (HelloRequest) returns (HelloResponse);
}
```
### protobuf 파일을 C#과 javascript로 변환하기
- protobuf 파일을 C#과 javascript로 변환하기 위해 `protoc` 컴파일러를 사용합니다.
- `protoc` 컴파일러는 protobuf 파일을 C#과 javascript로 변환하는데 사용됩니다.
- WebView RPC를 위한 [별도의 커스터마이징 된 코드 생성기](https://github.com/kwan3854/ProtocGenWebviewRpc)가 준비되어 있습니다.
- 아래의 명령어를 통해 protobuf 파일을 C#과 javascript로 변환합니다.

**C# 공용 코드 생성하기 (C#측이 서버가 되든, 클라이언트가 되든 공용으로 사용되는 코드)**
```bash
protoc -I. --csharp_out=. HelloWorld.proto 

// 결과물로 HelloWorld.cs 파일이 생성됩니다.
```

**C# 서버 코드 생성하기**
```bash
protoc \
  --plugin=protoc-gen-webviewrpc=./protoc-gen-webviewrpc \
  --webviewrpc_out=cs_server:. \
  -I. HelloWorld.proto
  
// 결과물로 HelloWorld_HelloServiceBase.cs 파일이 생성됩니다.
```

**javascript 공용 코드 생성하기 (javascript측이 클라이언트가 되든, 서버가 되든 공용으로 사용되는 코드)**
> [!IMPORTANT]
> [pbjs 라이브러리가 필요합니다.](https://www.npmjs.com/package/pbjs)
```bash
npx pbjs HelloWorld.proto --es6 hello_world.js

// 결과물로 hello_world.js 파일이 생성됩니다.
// 결과물 이름은 proto 파일에서 Service 이름을 따서 생성하는 것을 권장합니다.
```

**javascript 클라이언트 코드 생성하기**
```bash
protoc \
  --plugin=protoc-gen-webviewrpc=./protoc-gen-webviewrpc \
  --webviewrpc_out=js_client:. \
  -I. HelloWorld.proto
  
// 결과물로 HelloWorld_HelloServiceClient.js 파일이 생성됩니다.
```

### 생성된 코드를 각자의 프로젝트에 추가하기
- 생성된 코드를 각자의 프로젝트에 추가합니다.
- Github action을 통해 자동으로 생성된 코드가 생성되고 프로젝트에 추가되도록 할 수 있습니다.

### 브릿지 코드 구현하기
- 브릿지 코드는 C#과 javascript 간의 통신을 중계하는 역할을 합니다.
- 어떠한 웹뷰 라이브러리를 사용하더라도 WebViewRpc 를 사용할 수 있도록 구현이 추상화되어있습니다.
- 각자의 웹뷰 라이브러리에 맞는 브릿지 코드를 구현합니다.
- 아래는 Viewplex의 CanvasWebViewPrefab을 사용하는 예시입니다.

```csharp
using System;
using Vuplex.WebView;
using WebViewRPC;

public class ViewplexWebViewBridge : IWebViewBridge
{
    public event Action<string> OnMessageReceived;
    private readonly CanvasWebViewPrefab _webViewPrefab;
    
    public ViewplexWebViewBridge(CanvasWebViewPrefab webViewPrefab)
    {
        _webViewPrefab = webViewPrefab;
        
        _webViewPrefab.WebView.MessageEmitted += (sender, args) =>
        {
            OnMessageReceived?.Invoke(args.Value);
        };
    }
    
    public void SendMessageToWeb(string message)
    {
        _webViewPrefab.WebView.PostMessage(message);
    }
}
```
```javascript
export class VuplexBridge {
    constructor() {
        this._onMessageCallback = null;
        this._isVuplexReady = false;
        this._pendingMessages = [];

        // 1) 만약 window.vuplex가 이미 있으면 바로 사용
        if (window.vuplex) {
            this._isVuplexReady = true;
        } else {
            // 아직 window.vuplex가 없으므로 'vuplexready' 이벤트 대기
            window.addEventListener('vuplexready', () => {
                this._isVuplexReady = true;
                // 대기중이던 메시지들을 모두 보냄
                for (const msg of this._pendingMessages) {
                    window.vuplex.postMessage(msg);
                }
                this._pendingMessages = [];
            });
        }

        // 2) C# -> JS 메시지: "vuplexmessage" 이벤트
        //    event.value 에 문자열이 들어있음 (C# PostMessage로 보낸)
        window.addEventListener('vuplexmessage', event => {
            const base64Str = event.value; // 보통 Base64
            if (this._onMessageCallback) {
                this._onMessageCallback(base64Str);
            }
        });
    }

    /**
     * JS -> C# 문자열 전송 (base64Str)
     */
    sendMessage(base64Str) {
        // Vuplex는 전달된 인자가 JS object이면 JSON 직렬화해 보내지만,
        // 우리가 'string'을 넘기면 'string' 그대로 보냄.
        if (this._isVuplexReady && window.vuplex) {
            window.vuplex.postMessage(base64Str);
        } else {
            // 아직 vuplex가 준비 안됐으면 대기열에 저장
            this._pendingMessages.push(base64Str);
        }
    }

    /**
     * onMessage(cb): JS가 C#으로부터 문자열을 받을 때 콜백 등록
     */
    onMessage(cb) {
        this._onMessageCallback = cb;
    }
}
```

### C# 서버와 javascript 클라이언트 코드 작성하기

```csharp
public class WebViewRpcTester : MonoBehaviour
{
    [SerializeField] private CanvasWebViewPrefab webViewPrefab;

    private async void Start()
    {
        await InitializeWebView(webViewPrefab);
        
        // 브릿지 생성
        var bridge = new ViewplexWebViewBridge(webViewPrefab);
        // 서버 생성
        var server = new WebViewRPC.WebViewRpcServer(bridge)
        {
            Services =
            {
                // HelloService를 바인딩
                HelloService.BindService(new HelloWorldService()),
                // 다른 서비스도 추가 가능
            }
        };
        
        // 서버 시작
        server.Start();
    }
    
    private async Task InitializeWebView(CanvasWebViewPrefab webView)
    {
        // 예시로 사용한 WebViewPrefab은 Viewplex의 CanvasWebViewPrefab을 사용하였습니다.
        await webView.WaitUntilInitialized();
        webView.WebView.LoadUrl("http://localhost:8081");
        await webView.WebView.WaitForNextPageLoadToFinish();
    }
}
```
```csharp
using Cysharp.Threading.Tasks;
using HelloWorld;
using UnityEngine;

namespace SampleRpc
{
    // HelloServiceBase를 상속받아 SayHello 메서드를 구현
    // HelloServiceBase는 HelloWorld.proto에서 생성된 코드입니다.
    public class HelloWorldService : HelloServiceBase
    {
        public override async UniTask<HelloResponse> SayHello(HelloRequest request)
        {
            Debug.Log($"Received request: {request.Name}");
            
            // 비동기 작업 예시
            await UniTask.Delay(100);
            
            return new HelloResponse()
            {
                // 요청을 받아 응답을 반환
                Greeting = $"Hello, {request.Name}!"
            };
        }
    }
}
```
```javascript
// 1) 브리지 생성
const bridge = new VuplexBridge();
// 2) RpcClient 생성
const rpcClient = new WebViewRpcClient(bridge);
// 3) HelloServiceClient
const helloClient = new HelloServiceClient(rpcClient);

document.getElementById('btnSayHello').addEventListener('click', async () => {
    try {
        const reqObj = {name: "Hello World! From WebView"};
        console.log("Request to Unity: ", reqObj);

        const resp = await helloClient.SayHello(reqObj);
        console.log("Response from Unity: ", resp.greeting);
    } catch (err) {
        console.error("Error: ", err);
    }
});
```
### 실행하기
- 유니티에서 WebViewRpcTester 스크립트를 실행하고, 웹뷰를 띄웁니다.
- 웹뷰에서 버튼을 클릭하면, 유니티에서 HelloService를 통해 요청을 처리하고 응답을 반환합니다.

### 반대의 경우 (C# 클라이언트, javascript 서버)
- 반대의 경우도 동일하게 구현이 가능합니다.
- 이미 공용 코드가 생성되어 있으므로, C# 클라이언트와 javascript 서버 코드를 생성 합니다.

**C# 클라이언트 코드 생성하기**
```bash
protoc \
  --plugin=protoc-gen-webviewrpc=./protoc-gen-webviewrpc \
  --webviewrpc_out=cs_client:. \
  -I. HelloWorld.proto
```
**javascript 서버 코드 생성하기**
```bash
protoc \
  --plugin=protoc-gen-webviewrpc=./protoc-gen-webviewrpc \
  --webviewrpc_out=js_server:. \
  -I. HelloWorld.proto
```

**C# 클라이언트 코드 작성하기**
```csharp
public class WebViewRpcTester : MonoBehaviour
{
    [SerializeField] private CanvasWebViewPrefab webViewPrefab;

    private void Awake()
    {
        Web.ClearAllData();
    }

    private async void Start()
    {
        await InitializeWebView(webViewPrefab);
        
        // 브릿지 생성
        var bridge = new ViewplexWebViewBridge(webViewPrefab);
        // RpcClient 생성
        var rpcClient = new WebViewRPC.WebViewRpcClient(bridge);
        // HelloServiceClient 생성
        var client = new HelloServiceClient(rpcClient);
        
        // 요청 보내기
        var response = await client.SayHello(new HelloRequest()
        {
            Name = "World"
        });
        
        // 응답 확인
        Debug.Log($"Received response: {response.Greeting}");
    }
    
    private async Task InitializeWebView(CanvasWebViewPrefab webView)
    {
        await webView.WaitUntilInitialized();
        webView.WebView.LoadUrl("http://localhost:8081");
        await webView.WebView.WaitForNextPageLoadToFinish();
    }
}
```

**javascript 서버 코드 작성하기**
```javascript
// 1) 브리지 생성
const bridge = new VuplexBridge();
// 2) RpcServer 생성
const rpcServer = new WebViewRpcServer(bridge);
// 3) 서비스 구현체 생성
const impl = new MyHelloServiceImpl();
// 4) 서비스 바인딩
const def = HelloService.bindService(impl);
// 5) 서비스 등록
rpcServer.services.push(def);
// 6) 서버 시작
rpcServer.start();
```
```javascript
import { HelloServiceBase } from "./HelloWorld_HelloServiceBase.js";

// 자동 생성된 HelloWorld_HelloServiceBase.js의 HelloServiceBase를 상속받아 구현
export class MyHelloServiceImpl extends HelloServiceBase {
    async SayHello(requestObj) {
        // 받은 요청을 확인
        console.log("JS Server received: ", requestObj);
        
        // 비동기 작업 예시
        await new Promise(resolve => setTimeout(resolve, 100));
        
        // 요청을 받아 응답을 반환
        return { greeting: "Hello from JS! I got your message: " + requestObj.name };
    }
}

```

## License
This project is licensed under the MIT License - see the [LICENSE.md](LICENSE) file for details.