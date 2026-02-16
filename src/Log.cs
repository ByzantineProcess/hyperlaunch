namespace Hyperlaunch;

/// <summary>
/// Thin logging facade so library code compiles with or without Godot.
/// When the Godot SDK is active it forwards to GD.Print / GD.PrintErr;
/// otherwise it writes to the console.
/// </summary>
public static class Log
{
    public static void Print(string message)
    {
#if GODOT
        Godot.GD.Print(message);
#else
        System.Console.WriteLine(message);
#endif
    }

    public static void PrintErr(string message)
    {
#if GODOT
        Godot.GD.PrintErr(message);
#else
        System.Console.Error.WriteLine(message);
#endif
    }
}
