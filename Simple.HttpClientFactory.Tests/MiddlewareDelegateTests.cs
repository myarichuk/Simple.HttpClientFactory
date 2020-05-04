using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Simple.HttpClientFactory.Tests
{
    public partial class MiddlewareDelegateTests
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
                .WithMessageHandler(trafficRecorderMessageHandler)
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

        [Fact]
        public async Task Multiple_middleware_handlers_with_reverse_order_should_work()
        {
            var trafficRecorderMessageHandler = new TrafficRecorderMessageHandler(_visitedMiddleware);
            var eventMessageHandler = new EventMessageHandler(_visitedMiddleware);

            var client = HttpClientFactory.Create()
                .WithMessageHandler(trafficRecorderMessageHandler) //first execute this, then eventMessageHandler
                .WithMessageHandler(eventMessageHandler)
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
    }
}
