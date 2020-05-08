using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Polly.Timeout;
using Simple.HttpClientFactory.MessageHandlers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Simple.HttpClientFactory.Tests
{
    public class ExceptionTranslatorTests
    {
        private readonly WireMockServer _server;
        private readonly List<string> _visitedMiddleware = new List<string>();

        public ExceptionTranslatorTests()
        {
            _server = WireMockServer.Start();
            _server.Given(Request.Create().WithPath("/hello/world").UsingAnyMethod())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "text/plain")
                        .WithBody("Hello world!"));       
            
            _server
                .Given(Request.Create()
                    .WithPath("/timeout")
                    .UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(408));
        }

        public class TestException : Exception
        {
            public TestException(string message) : base(message)
            {
            }
        }

        [Fact]
        public void Exception_messege_handler_ctor_should_validate_first_param() => 
            Assert.Throws<ArgumentNullException>(() => new ExceptionTranslatorRequestMiddleware(null, e => e));

        [Fact]
        public void Exception_messege_handler_ctor_should_validate_second_param() => 
            Assert.Throws<ArgumentNullException>(() => new ExceptionTranslatorRequestMiddleware(e => true, null));

        [Fact]
        public void Exception_messege_handler_ctor_should_validate_first_param_overload() => 
            Assert.Throws<ArgumentNullException>(() => new ExceptionTranslatorRequestMiddleware(null, e => e, new ExceptionTranslatorRequestMiddleware(e => true, e => e)));

        [Fact]
        public void Exception_messege_handler_ctor_should_validate_second_param_overload() => 
            Assert.Throws<ArgumentNullException>(() => new ExceptionTranslatorRequestMiddleware(e => true, null, new ExceptionTranslatorRequestMiddleware(e => true, e => e)));

        [Fact]
        public async Task Exception_translator_can_translate_exception_types()
        {
            var clientWithRetry = HttpClientFactory.Create()
                .WithMessageExceptionHandler(ex => true, ex => new TestException(ex.Message))
                .WithPolicy(
                    Policy<HttpResponseMessage>
                        .Handle<HttpRequestException>()
                        .OrResult(result => (int)result.StatusCode >= 500 || result.StatusCode == HttpStatusCode.RequestTimeout)
                        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
                .WithPolicy(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(4), TimeoutStrategy.Optimistic))
                .Build();

            await Assert.ThrowsAsync<TestException>(() => clientWithRetry.GetAsync(_server.Urls[0] + "/timeout"));
            Assert.Equal(4, _server.LogEntries.Count());
            
        }


        [Fact]
        public async Task Exception_translator_should_not_change_unhandled_exceptions()
        {
            var clientWithRetry = HttpClientFactory.Create()
                .WithMessageExceptionHandler(ex => true, ex => ex)
                .WithPolicy(
                    Policy<HttpResponseMessage>
                        .Handle<HttpRequestException>()
                        .OrResult(result => (int)result.StatusCode >= 500 || result.StatusCode == HttpStatusCode.RequestTimeout)
                        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
                .WithPolicy(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(4), TimeoutStrategy.Optimistic))
                .Build();

            await Assert.ThrowsAsync<HttpRequestException>(() => clientWithRetry.GetAsync(_server.Urls[0] + "/timeout"));
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
                        .OrResult(result => (int)result.StatusCode >= 500 || result.StatusCode == HttpStatusCode.RequestTimeout)
                        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
                .WithPolicy(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(4), TimeoutStrategy.Optimistic))
                .Build();

            await Assert.ThrowsAsync<HttpRequestException>(() => clientWithRetry.GetAsync(_server.Urls[0] + "/timeout"));
            Assert.Equal(4, _server.LogEntries.Count());
            
        }

        [Fact]
        public async Task Exception_translator_without_errors_should_not_affect_anything()
        {
            var trafficRecorderMessageHandler = new TrafficRecorderMessageHandler(_visitedMiddleware);
            var eventMessageHandler = new EventMessageHandler(_visitedMiddleware);

            var client = HttpClientFactory.Create()
                .WithMessageExceptionHandler(ex => true, ex => ex)
                .WithMessageHandler(eventMessageHandler)
                .WithMessageHandler(trafficRecorderMessageHandler)
                .Build();

            var raisedEvent = await Assert.RaisesAsync<EventMessageHandler.RequestEventArgs>(
                h => eventMessageHandler.Request += h,
                h => eventMessageHandler.Request -= h,
                () => client.GetAsync(_server.Urls[0] + "/hello/world"));

            Assert.True(raisedEvent.Arguments.Request.Headers.Contains("foobar"));
            Assert.Equal("foobar",raisedEvent.Arguments.Request.Headers.GetValues("foobar").FirstOrDefault());
            Assert.Single(trafficRecorderMessageHandler.Traffic);

            Assert.Equal(HttpStatusCode.OK, trafficRecorderMessageHandler.Traffic[0].Item2.StatusCode);
            Assert.Equal(new [] { nameof(TrafficRecorderMessageHandler), nameof(EventMessageHandler) }, _visitedMiddleware);
        }

    }
}
