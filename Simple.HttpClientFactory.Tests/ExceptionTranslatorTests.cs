using Simple.HttpClientFactory.MessageHandlers;
using Simple.HttpClientFactory.Tests.MessageHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FakeItEasy;
using Polly;
using Polly.Timeout;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Simple.HttpClientFactory.Tests
{
    public class ExceptionTranslatorTests
    {
        private const string _endpointUri = "/hello/world";
        private const string _endpointUriTimeout = "/timeout";

        private readonly WireMockServer _server;

        public ExceptionTranslatorTests()
        {
            _server = WireMockServer.Start();
            _server.Given(Request.Create().WithPath(_endpointUri).UsingAnyMethod())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithHeader("Content-Type", "text/plain")
                        .WithBody("Hello world!"));       
            
            _server
                .Given(Request.Create()
                    .WithPath(_endpointUriTimeout)
                    .UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.RequestTimeout));
        }

        public class TestException : Exception
        {
            public TestException(string message) : base(message) { }
        }

        [Fact]
        public void Exception_message_handler_ctor_should_validate_first_param()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithMessageExceptionHandler(null, e => e));
            Assert.Equal("exceptionHandlingPredicate", exception.ParamName);
        }

        [Fact]
        public void Exception_message_handler_ctor_should_validate_second_param()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithMessageExceptionHandler(e => true, null));
            Assert.Equal("exceptionHandler", exception.ParamName);
        }

        [Fact]
        public async Task Exception_translator_should_call_request_exception_event_handler_on_exception_if_provided()
        {
            var requestExceptionEventHandler = A.Fake<EventHandler<HttpRequestException>>();

            var client = HttpClientFactory.Create()
                .WithMessageExceptionHandler(ex => true, ex => ex, requestExceptionEventHandler)
                .Build();

            await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"{_server.Urls[0]}{_endpointUriTimeout}"));

            A.CallTo(() => requestExceptionEventHandler.Invoke(A<ExceptionTranslatorRequestMiddleware>.That.IsNotNull(), A<HttpRequestException>.That.IsNotNull())).MustHaveHappenedOnceExactly();
        }


        [Fact]
        public async Task Exception_translator_can_translate_exception_types()
        {
            var transformedRequestExceptionEventHandler = A.Fake<EventHandler<Exception>>();

            var clientWithRetry = HttpClientFactory.Create()
                .WithMessageExceptionHandler(ex => true, ex => new TestException(ex.Message), null, transformedRequestExceptionEventHandler)
                .WithPolicy(
                    Policy<HttpResponseMessage>
                        .Handle<HttpRequestException>()
                        .OrResult(result => result.StatusCode >= HttpStatusCode.InternalServerError || result.StatusCode == HttpStatusCode.RequestTimeout)
                        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
                .WithPolicy(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(4), TimeoutStrategy.Optimistic))
                .Build();

            await Assert.ThrowsAsync<TestException>(() => clientWithRetry.GetAsync($"{_server.Urls[0]}{_endpointUriTimeout}"));
            Assert.Equal(4, _server.LogEntries.Count());
            A.CallTo(() => transformedRequestExceptionEventHandler.Invoke(A<ExceptionTranslatorRequestMiddleware>.That.IsNotNull(), A<Exception>.That.IsNotNull())).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Exception_translator_should_throw_if_translation_resolves_to_null()
        {
            var requestExceptionEventHandler            = A.Fake<EventHandler<HttpRequestException>>();
            var transformedRequestExceptionEventHandler = A.Fake<EventHandler<Exception>>();

            var clientWithRetry = HttpClientFactory.Create()
                .WithMessageExceptionHandler(ex => true, ex => null as Exception, requestExceptionEventHandler, transformedRequestExceptionEventHandler)
                .WithPolicy(
                    Policy<HttpResponseMessage>
                        .Handle<HttpRequestException>()
                        .OrResult(result => result.StatusCode >= HttpStatusCode.InternalServerError || result.StatusCode == HttpStatusCode.RequestTimeout)
                        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
                .WithPolicy(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(4), TimeoutStrategy.Optimistic))
                .Build();

            var exception = await Assert.ThrowsAsync<Exception>(() => clientWithRetry.GetAsync($"{_server.Urls[0]}{_endpointUriTimeout}"));

            Assert.IsType<HttpRequestException>(exception.InnerException);
            Assert.Equal(4, _server.LogEntries.Count());
            A.CallTo(() => requestExceptionEventHandler.Invoke(A<ExceptionTranslatorRequestMiddleware>.That.IsNotNull(), A<HttpRequestException>.That.IsNotNull())).MustHaveHappenedOnceExactly();
            A.CallTo(() => transformedRequestExceptionEventHandler.Invoke(A<ExceptionTranslatorRequestMiddleware>.That.IsNotNull(), A<Exception>.That.IsNotNull())).MustNotHaveHappened();
        }

        [Fact]
        public async Task Exception_translator_should_throw_when_offline_and_translation_resolves_to_null()
        {
            var requestExceptionEventHandler = A.Fake<EventHandler<HttpRequestException>>();
            var transformedRequestExceptionEventHandler = A.Fake<EventHandler<Exception>>();

            var client = HttpClientFactory.Create()
                .WithMessageExceptionHandler(ex => true, ex => null as Exception, requestExceptionEventHandler, transformedRequestExceptionEventHandler)
                .WithMessageHandler(new OfflineMessageHandler())
                .Build();

            var exception = await Assert.ThrowsAsync<Exception>(() => client.GetAsync($"{_server.Urls[0]}{_endpointUri}"));

            Assert.IsType<HttpRequestException>(exception.InnerException);
            Assert.Empty(_server.LogEntries);
            A.CallTo(() => requestExceptionEventHandler.Invoke(A<ExceptionTranslatorRequestMiddleware>.That.IsNotNull(), A<HttpRequestException>.That.IsNotNull())).MustHaveHappenedOnceExactly();
            A.CallTo(() => transformedRequestExceptionEventHandler.Invoke(A<ExceptionTranslatorRequestMiddleware>.That.IsNotNull(), A<Exception>.That.IsNotNull())).MustNotHaveHappened();
        }

        [Fact]
        public async Task Exception_translator_should_not_change_unhandled_exceptions()
        {
            var clientWithRetry = HttpClientFactory.Create()
                .WithMessageExceptionHandler(ex => true, ex => ex)
                .WithPolicy(
                    Policy<HttpResponseMessage>
                        .Handle<HttpRequestException>()
                        .OrResult(result => result.StatusCode >= HttpStatusCode.InternalServerError || result.StatusCode == HttpStatusCode.RequestTimeout)
                        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
                .WithPolicy(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(4), TimeoutStrategy.Optimistic))
                .Build();

            await Assert.ThrowsAsync<HttpRequestException>(() => clientWithRetry.GetAsync($"{_server.Urls[0]}{_endpointUriTimeout}"));
            Assert.Equal(4, _server.LogEntries.Count());
            
        }

        [Fact]
        public async Task Exception_translator_should_throw_original_exception_if_delegate_is_false()
        {
            var clientWithRetry = HttpClientFactory.Create()
                .WithMessageExceptionHandler(ex => false, ex => ex)
                .WithPolicy(
                    Policy<HttpResponseMessage>
                        .Handle<HttpRequestException>()
                        .OrResult(result => result.StatusCode >= HttpStatusCode.InternalServerError || result.StatusCode == HttpStatusCode.RequestTimeout)
                        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
                .WithPolicy(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(4), TimeoutStrategy.Optimistic))
                .Build();

            await Assert.ThrowsAsync<HttpRequestException>(() => clientWithRetry.GetAsync($"{_server.Urls[0]}{_endpointUriTimeout}"));
            Assert.Equal(4, _server.LogEntries.Count());
        }

        [Fact]
        public async Task Exception_translator_without_errors_should_not_affect_anything()
        {
            var requestEventHandler                     = A.Fake<EventHandler<EventMessageHandler.RequestEventArgs>>();
            var responseEventHandler                    = A.Fake<EventHandler<EventMessageHandler.ResponseEventArgs>>();
            var requestExceptionEventHandler            = A.Fake<EventHandler<HttpRequestException>>();
            var transformedRequestExceptionEventHandler = A.Fake<EventHandler<Exception>>();

            var visitedMiddleware = new List<string>();

            var trafficRecorderMessageHandler = new TrafficRecorderMessageHandler(visitedMiddleware);
            var eventMessageHandler = new EventMessageHandler(visitedMiddleware);
            eventMessageHandler.Request  += requestEventHandler;
            eventMessageHandler.Response += responseEventHandler;

            var client = HttpClientFactory.Create()
                .WithMessageExceptionHandler(ex => true, ex => ex, requestExceptionEventHandler, transformedRequestExceptionEventHandler)
                .WithMessageHandler(eventMessageHandler)
                .WithMessageHandler(trafficRecorderMessageHandler)
                .Build();


            await client.GetAsync($"{_server.Urls[0]}{_endpointUri}");

                  A.CallTo(() =>  requestEventHandler.Invoke(A<EventMessageHandler>.That.IsSameAs(eventMessageHandler), A<EventMessageHandler.RequestEventArgs>.That.Matches(e => e.Request.Headers.Single(h => h.Key == "foobar").Value.FirstOrDefault() == "foobar"))).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => responseEventHandler.Invoke(A<EventMessageHandler>.That.IsSameAs(eventMessageHandler), A<EventMessageHandler.ResponseEventArgs>.That.Matches(e => e.Response.Headers.Single(h => h.Key == "foobar").Value.FirstOrDefault() == "foobar"))).MustHaveHappenedOnceExactly());

            A.CallTo(() => requestExceptionEventHandler.Invoke(A<ExceptionTranslatorRequestMiddleware>._, A<HttpRequestException>._)).MustNotHaveHappened();
            A.CallTo(() => transformedRequestExceptionEventHandler.Invoke(A<ExceptionTranslatorRequestMiddleware>._, A<Exception>._)).MustNotHaveHappened();

            var traffic = Assert.Single(trafficRecorderMessageHandler.Traffic);

            Assert.Equal(HttpStatusCode.OK, traffic.Item2.StatusCode);
            Assert.Equal(new [] { nameof(TrafficRecorderMessageHandler), nameof(EventMessageHandler) }, visitedMiddleware);
        }
    }
}
