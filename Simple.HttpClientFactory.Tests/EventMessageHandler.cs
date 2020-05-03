namespace Simple.HttpClientFactory.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="EventMessageHandler" />.
    /// </summary>
    public class EventMessageHandler : DelegatingHandler
    {
        /// <summary>
        /// Defines the Request.
        /// </summary>
        public event EventHandler<RequestEventArgs> Request;

        /// <summary>
        /// Defines the Response.
        /// </summary>
        public event EventHandler<ResponseEventArgs> Response;

        /// <summary>
        /// Defines the _visitedMiddleware.
        /// </summary>
        private readonly List<string> _visitedMiddleware;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventMessageHandler"/> class.
        /// </summary>
        /// <param name="visitedMiddleware">The visitedMiddleware<see cref="List{string}"/>.</param>
        public EventMessageHandler(List<string> visitedMiddleware) => _visitedMiddleware = visitedMiddleware;

        /// <summary>
        /// Defines the <see cref="RequestEventArgs" />.
        /// </summary>
        public class RequestEventArgs : EventArgs
        {
            /// <summary>
            /// Gets or sets the Request.
            /// </summary>
            public HttpRequestMessage Request { get; set; }
        }

        /// <summary>
        /// Defines the <see cref="ResponseEventArgs" />.
        /// </summary>
        public class ResponseEventArgs : EventArgs
        {
            /// <summary>
            /// Gets or sets the Response.
            /// </summary>
            public HttpResponseMessage Response { get; set; }
        }

        /// <summary>
        /// The SendAsync.
        /// </summary>
        /// <param name="request">The request<see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task{HttpResponseMessage}"/>.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request?.Invoke(this, new RequestEventArgs { Request = request });
            var response = await base.SendAsync(request, cancellationToken);
            Response?.Invoke(this, new ResponseEventArgs { Response = response });
            _visitedMiddleware.Add(nameof(EventMessageHandler));
            return response;
        }
    }
}
