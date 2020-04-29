using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;
using Xunit;

namespace Simple.HttpClientFactory.Tests
{
    public class BasicClientBuilderTests : IDisposable
    {
        private readonly WireMockServer _server;
        private readonly WireMockServer _secureServer;

        public BasicClientBuilderTests()
        {
             _server = WireMockServer.Start();
            _secureServer = WireMockServer.Start(ssl: true);

            _server.Given(Request.Create().WithPath("/hello/world").UsingAnyVerb())
                   .RespondWith(
                       Response.Create()
                          .WithStatusCode(200)
                          .WithHeader("Content-Type", "text/plain")
                          .WithBody("Hello world!")
                       );

            _secureServer.Given(Request.Create().WithPath("/hello/world").UsingAnyVerb())
                           .RespondWith(
                               Response.Create()
                                  .WithStatusCode(200)
                                  .WithHeader("Content-Type", "text/plain")
                                  .WithBody("Hello world!")
                               );
        }

        [Fact]
        public async Task Can_do_http_get_with_plain_client()
        {
            var client = new HttpClientBuilder().Build();
            var response = await client.GetAsync(_server.Urls[0] + "/hello/world");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Can_do_http_post_with_plain_client()
        {
            var client = new HttpClientBuilder().Build();
            var response = await client.PostAsync(_server.Urls[0] + "/hello/world", new StringContent("{}"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Can_do_https_get_with_plain_client()
        {
            var client = new HttpClientBuilder()
                                .WithCertificate(PublicCertificateHelper.GetX509Certificate2())
                                .Build();

            var response = await client.GetAsync(_secureServer.Urls[0] + "/hello/world");

            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        public void Dispose()
        {
            _server.Dispose();
            _secureServer.Dispose();
        }
    }
}
