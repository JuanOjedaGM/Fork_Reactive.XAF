﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using akarnokd.reactive_extensions;
using HarmonyLib;
using Moq;
using Moq.Language.Flow;
using Moq.Protected;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Xpand.Extensions.StringExtensions;

namespace Xpand.TestsLib.Common {
    public class HarmonyTest:Harmony {
        public HarmonyTest() : base(TestContext.CurrentContext.Test.FullName){
        }
    }
    public static class MockExtensions {
        private static Mock<HttpWebResponse> _mockResponse;
        private static Func<Uri,bool> _matchUri;

        public static Mock<HttpWebResponse> MockResponse(this TestScheduler testScheduler, HttpStatusCode statusCode,DateTimeOffset? date=null,string responseString=null) {
            responseString ??= TestContext.CurrentContext.Test.Name;
            date ??= testScheduler.Now;
            var mockResponse = new Mock<HttpWebResponse>();
            mockResponse.Setup(response => response.GetResponseStream())
                .Returns(new MemoryStream(responseString.Bytes()));
            mockResponse.Setup(response => response.StatusCode).Returns(statusCode);
            mockResponse.Setup(response => response.Headers).Returns(new WebHeaderCollection{ { "Date", date.ToString() } });
            return mockResponse;
        }
        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        static bool WebRequestCreate(Uri requestUri, ref WebRequest __result){
            if (_matchUri(requestUri)){
                var mockRequest = new Mock<HttpWebRequest>();
                var timer = Observable.Timer(TimeSpan.FromMilliseconds(200));
                if (_mockResponse.Object.StatusCode != HttpStatusCode.OK) {
                    mockRequest.Setup(request => request.GetResponseAsync())
                        .Returns(() => timer.SelectMany(_ =>
                            Observable.Throw<WebResponse>((new WebException("", null, WebExceptionStatus.ReceiveFailure,
                                _mockResponse.Object)))).ToTask());    
                }
                else {
                    mockRequest.Setup(request => request.GetResponseAsync())
                        .Returns(() => timer.Select(_ => _mockResponse.Object).Cast<WebResponse>().ToTask());
                }
                
                __result = mockRequest.Object;
                return false;
            }
            return true;
        }

        public static IDisposable PatchWebRequest(this  Harmony harmony,Func<Uri,bool> matchUri,Mock<HttpWebResponse> mockResoponse){
            var methodInfo = typeof(WebRequest).GetMethod(nameof(WebRequest.CreateHttp),new[]{typeof(Uri)});
            var harmonyMethod = new HarmonyMethod(typeof(MockExtensions), nameof(WebRequestCreate));
            harmony.Patch(methodInfo, harmonyMethod);
            _matchUri = matchUri;
            _mockResponse=mockResoponse;
            return Observable.While(() => TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Inconclusive, Observable.Empty<Unit>())
                .Finally(() => harmony.Unpatch(methodInfo, harmonyMethod.method))
                .SubscribeOn(Scheduler.Default).Subscribe();
        }

        public static Mock<T> GetMock<T>(this T t) where T : class => Mock.Get(t);

        public static void VerifySend(this Mock<HttpMessageHandler> handlerMock, Times times,Func<HttpRequestMessage, bool> filter) 
            => handlerMock.Protected().Verify("SendAsync", times, ItExpr.Is<HttpRequestMessage>(message => filter==null||filter(message)),
                ItExpr.IsAny<CancellationToken>());

        public static IReturnsResult<HttpMessageHandler> SetupSend(this Mock<HttpMessageHandler> handlerMock, Action<HttpResponseMessage> configure,IScheduler scheduler=null) {
            scheduler??=Scheduler.Default;
            return handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(async (HttpRequestMessage requestMessage, CancellationToken _)
                    => await Observable.Start(() => new HttpResponseMessage {
                            StatusCode = HttpStatusCode.OK,
                            RequestMessage = requestMessage
                        }).Delay(TimeSpan.FromMilliseconds(50),scheduler)
                        .Do(configure));
        }
    }
}