namespace Aevatar.Workshop.Tests;

public interface ISampleService
{
    string Test();
}

public class SampleService : ISampleService
{
    public string Test()
    {
        return "Hello";
    }
}