using MatrixStencil.ConsoleHost;

namespace MatrixStencil.ConsoleApp;

internal static class Program
{
    private const string Message = "HELLO WORLD"; // "Tony-Devs";

    private static int Main()
    {
        using var application = new MatrixConsoleApplication(Message);
        return application.Run();
    }
}
