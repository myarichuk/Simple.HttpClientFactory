using System.Net.Http;
using Polly;

namespace Simple.HttpClientFactory
{
#pragma warning disable 1591
    public static class HttpMessageExtensions
    {
        private const string PolicyExecutionContextKey = "PolicyExecutionContext";

        /// <summary>
        /// Sets a Polly policy context on an HTTP request message.
        /// <para>Do not re-use an instance of <see cref="Context"/> across more than one execution.</para>
        /// </summary>
        /// <param name="request">The HTTP request message to set the <see cref="Context"/> on.</param>
        /// <param name="ctx">The Polly policy context to set on the request message.</param>
        public static void SetPolicyExecutionContext(this HttpRequestMessage request, Context ctx)
        {
            if (request.Properties.ContainsKey(PolicyExecutionContextKey))
                request.Properties[PolicyExecutionContextKey] = ctx;
            else
                request.Properties.Add(PolicyExecutionContextKey, ctx);
        }

        /// <summary>
        /// Gets a Polly policy context from an HTTP request message, if previously set.
        /// <para>Do not re-use an instance of <see cref="Context"/> across more than one execution.</para>
        /// </summary>
        /// <param name="request">The HTTP request message to get the <see cref="Context"/> from.</param>
        /// <param name="ctx">If found, The Polly policy context that has been previously set on the provided HTTP request message.</param>
        public static bool TryGetPolicyExecutionContext(this HttpRequestMessage request, out Context ctx)
        {
            ctx = default;
            if (!request.Properties.TryGetValue(PolicyExecutionContextKey, out var genericContext))
                return false;

            ctx = genericContext as Context;
            return ctx != null;
        }
    }
}
