using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Simple.HttpClientFactory.Tests
{
    public class MiddlewareDelegateTests
    {
        private readonly WireMockServer _server;
        private readonly List<string> _visitedMiddleware = new List<string>();

        public MiddlewareDelegateTests()
        {
            _server = WireMockServer.Start();
            _server.Given(Request.Create().WithPath("/hello/world").UsingAnyMethod())
                   .RespondWith(
                       Response.Create()
                          .WithStatusCode(200)
                          .WithHeader("Content-Type", "text/plain")
                          .WithBody("Hello world!"));         
        }

        [Fact]
        public async Task Single_middleware_handler_should_work()
        {
            var trafficRecorderMessageHandler = new TrafficRecorderMessageHandler(_visitedMiddleware);
            var client = HttpClientFactory.Create()
                .WithHttpHandler(trafficRecorderMessageHandler)
                .Build();

            var _ = await client.GetAsync(_server.Urls[0] + "/hello/world");

            Assert.Single(trafficRecorderMessageHandler.Traffic);
            Assert.Equal(HttpStatusCode.OK, trafficRecorderMessageHandler.Traffic[0].Item2.StatusCode);
        }

        [Fact]
        public async Task Multiple_middleware_handlers_should_work()
        {
            var trafficRecorderMessageHandler = new TrafficRecorderMessageHandler(_visitedMiddleware);
            var eventMessageHandler = new EventMessageHandler(_visitedMiddleware);

            var client = HttpClientFactory.Create()
                .WithHttpHandler(eventMessageHandler)
                .WithHttpHandler(trafficRecorderMessageHandler)
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

        [Fact]
        public async Task Multiple_middleware_handlers_with_reverse_order_should_work()
        {
            var trafficRecorderMessageHandler = new TrafficRecorderMessageHandler(_visitedMiddleware);
            var eventMessageHandler = new EventMessageHandler(_visitedMiddleware);

            var client = HttpClientFactory.Create()
                .WithHttpHandler(trafficRecorderMessageHandler) //first execute this, then eventMessageHandler
                .WithHttpHandler(eventMessageHandler)
                .Build();

           var raisedEvent = await Assert.RaisesAsync<EventMessageHandler.RequestEventArgs>(
                h => eventMessageHandler.Request += h,
                h => eventMessageHandler.Request -= h,
                () => client.GetAsync(_server.Urls[0] + "/hello/world"));

            Assert.True(raisedEvent.Arguments.Request.Headers.Contains("foobar"));
            Assert.Equal("foobar",raisedEvent.Arguments.Request.Headers.GetValues("foobar").FirstOrDefault());
            Assert.Single(trafficRecorderMessageHandler.Traffic);

            Assert.Equal(HttpStatusCode.OK, trafficRecorderMessageHandler.Traffic[0].Item2.StatusCode);

            Assert.Equal(new [] { nameof(EventMessageHandler), nameof(TrafficRecorderMessageHandler) }, _visitedMiddleware);
        }

        public class EventMessageHandler : DelegatingHandler
        {
            public event EventHandler<RequestEventArgs> Request;
            public event EventHandler<ResponseEventArgs> Response;

            private readonly List<string> _visitedMiddleware;

            public EventMessageHandler(List<string> visitedMiddleware) => _visitedMiddleware = visitedMiddleware;

            public class RequestEventArgs : EventArgs
            {
                public HttpRequestMessage Request { get; set; }
            }

            public class ResponseEventArgs : EventArgs
            {
                public HttpResponseMessage Response { get; set; }
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Request?.Invoke(this ,new RequestEventArgs { Request = request });
                var response = await base.SendAsync(request, cancellationToken);
                Response?.Invoke(this, new ResponseEventArgs { Response = response });
                _visitedMiddleware.Add(nameof(EventMessageHandler));
                return response;
            }
        }

        public class TrafficRecorderMessageHandler : DelegatingHandler
        {
            public List<(HttpRequestMessage, HttpResponseMessage)> Traffic { get; } = new List<(HttpRequestMessage, HttpResponseMessage)>();

            private readonly List<string> _visitedMiddleware;

            public TrafficRecorderMessageHandler(List<string> visitedMiddleware) => _visitedMiddleware = visitedMiddleware;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("foobar", "foobar");
                var response = await base.SendAsync(request, cancellationToken);
                _visitedMiddleware.Add(nameof(TrafficRecorderMessageHandler));
                Traffic.Add((request, response));

                return response;
            }
        }
    }
}
