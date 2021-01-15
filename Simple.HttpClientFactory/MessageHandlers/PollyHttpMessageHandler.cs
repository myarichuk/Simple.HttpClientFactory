using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Simple.HttpClientFactory.MessageHandlers
{
   internal sealed class PollyMessageMiddleware : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        /// <summary>
        /// Creates a new <see cref="PollyMessageMiddleware"/>.
        /// </summary>
        /// <param name="policy">The policy.</param>
        /// <param name="innerHandler">The inner handler which is responsible for processing the HTTP response messages.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="policy"/> is <see langword="null"/></exception>
        internal PollyMessageMiddleware(IAsyncPolicy<HttpResponseMessage> policy, HttpMessageHandler innerHandler) : base(innerHandler) => _policy = policy;

        /// <inheritdoc />
        /// <exception cref="T:System.ArgumentNullException"><paramref name="request"/> is <see langword="null"/></exception>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendAsyncInternal(request, cancellationToken);
        }

        private Task<HttpResponseMessage> SendAsyncInternal(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Guarantee the existence of a context for every policy execution, but only create a new one if needed. This
            // allows later handlers to flow state if desired.
            var cleanUpContext = false;
            var context = GetOrCreatePolicyExecutionContext(request, ref cleanUpContext);

            //do not await for the task so the async state machine won't grow big
            var responseTask = 
                _policy.ExecuteAsync(
                    async (c, ct) => 
                            await base.SendAsync(request, cancellationToken), context, cancellationToken)
                       .ContinueWith(t =>
                       {
                           if (cleanUpContext)
                               request.SetPolicyExecutionContext(null);
                           return t.Result;
                       }, cancellationToken);

            responseTask.ConfigureAwait(false);

            return responseTask;

            Context GetOrCreatePolicyExecutionContext(HttpRequestMessage httpRequestMessage, ref bool shouldCleanupContext)
            {
                if (!httpRequestMessage.TryGetPolicyExecutionContext(out var fetchedContext))
                {
                    fetchedContext = new Context();
                    httpRequestMessage.SetPolicyExecutionContext(fetchedContext);
                    shouldCleanupContext = true;
                }

                return fetchedContext;
            }
        }
    }
}
