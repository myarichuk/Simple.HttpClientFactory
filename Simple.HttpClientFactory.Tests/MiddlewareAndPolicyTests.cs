using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
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
    public class MiddlewareAndPolicyTests
    {
		private readonly WireMockServer _server;
		private readonly List<string> _visitedMiddleware = new List<string>();

		public MiddlewareAndPolicyTests()
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
		public async Task Client_with_retry_and_timeout_policy_should_properly_apply_policies_with_single_middleware()
		{
            var eventMessageHandler = new EventMessageHandler(_visitedMiddleware);

            //timeout after 2 secons, then retry
			var clientWithRetry = HttpClientFactory.Create()
				.WithPolicy(
						HttpPolicyExtensions
						.HandleTransientHttpError()
							.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
				.WithPolicy(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(4), TimeoutStrategy.Optimistic))
                .WithHttpHandler(eventMessageHandler)
				.Build();

            Task<HttpResponseMessage> responseTask = null;

            await Assert.RaisesAsync<EventMessageHandler.RequestEventArgs>(
                 h => eventMessageHandler.Request += h,
                 h => eventMessageHandler.Request -= h,
                 () => responseTask = clientWithRetry.GetAsync(_server.Urls[0] + "/timeout"));

            var responseWithTimeout = await responseTask;

            Assert.Equal(4, _server.LogEntries.Count());
            Assert.Equal(HttpStatusCode.RequestTimeout, responseWithTimeout.StatusCode);
		}


		[Fact]
		public async Task Client_with_retry_and_timeout_policy_should_properly_apply_policies_with_multiple_middlewares()
		{
            var trafficRecorderMessageHandler = new TrafficRecorderMessageHandler(_visitedMiddleware);
            var eventMessageHandler = new EventMessageHandler(_visitedMiddleware);

            //timeout after 2 secons, then retry
			var clientWithRetry = HttpClientFactory.Create()
				.WithPolicy(
						HttpPolicyExtensions
						.HandleTransientHttpError()
							.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
				.WithPolicy(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(4), TimeoutStrategy.Optimistic))
                .WithHttpHandler(eventMessageHandler)
                .WithHttpHandler(trafficRecorderMessageHandler)
				.Build();

            Task<HttpResponseMessage> responseTask = null;

            var raisedEvent = await Assert.RaisesAsync<EventMessageHandler.RequestEventArgs>(
                 h => eventMessageHandler.Request += h,
                 h => eventMessageHandler.Request -= h,
                 () => responseTask = clientWithRetry.GetAsync(_server.Urls[0] + "/timeout"));

            var responseWithTimeout = await responseTask;

            Assert.True(raisedEvent.Arguments.Request.Headers.Contains("foobar"));
            Assert.Equal("foobar",raisedEvent.Arguments.Request.Headers.GetValues("foobar").FirstOrDefault());
            Assert.Single(trafficRecorderMessageHandler.Traffic);
            Assert.Equal(4, _server.LogEntries.Count());
            Assert.Equal(HttpStatusCode.RequestTimeout, responseWithTimeout.StatusCode);
		}


        [Fact]
        public async Task Retry_policy_should_work_with_multiple_middleware()
        {
            var trafficRecorderMessageHandler = new TrafficRecorderMessageHandler(_visitedMiddleware);
            var eventMessageHandler = new EventMessageHandler(_visitedMiddleware);

			var clientWithRetry = HttpClientFactory.Create()
				.WithPolicy(HttpPolicyExtensions
						.HandleTransientHttpError()
						.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
                .WithHttpHandler(eventMessageHandler)
                .WithHttpHandler(trafficRecorderMessageHandler)
				.Build();

			Task<HttpResponseMessage> responseTask = null;
            
           var raisedEvent = await Assert.RaisesAsync<EventMessageHandler.RequestEventArgs>(
                h => eventMessageHandler.Request += h,
                h => eventMessageHandler.Request -= h,
                () => responseTask = clientWithRetry.GetAsync(_server.Urls[0] + "/hello/world"));
			
			var response = await responseTask;

            Assert.True(raisedEvent.Arguments.Request.Headers.Contains("foobar"));
            Assert.Equal("foobar",raisedEvent.Arguments.Request.Headers.GetValues("foobar").FirstOrDefault());
            Assert.Single(trafficRecorderMessageHandler.Traffic);
			Assert.Equal(2, _server.LogEntries.Count());
			Assert.True(_server.LogEntries.Count(entry => (int)entry.ResponseMessage.StatusCode == 200) == 1);
			Assert.True(_server.LogEntries.Count(entry => (int)entry.ResponseMessage.StatusCode == 408) == 1);
            
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Retry_policy_should_work_with_single_middleware()
        {
            var eventMessageHandler = new EventMessageHandler(_visitedMiddleware);

			var clientWithRetry = HttpClientFactory.Create()
				.WithPolicy(HttpPolicyExtensions
						.HandleTransientHttpError()
						.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1)))
                .WithHttpHandler(eventMessageHandler)
				.Build();

			Task<HttpResponseMessage> responseTask = null;
            
           await Assert.RaisesAsync<EventMessageHandler.RequestEventArgs>(
                h => eventMessageHandler.Request += h,
                h => eventMessageHandler.Request -= h,
                () => responseTask = clientWithRetry.GetAsync(_server.Urls[0] + "/hello/world"));
			
			var response = await responseTask;

			Assert.Equal(2, _server.LogEntries.Count());
			Assert.True(_server.LogEntries.Count(entry => (int)entry.ResponseMessage.StatusCode == 200) == 1);
			Assert.True(_server.LogEntries.Count(entry => (int)entry.ResponseMessage.StatusCode == 408) == 1);
            
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello world!", await response.Content.ReadAsStringAsync());
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
