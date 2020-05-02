using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Simple.HttpClientFactory.Tests
{
    public class SecureClientBuilderTests : IDisposable
    {

        private readonly WireMockServer _server;

        public SecureClientBuilderTests()
        {
            _server = WireMockServer.Start(ssl: true);

            _server.Given(Request.Create().WithPath("/hello/world").UsingAnyMethod())
                   .RespondWith(
                       Response.Create()
                          .WithStatusCode(200)
                          .WithHeader("Content-Type", "text/plain")
                          .WithBody("Hello world!"));         
        }

        private HttpClient CreateClient() =>
            HttpClientFactory
                .Create()
                .WithCertificate(DefaultDevCert.Get())
                .WithPolicy(
    					Policy<HttpResponseMessage>
                            .Handle<HttpRequestException>()
                            .OrResult(result => (int)result.StatusCode >= 500 || result.StatusCode == HttpStatusCode.RequestTimeout)
                       .RetryAsync(3))
                .Build();

        [Fact(Skip = "Requires local certificate setup")]
        public async Task Can_do_https_get_with_plain_client()
        {
            var client = CreateClient();
            var response = await client.GetAsync(_server.Urls[0] + "/hello/world");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact(Skip = "Requires local certificate setup")]
        public async Task Can_do_https_post_with_plain_client()
        {
            var client = CreateClient();
            var response = await client.PostAsync(_server.Urls[0] + "/hello/world", new StringContent("{}"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        public void Dispose() => _server.Dispose();


    }
}
