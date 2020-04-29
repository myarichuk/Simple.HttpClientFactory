using Polly;
using System.Net.Http;

namespace Simple.HttpClientFactory.Polly
{
    public static class HttpMessageExtensions
    {
        private const string PolicyExecutionContextKey = "PolicyExecutionContext";

        public static void SetPolicyExecutionContext(this HttpRequestMessage request, Context ctx)
        {
            if(request.Properties.ContainsKey(PolicyExecutionContextKey))
                request.Properties[PolicyExecutionContextKey] = ctx;
            else
                request.Properties.Add(PolicyExecutionContextKey, ctx);
        }

        public static bool TryGetPolicyExecutionContext(this HttpRequestMessage request, out Context ctx)
        {
            ctx = default;
            if(!request.Properties.TryGetValue(PolicyExecutionContextKey, out var genericContext))
                return false;

            ctx = genericContext as Context;
            return ctx != null;
        }
    }
}
