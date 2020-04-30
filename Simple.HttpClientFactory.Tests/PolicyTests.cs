using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Simple.HttpClientFactory.Tests
{
    public class PolicyTests
    {
		private WireMockServer _server;

		public PolicyTests()
        {
			_server = WireMockServer.Start();

			_server
				.Given(Request.Create()
					.WithPath("/hello/world")
					.UsingGet())
				.InScenario("Timeout")
				.WillSetStateTo("Transient issue resolved")
				.RespondWith(Response.Create()
					.WithStatusCode(408));

			_server
				.Given(Request.Create()
					.WithPath("/hello/world")
					.UsingGet())
				.InScenario("Timeout")
				.WhenStateIs("Transient issue resolved")
				.WillSetStateTo("All ok")
				.RespondWith(Response.Create()
					.WithStatusCode(200)
					.WithHeader("Content-Type", "text/plain")
                    .WithBody("Hello world!"));

        }

		[Fact]
		public async Task Client_without_retry_policy_should_fail_with_timeout()
		{
			var clientWithoutRetry = new HttpClientBuilder().Build();

			var responseWithTimeout = await clientWithoutRetry.GetAsync(_server.Urls[0] + "/hello/world");
            Assert.Equal(HttpStatusCode.RequestTimeout, responseWithTimeout.StatusCode);
		}

        [Fact]
        public async Task Retry_policy_should_work()
        {
			var clientWithRetry = new HttpClientBuilder()
				.WithPolicy(HttpPolicyExtensions
						.HandleTransientHttpError()
						.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
				.Build();

			var response = await clientWithRetry.GetAsync(_server.Urls[0] + "/hello/world");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }
    }
}
