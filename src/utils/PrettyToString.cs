using System.Collections.Generic;
using System.Reflection;

namespace Hyperlaunch.Utilities;

public static class PrettyToString
{
    public static string Generic(object any)
    {
        FieldInfo[] fields = any.GetType().GetFields();
        string res = $"{any.GetType().Name} {{ \n";
        foreach (FieldInfo field in fields)
        {
            res += $"  {field.Name} = {field.GetValue(any)}";
        }
        res += "}\n\n\n";
        return res;
    }

    public static string List<T>(List<T> any)
    {
        string res = $"List [ \n";
        int count = 0;
        foreach (T generic in any)
        {
            res += $"  {count} = {generic}\n";
            count++;
        }
        res += "]\n";
        return res;
    }
}