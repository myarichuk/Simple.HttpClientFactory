namespace Simple.HttpClientFactory.Tests.MessageHandlers
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="OfflineMessageHandler" />.
    /// </summary>
    internal class OfflineMessageHandler : DelegatingHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OfflineMessageHandler"/> class.
        /// </summary>
        public OfflineMessageHandler()
        {
        }

        /// <summary>
        /// The SendAsync.
        /// </summary>
        /// <param name="request">The request<see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task{HttpResponseMessage}"/>.</returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException($"No such host is known. ({request.RequestUri.Host}:{request.RequestUri.Port})");
        }
    }
}
