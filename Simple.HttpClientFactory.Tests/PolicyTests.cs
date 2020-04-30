﻿using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
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
    public class PolicyTests
    {
		private readonly WireMockServer _server;

		public PolicyTests()
        {
			_server = WireMockServer.Start();

			_server
				.Given(Request.Create()
					.WithPath("/hello/world")
					.UsingGet())
				.InScenario("Timeout-then-resolved")
				.WillSetStateTo("Transient issue resolved")
				.RespondWith(Response.Create()
					.WithStatusCode(408));

			_server
				.Given(Request.Create()
					.WithPath("/hello/world")
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
					.WithPath("/timeout")
					.UsingGet())
				.RespondWith(Response.Create()
					.WithStatusCode(408));

        }

		[Fact]
		public async Task Client_with_retry_and_timeout_policy_should_properly_apply_policies()
		{
			//timeout after 2 secons, then retry
			var clientWithRetry = HttpClientFactory.Create()
				.WithPolicy(
						HttpPolicyExtensions
						.HandleTransientHttpError()
							.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
				.WithPolicy(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(4), TimeoutStrategy.Optimistic))
				.Build();

			var responseWithTimeout = await clientWithRetry.GetAsync(_server.Urls[0] + "/timeout");
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
						HttpPolicyExtensions
						.HandleTransientHttpError()
							.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1))))
				.Build();

			var response = await clientWithRetry.GetAsync(_server.Urls[0] + "/timeout");
			Assert.Equal(4, _server.LogEntries.Count());
            Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
		}


		[Fact]
		public async Task Client_without_retry_policy_should_fail_with_timeout()
		{
			var clientWithoutRetry = HttpClientFactory.Create().Build();

			var responseWithTimeout = await clientWithoutRetry.GetAsync(_server.Urls[0] + "/hello/world");

			Assert.Single(_server.LogEntries);
			Assert.True(_server.LogEntries.Count(entry => (int)entry.ResponseMessage.StatusCode == 408) == 1);

            Assert.Equal(HttpStatusCode.RequestTimeout, responseWithTimeout.StatusCode);
		}

        [Fact]
        public async Task Retry_policy_should_work()
        {
			var clientWithRetry = HttpClientFactory.Create()
				.WithPolicy(HttpPolicyExtensions
						.HandleTransientHttpError()
						.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
				.Build();

			var response = await clientWithRetry.GetAsync(_server.Urls[0] + "/hello/world");
            
			Assert.Equal(2, _server.LogEntries.Count());
			Assert.True(_server.LogEntries.Count(entry => (int)entry.ResponseMessage.StatusCode == 200) == 1);
			Assert.True(_server.LogEntries.Count(entry => (int)entry.ResponseMessage.StatusCode == 408) == 1);
            
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }
    }
}
