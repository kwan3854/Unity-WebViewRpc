import { HelloServiceBase } from "./HelloWorld_HelloServiceBase.js";
// (자동생성된 코드, 위 예시)

export class MyHelloServiceImpl extends HelloServiceBase {
    SayHello(requestObj) {
        console.log("JS Server received: ", requestObj);
        return { greeting: "Hello from JS! I got your message: " + requestObj.name };
    }
}
