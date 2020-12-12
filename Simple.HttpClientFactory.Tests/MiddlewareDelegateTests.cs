using Simple.HttpClientFactory.Tests.MessageHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using FakeItEasy;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Simple.HttpClientFactory.Tests
{
    public class MiddlewareDelegateTests
    {
        private const string _endpointUri = "/hello/world";

        private readonly WireMockServer _server;

        public MiddlewareDelegateTests()
        {
            _server = WireMockServer.Start();
            _server.Given(Request.Create().WithPath(_endpointUri).UsingAnyMethod())
                   .RespondWith(
                       Response.Create()
                          .WithStatusCode(200)
                          .WithHeader("Content-Type", "text/plain")
                          .WithBody("Hello world!"));
        }

        [Fact]
        public void Providing_null_message_handler_params_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithMessageHandler(null));
            Assert.Equal("handlers", exception.ParamName);
        }

        [Fact]
        public void Providing_a_null_message_handler_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithMessageHandler(A.Dummy<DelegatingHandler>(), null));
            Assert.Equal("handler", exception.ParamName);
        }

        [Fact]
        public void Providing_no_arguments_to_message_handler_should_throw_argumentexception()
        {
            var exception = Assert.Throws<ArgumentException>(() => HttpClientFactory.Create().WithMessageHandler());
            Assert.Equal("handlers", exception.ParamName);
        }

        [Fact]
        public void Providing_a_null_message_handler_collection_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithMessageHandlers(null));
            Assert.Equal("handlers", exception.ParamName);
        }

        [Fact]
        public void Providing_a_null_primary_message_handler_configurator_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().WithPrimaryMessageHandlerConfigurator(null));
            Assert.Equal("configurator", exception.ParamName);
        }

#if NET472
        [Fact]
        public void Providing_a_null_substitute_primary_message_handler_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().Build(null as HttpClientHandler));
            Assert.Equal("defaultPrimaryMessageHandler", exception.ParamName);
        }

        [Fact]
        public void Should_use_provided_substitute_primary_message_handler_when_no_additional_message_handlers_provided()
        {
            var substitutePrimaryMessageHandler = new HttpClientHandler();
            var client = HttpClientFactory.Create().Build(substitutePrimaryMessageHandler);

            var handlerField = typeof(HttpMessageInvoker).GetField("handler", BindingFlags.Instance | BindingFlags.NonPublic);
            var value = handlerField.GetValue(client);

            Assert.Same(substitutePrimaryMessageHandler, value);
        }

        [Fact]
        public void Primary_message_handler_configurator_for_build_should_be_invoked_after_factory_configurator()
        {
            string configuratorName = null;

            var configuratorFactory = A.Fake<Action<HttpClientHandler>>();
            var configuratorBuild   = A.Fake<Action<HttpClientHandler>>();

            A.CallTo(() => configuratorFactory.Invoke(A<HttpClientHandler>._)).Invokes(() => configuratorName = nameof(configuratorFactory));
            A.CallTo(() => configuratorBuild.Invoke(A<HttpClientHandler>._)).Invokes(() => configuratorName = nameof(configuratorBuild));

            HttpClientFactory.Create().WithPrimaryMessageHandlerConfigurator(configuratorFactory).Build(configuratorBuild);

                  A.CallTo(() => configuratorFactory.Invoke(A<HttpClientHandler>.That.IsNotNull())).MustHaveHappened()
            .Then(A.CallTo(() => configuratorBuild  .Invoke(A<HttpClientHandler>.That.IsNotNull())).MustHaveHappened());

            Assert.Equal(nameof(configuratorBuild), configuratorName);
        }
#else

        [Fact]
        public void Primary_message_handler_configurator_for_build_should_be_invoked_after_factory_configurator()
        {
            string configuratorName = null;

            var configuratorFactory = A.Fake<Action<SocketsHttpHandler>>();
            var configuratorBuild = A.Fake<Action<SocketsHttpHandler>>();

            A.CallTo(() => configuratorFactory.Invoke(A<SocketsHttpHandler>._)).Invokes(() => configuratorName = nameof(configuratorFactory));
            A.CallTo(() => configuratorBuild.Invoke(A<SocketsHttpHandler>._)).Invokes(() => configuratorName = nameof(configuratorBuild));

            HttpClientFactory.Create().WithPrimaryMessageHandlerConfigurator(configuratorFactory).Build(configuratorBuild);

                  A.CallTo(() => configuratorFactory.Invoke(A<SocketsHttpHandler>.That.IsNotNull())).MustHaveHappened()
            .Then(A.CallTo(() => configuratorBuild  .Invoke(A<SocketsHttpHandler>.That.IsNotNull())).MustHaveHappened());

            Assert.Equal(nameof(configuratorBuild), configuratorName);
        }

        [Fact]
        public void Providing_a_null_substitute_primary_message_handler_should_throw_argumentnullexception()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => HttpClientFactory.Create().Build(null as SocketsHttpHandler));
            Assert.Equal("defaultPrimaryMessageHandler", exception.ParamName);
        }

        [Fact]
        public void Should_use_provided_substitute_primary_message_handler_when_no_additional_message_handlers_provided()
        {
            var substitutePrimaryMessageHandler = new SocketsHttpHandler();
            var client = HttpClientFactory.Create().Build(substitutePrimaryMessageHandler);

            var handlerField = typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic);
            var value = handlerField.GetValue(client);

            Assert.Same(substitutePrimaryMessageHandler, value);
        }
#endif

        [Fact]
        public async Task Single_middleware_handler_should_work()
        {
            await SingleMiddlewareHandler($"{_server.Urls[0]}{_endpointUri}", tr => HttpClientFactory.Create().WithMessageHandler(tr));
        }

        [Fact]
        public async Task Single_middleware_handler_should_work_from_create_with_string_base_url()
        {
            await SingleMiddlewareHandler(_endpointUri, tr => HttpClientFactory.Create(_server.Urls[0], tr));
        }

        [Fact]
        public async Task Single_middleware_handler_should_work_from_create_with_base_url()
        {
            await SingleMiddlewareHandler(_endpointUri, tr => HttpClientFactory.Create(new Uri(_server.Urls[0]), tr));
        }

        [Fact]
        public async Task Single_middleware_handler_should_work_from_create()
        {
            await SingleMiddlewareHandler($"{_server.Urls[0]}{_endpointUri}", tr => HttpClientFactory.Create(tr));
        }

        private static async Task SingleMiddlewareHandler(string endpoint, Func<TrafficRecorderMessageHandler, IHttpClientBuilder> factory)
        {
            var trafficRecorderMessageHandler = new TrafficRecorderMessageHandler(new List<string>());

            var client = factory(trafficRecorderMessageHandler).Build();

            var _ = await client.GetAsync(endpoint);

            Assert.Single(trafficRecorderMessageHandler.Traffic);
            Assert.Equal(HttpStatusCode.OK, trafficRecorderMessageHandler.Traffic[0].Item2.StatusCode);
        }

        [Fact]
        public async Task Multiple_middleware_handlers_should_work()
        {
            await MultipleMiddlewareHandlers($"{_server.Urls[0]}{_endpointUri}", (tr, e) => HttpClientFactory.Create().WithMessageHandler(e).WithMessageHandler(tr), new[] { nameof(TrafficRecorderMessageHandler), nameof(EventMessageHandler) });
        }

        [Fact]
        public async Task Multiple_middleware_handlers_should_work_from_create()
        {
            await MultipleMiddlewareHandlers($"{_server.Urls[0]}{_endpointUri}", (tr, e) => HttpClientFactory.Create(e, tr), new[] { nameof(TrafficRecorderMessageHandler), nameof(EventMessageHandler) });
        }

        [Fact]
        public async Task Multiple_middleware_handlers_should_work_from_create_with_string_base_url()
        {
            await MultipleMiddlewareHandlers(_endpointUri, (tr, e) => HttpClientFactory.Create(_server.Urls[0], tr, e), new[] { nameof(EventMessageHandler), nameof(TrafficRecorderMessageHandler) });
        }

        [Fact]
        public async Task Multiple_middleware_handlers_should_work_from_create_with_base_url()
        {
            await MultipleMiddlewareHandlers(_endpointUri, (tr, e) => HttpClientFactory.Create(new Uri(_server.Urls[0]), tr, e), new[] { nameof(EventMessageHandler), nameof(TrafficRecorderMessageHandler) });
        }

        [Fact]
        public async Task Multiple_middleware_handlers_with_reverse_order_should_work()
        {
            await MultipleMiddlewareHandlers($"{_server.Urls[0]}{_endpointUri}", (tr, e) => HttpClientFactory.Create().WithMessageHandler(tr).WithMessageHandler(e), new[] { nameof(EventMessageHandler), nameof(TrafficRecorderMessageHandler) });
        }

        [Fact]
        public async Task Multiple_middleware_handlers_with_reverse_order_should_work_from_create()
        {
            await MultipleMiddlewareHandlers($"{_server.Urls[0]}{_endpointUri}", (tr, e) => HttpClientFactory.Create(tr, e), new[] { nameof(EventMessageHandler), nameof(TrafficRecorderMessageHandler) });
        }

        private static async Task MultipleMiddlewareHandlers(string endpoint, Func<TrafficRecorderMessageHandler, EventMessageHandler, IHttpClientBuilder> factory, IEnumerable<string> expectedVisitedMiddleware)
        {
            var actuallyVisitedMiddleware = new List<string>();

            var trafficRecorderMessageHandler = new TrafficRecorderMessageHandler(actuallyVisitedMiddleware);
            var eventMessageHandler = new EventMessageHandler(actuallyVisitedMiddleware);

            var client = factory(trafficRecorderMessageHandler, eventMessageHandler).Build();

            var raisedEvent = await Assert.RaisesAsync<EventMessageHandler.RequestEventArgs>(
                h => eventMessageHandler.Request += h,
                h => eventMessageHandler.Request -= h,
                () => client.GetAsync(endpoint));

            Assert.True(raisedEvent.Arguments.Request.Headers.Contains("foobar"));
            Assert.Equal("foobar", raisedEvent.Arguments.Request.Headers.GetValues("foobar").FirstOrDefault());
            Assert.Single(trafficRecorderMessageHandler.Traffic);

            Assert.Equal(HttpStatusCode.OK, trafficRecorderMessageHandler.Traffic[0].Item2.StatusCode);
            Assert.Equal(expectedVisitedMiddleware, actuallyVisitedMiddleware);
        }
    }
}
