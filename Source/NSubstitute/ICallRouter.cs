using System;
using System.Collections.Generic;

namespace NSubstitute
{
    public interface ICallRouter
    {
        void LastCallShouldReturn<T>(T valueToReturn);
        object Route(ICall call);
        void AssertNextCallHasBeenReceived();
        void RaiseEventFromNextCall(Func<ICall, object[]> argumentsToRaiseEventWith);
        void AddCallbackForNextCall(Action<object[]> callbackWithArguments);
        void SetRoute<TRoute>(params object[] routeArguments) where TRoute : IRoute;
    }
}