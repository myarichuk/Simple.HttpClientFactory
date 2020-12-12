namespace Simple.HttpClientFactory.Tests.MessageHandlers
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="TrafficRecorderMessageHandler" />.
    /// </summary>
    internal class TrafficRecorderMessageHandler : DelegatingHandler
    {
        /// <summary>
        /// Gets the Traffic.
        /// </summary>
        public List<(HttpRequestMessage, HttpResponseMessage)> Traffic { get; } = new List<(HttpRequestMessage, HttpResponseMessage)>();

        /// <summary>
        /// Defines the _visitedMiddleware.
        /// </summary>
        private readonly List<string> _visitedMiddleware;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrafficRecorderMessageHandler"/> class.
        /// </summary>
        /// <param name="visitedMiddleware">The visitedMiddleware<see cref="List{string}"/>.</param>
        public TrafficRecorderMessageHandler(List<string> visitedMiddleware) => _visitedMiddleware = visitedMiddleware;

        /// <summary>
        /// The SendAsync.
        /// </summary>
        /// <param name="request">The request<see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task{HttpResponseMessage}"/>.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("foobar", "foobar");
            var response = await base.SendAsync(request, cancellationToken);
            response.Headers.Add("foobar", "foobar");
            _visitedMiddleware.Add(nameof(TrafficRecorderMessageHandler));
            Traffic.Add((request, response));

            return response;
        }
    }
}
