﻿using System.Threading.Tasks;
using NUnit.Framework;

namespace DryIoc.WebApi.UnitTests
{
    [TestFixture]
    public class RegisterHttpRequestMessageInRequestTests
    {
        public class MessageRequest {}

        public class A
        {
            public MessageRequest Request { get; set; }

            public A(MessageRequest r)
            {
                Request = r;
            }
        }

        [Test]
        public void ScopeTest()
        {
            // Create container with ExecutionFlowScopeContext which works across Async/Await boundaries.
            // In case of MVC it may be changed to HttpContextScopeContext.
            // If not specified container will use ThreadScopeContext.
            var container = new Container( 
                scopeContext: new ExecutionFlowScopeContext());

            container.Register<A>();

            // Register Null request in parent container in order to swap to actual request in current scope.
            // When resolving A container will find registered request dependency and cache access to it for fast performance.
            container.RegisterInstance(default(MessageRequest),
                WebReuse.InRequest, Setup.With(reuseWrappers: typeof(ReuseSwapable)));

            var request1 = Task.Run(async () =>
            {
                var request = new MessageRequest();
                using (var scope = container.OpenScope())
                {
                    // Resolve request as early registered ReuseSwapable.
                    // and swap its current value (null) with your request.
                    // It will replace request instance inside current scope, keep all resolution cache, etc intact. It is fast.
                    scope.Resolve<ReuseSwapable>(typeof(MessageRequest)).Swap(request);

                    await Task.Delay(5);//processing request
                    Assert.AreSame(request, scope.Resolve<A>().Request);
                }
            });

            var request2 = Task.Run(async () =>
            {
                var request = new MessageRequest();
                using (var scope = container.OpenScope())
                {
                    scope.Resolve<ReuseSwapable>(typeof(MessageRequest)).Swap(request);

                    await Task.Delay(2);//processing request
                    Assert.AreSame(request, scope.Resolve<A>().Request);
                }
            });

            Task.WaitAll(request1, request2);
        }
    }
}