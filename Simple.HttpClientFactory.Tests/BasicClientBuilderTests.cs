#if NET472
using Simple.HttpClientFactory.MessageHandlers;
#endif
using Simple.HttpClientFactory.Tests.MessageHandlers;
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
    public sealed class BasicClientBuilderTests : IDisposable
    {
        private const string _endpointUri = "/hello/world";
        private const string _endpointUri2 = "/hello/world2";

        private readonly WireMockServer _server;

        public BasicClientBuilderTests()
        {
            _server = WireMockServer.Start();

            _server.Given(Request.Create().WithPath(_endpointUri).UsingAnyMethod())
                   .RespondWith(
                       Response.Create()
                          .WithStatusCode(HttpStatusCode.OK)
                          .WithHeader("Content-Type", "text/plain")
                          .WithBody("Hello world!"));

            _server.Given(Request.Create().WithPath(_endpointUri2).UsingAnyMethod())
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(HttpStatusCode.OK)
                        .WithHeader("Content-Type", "text/plain")
                        .WithBody("Hello world 2!"));
        }


        [Fact]
        public void Providing_a_null_string_base_url_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithBaseUrl(null as string));
            Assert.Equal("uriString", exception.ParamName);
        }

        [Fact]
        public void Providing_a_null_base_url_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithBaseUrl(null as Uri));
            Assert.Equal("baseUrl", exception.ParamName);
        }

        [Fact]
        public async Task Can_do_http_get_with_plain_client()
        {
            var client = HttpClientFactory.Create().Build();
            var response = await client.GetAsync($"{_server.Urls[0]}{_endpointUri}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Can_do_http_get_with_plain_client_with_string_base_url()
        {
            var client = HttpClientFactory.Create(_server.Urls[0]).Build();
            var response = await client.GetAsync(_endpointUri);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Can_do_http_get_with_plain_client_with_base_url()
        {
            var client = HttpClientFactory.Create(new Uri(_server.Urls[0])).Build();
            var response = await client.GetAsync(_endpointUri);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Can_do_http_get_with_plain_client_with_string_base_url_alternative_syntax()
        {
            var client = HttpClientFactory.Create().WithBaseUrl(_server.Urls[0]).Build();
            var response = await client.GetAsync(_endpointUri);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Can_do_http_get_with_plain_client_with_base_url_alternative_syntax()
        {
            var client = HttpClientFactory.Create().WithBaseUrl(new Uri(_server.Urls[0])).Build();
            var response = await client.GetAsync(_endpointUri);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public void Providing_a_null_default_header_name_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithDefaultHeader(null, "value"));
            Assert.Equal("name", exception.ParamName);
        }

        [Fact]
        public void Providing_a_null_default_header_value_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithDefaultHeader("name", null));
            Assert.Equal("value", exception.ParamName);
        }

        [Fact]
        public void Providing_a_null_default_headers_dictionary_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithDefaultHeaders(null));
            Assert.Equal("headers", exception.ParamName);
        }

        [Fact]
        public async Task Will_send_default_headers()
        {
            const string headerName = "foobar";
            const string headerValue = "xyz123";

            var trafficRecorder = new TrafficRecorderMessageHandler(new List<string>());

            var client = HttpClientFactory
                .Create()
                .WithDefaultHeaders(new Dictionary<string, string> { [headerName] = headerValue })
                .WithMessageHandler(trafficRecorder)
                .Build();

            _ = await client.GetAsync($"{_server.Urls[0]}{_endpointUri}");

            var traffic = Assert.Single(trafficRecorder.Traffic); //sanity check
            Assert.True(traffic.Item1.Headers.TryGetValues(headerName, out var headerValues));
            Assert.Equal(headerValue, headerValues.FirstOrDefault());
        }

#if NET472
        [Fact]
        public async Task HttpClientHandlerEx_should_cache_visited_url()
        {
            var baseUri = _server.Urls[0];

            var clientHandler = new HttpClientHandlerEx();
            var client = HttpClientFactory.Create().Build(clientHandler);

            _ = await client.GetAsync($"{baseUri}{_endpointUri}");

            var cachedUriKey = Assert.Single(clientHandler.AlreadySeenAddresses);
            Assert.Equal(new HttpClientHandlerEx.UriCacheKey($"{baseUri}{_endpointUri}"), cachedUriKey);
        }

        [Fact]
        public async Task UriCacheKey_equal_comparison()
        {
            var baseUri = _server.Urls[0];

            var clientHandler = new HttpClientHandlerEx();
            var client = HttpClientFactory.Create().Build(clientHandler);

            _ = await client.GetAsync($"{baseUri}{_endpointUri}");

            var cachedUriKey = Assert.Single(clientHandler.AlreadySeenAddresses);
            Assert.True(new HttpClientHandlerEx.UriCacheKey($"{baseUri}{_endpointUri}") == cachedUriKey);
        }

        [Fact]
        public async Task UriCacheKey_not_equal_comparison()
        {
            var baseUri = _server.Urls[0];

            var clientHandler = new HttpClientHandlerEx();
            var client = HttpClientFactory.Create().Build(clientHandler);

            _ = await client.GetAsync($"{baseUri}{_endpointUri}");
            _ = await client.GetAsync($"{baseUri}{_endpointUri2}");

            Assert.Equal(2, clientHandler.AlreadySeenAddresses.Count);

            var cachedUriKey = Assert.Single(clientHandler.AlreadySeenAddresses, uck => uck != new HttpClientHandlerEx.UriCacheKey($"{baseUri}{_endpointUri}"));
            Assert.Equal(new HttpClientHandlerEx.UriCacheKey($"{baseUri}{_endpointUri2}"), cachedUriKey);
        }

        [Fact]
        public void UriCacheKey_equal_comparison_object()
        {
            object uriCacheKeyObject = new HttpClientHandlerEx.UriCacheKey($"{_server.Urls[0]}{_endpointUri}");

            Assert.True(new HttpClientHandlerEx.UriCacheKey($"{_server.Urls[0]}{_endpointUri}").Equals(uriCacheKeyObject));
        }

        [Fact]
        public void UriCacheKey_not_equal_comparison_object()
        {
            object notUriCacheKeyObject = 0;

            Assert.False(new HttpClientHandlerEx.UriCacheKey($"{_server.Urls[0]}{_endpointUri2}").Equals(notUriCacheKeyObject));
        }

        [Fact]
        public void UriCacheKey_ToString()
        {
            Assert.Equal($"{_server.Urls[0]}{_endpointUri}", new HttpClientHandlerEx.UriCacheKey($"{_server.Urls[0]}{_endpointUri}").ToString());
        }

        [Fact]
        public void Two_UriCacheKeys_created_with_a_string_vs_a_uri_should_be_equal()
        {
            Assert.Equal(new HttpClientHandlerEx.UriCacheKey($"{_server.Urls[0]}{_endpointUri}"), new HttpClientHandlerEx.UriCacheKey(new Uri($"{_server.Urls[0]}{_endpointUri}")));
        }
#endif

        [Fact]
        public async Task Can_do_http_post_with_plain_client()
        {
            var client = HttpClientFactory.Create(_server.Urls[0]).Build();
            var response = await client.PostAsync(_endpointUri, new StringContent("{}"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public void Should_set_timeout_on_the_client()
        {
            var timeout = TimeSpan.FromSeconds(999);

            var client = HttpClientFactory.Create().WithTimeout(timeout).Build();

            Assert.Equal(timeout, client.Timeout);
        }


        public void Dispose() => _server.Dispose();
    }
}
