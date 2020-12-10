using Polly;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FakeItEasy;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Simple.HttpClientFactory.Tests
{
    public sealed class SecureClientBuilderTests : IDisposable
    {
        private const string _endpointUri = "/hello/world";

        private readonly WireMockServer _server;

        public SecureClientBuilderTests()
        {
            _server = WireMockServer.Start(ssl: true);

            _server.Given(Request.Create().WithPath(_endpointUri).UsingAnyMethod())
                   .RespondWith(
                       Response.Create()
                          .WithStatusCode(200)
                          .WithHeader("Content-Type", "text/plain")
                          .WithBody("Hello world!"));
        }

        private static HttpClient CreateClient() =>
            HttpClientFactory
                .Create()
                .WithCertificate(DefaultDevCert.Get())
                .WithPolicy(
    					Policy<HttpResponseMessage>
                            .Handle<HttpRequestException>()
                            .OrResult(result => (int)result.StatusCode >= 500 || result.StatusCode == HttpStatusCode.RequestTimeout)
                       .RetryAsync(3))
                .Build();

        [Fact]
        public void Providing_null_certificate_params_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithCertificate(null));
            Assert.Equal("certificates", exception.ParamName);
        }

        [Fact]
        public void Providing_a_null_certificate_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithCertificate(A.Fake<X509Certificate2>(), null));
            Assert.Equal("certificate", exception.ParamName);
        }

        [Fact]
        public void Providing_no_arguments_to_certificate_should_throw_argumentexception()
        {
            var exception = Assert.Throws<ArgumentException>(() => HttpClientFactory.Create().WithCertificate());
            Assert.Equal("certificates", exception.ParamName);
        }

        [Fact]
        public void Providing_a_null_certificate_collection_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithCertificates(null));
            Assert.Equal("certificates", exception.ParamName);
        }

        [Fact(Skip = "Requires local certificate setup")]
        public async Task Can_do_https_get_with_plain_client()
        {
            var client = CreateClient();
            var response = await client.GetAsync($"{_server.Urls[0]}{_endpointUri}");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact(Skip = "Requires local certificate setup")]
        public async Task Can_do_https_post_with_plain_client()
        {
            var client = CreateClient();
            var response = await client.PostAsync($"{_server.Urls[0]}{_endpointUri}", new StringContent("{}"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }


        public void Dispose() => _server.Dispose();
    }
}
