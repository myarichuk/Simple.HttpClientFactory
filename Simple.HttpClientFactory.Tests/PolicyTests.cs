using Polly;
using Polly.Timeout;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FakeItEasy;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Simple.HttpClientFactory.Tests
{
    public class PolicyTests
    {
        private const string _endpointUri = "/hello/world";
        private const string _endpointUriTimeout = "/timeout";

		private readonly WireMockServer _server;

		public PolicyTests()
        {
			_server = WireMockServer.Start();

			_server
				.Given(Request.Create()
					.WithPath(_endpointUri)
					.UsingGet())
				.InScenario("Timeout-then-resolved")
				.WillSetStateTo("Transient issue resolved")
				.RespondWith(Response.Create()
					.WithStatusCode(408));

			_server
				.Given(Request.Create()
					.WithPath(_endpointUri)
					.UsingGet())
				.InScenario("Timeout-then-resolved")
				.WhenStateIs("Transient issue resolved")
				.WillSetStateTo("All ok")
				.RespondWith(Response.Create()
					.WithStatusCode(200)
					.WithHeader("Content-Type", "text/plain")
                    .WithBody("Hello world!"));

			_server
				.Given(Request.Create()
					.WithPath(_endpointUriTimeout)
					.UsingGet())
				.RespondWith(Response.Create()
					.WithStatusCode(408));
        }

        [Fact]
        public void Providing_null_policy_params_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithPolicy(null));
            Assert.Equal("policies", exception.ParamName);
        }

        [Fact]
        public void Providing_a_null_policy_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithPolicy(A.Fake<IAsyncPolicy<HttpResponseMessage>>(), null));
            Assert.Equal("policy", exception.ParamName);
        }

        [Fact]
        public void Providing_no_arguments_to_policy_should_throw_argumentexception()
        {
            var exception = Assert.Throws<ArgumentException>(() => HttpClientFactory.Create().WithPolicy());
            Assert.Equal("policies", exception.ParamName);
        }

		[Fact]
        public void Providing_a_null_policy_collection_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithPolicies(null));
            Assert.Equal("policies", exception.ParamName);
        }

		[Fact]
		public async Task Client_with_retry_and_timeout_policy_should_properly_apply_policies()
		{
			//timeout after 2 seconds, then retry
			var clientWithRetry = HttpClientFactory.Create()
				.WithPolicy(
    					Policy<HttpResponseMessage>
                            .Handle<HttpRequestException>()
                            .OrResult(result => (int)result.StatusCode >= 500 || result.StatusCode == HttpStatusCode.RequestTimeout)
							.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
				.WithPolicy(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(4), TimeoutStrategy.Optimistic))
				.Build();

			var responseWithTimeout = await clientWithRetry.GetAsync($"{_server.Urls[0]}{_endpointUriTimeout}");
			Assert.Equal(4, _server.LogEntries.Count());
            Assert.Equal(HttpStatusCode.RequestTimeout, responseWithTimeout.StatusCode);
		}

		[Fact]
		public async Task Client_with_retry_that_wraps_timeout_policy_should_properly_apply_policies()
		{
			var clientWithRetry = HttpClientFactory.Create()
				.WithPolicy(
				Policy.WrapAsync(
					Policy.TimeoutAsync<HttpResponseMessage>(25),
    					Policy<HttpResponseMessage>
                            .Handle<HttpRequestException>()
                            .OrResult(result => (int)result.StatusCode >= 500 || result.StatusCode == HttpStatusCode.RequestTimeout)
							.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1))))
				.Build();

			var response = await clientWithRetry.GetAsync($"{_server.Urls[0]}{_endpointUriTimeout}");
			Assert.Equal(4, _server.LogEntries.Count());
            Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
		}


		[Fact]
		public async Task Client_without_retry_policy_should_fail_with_timeout()
		{
			var clientWithoutRetry = HttpClientFactory.Create().Build();

			var responseWithTimeout = await clientWithoutRetry.GetAsync($"{_server.Urls[0]}{_endpointUri}");

			Assert.Single(_server.LogEntries);
			Assert.True(_server.LogEntries.Count(entry => (int)entry.ResponseMessage.StatusCode == 408) == 1);

            Assert.Equal(HttpStatusCode.RequestTimeout, responseWithTimeout.StatusCode);
		}

        [Fact]
        public async Task Retry_policy_should_work()
        {
			var clientWithRetry = HttpClientFactory.Create()
				.WithPolicy(
    					Policy<HttpResponseMessage>
                            .Handle<HttpRequestException>()
                            .OrResult(result => (int)result.StatusCode >= 500 || result.StatusCode == HttpStatusCode.RequestTimeout)
						.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
				.Build();

			var response = await clientWithRetry.GetAsync($"{_server.Urls[0]}{_endpointUri}");
            
			Assert.Equal(2, _server.LogEntries.Count());
			Assert.True(_server.LogEntries.Count(entry => (int)entry.ResponseMessage.StatusCode == 200) == 1);
			Assert.True(_server.LogEntries.Count(entry => (int)entry.ResponseMessage.StatusCode == 408) == 1);
            
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }
    }
}
