using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.TestBase;

public class TestStateProjector : IStateProjector
{
    public async Task ProjectAsync<T>(T state) where T : StateWrapperBase
    {

    }
}