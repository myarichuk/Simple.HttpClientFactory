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
    public class BasicClientBuilderTests : IDisposable
    {
        private readonly WireMockServer _server;

        public BasicClientBuilderTests()
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
        public async Task Will_send_default_headers()
        {
            var trafficRecorder = new TrafficRecorderMessageHandler(new List<string>());

            var client = HttpClientFactory
                .Create(trafficRecorder)
                .WithDefaultHeaders(new Dictionary<string, string> { { "foobar", "xyz123" } })
                .Build();

            _ = await client.GetAsync(_server.Urls[0] + "/hello/world");

            Assert.Single(trafficRecorder.Traffic); //sanity check
            Assert.True(trafficRecorder.Traffic[0].Item1.Headers.Contains("foobar"));
            Assert.Equal("xyz123", trafficRecorder.Traffic[0].Item1.Headers.GetValues("foobar").FirstOrDefault());
        }


        [Fact]
        public async Task Can_do_http_get_with_plain_client()
        {
            var client = HttpClientFactory.Create().Build();
            var response = await client.GetAsync(_server.Urls[0] + "/hello/world");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Can_do_http_post_with_plain_client()
        {
            var client = HttpClientFactory.Create().Build();
            var response = await client.PostAsync(_server.Urls[0] + "/hello/world", new StringContent("{}"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        public void Dispose() => _server.Dispose();
    }
}
