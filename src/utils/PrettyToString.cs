using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Hyperlaunch.Utilities;

public static class PrettyToString
{
    public static string Generic(object any)
    {
        PropertyInfo[] fields = any.GetType().GetProperties();
        Log.Print($"[pts]: there are allegedly {fields.Length} accessible fields");
        string res = $"{any.GetType().Name} {{ \n";
        foreach (PropertyInfo field in fields)
        {
            object obj = field.GetValue(any);
            if (obj == null) { res += $"  {field.Name} = [null]\n"; continue; }
            if (IsGenericList(obj) && obj.GetType().GenericTypeArguments[0] == typeof(string))
            {
                res +=$"  {field.Name} = (string){FromList((List<string>)obj).Replace("  ", "    ")}";
                continue;
            }
            res += $"  {field.Name} = {obj}\n";
        }
        res += "}\n";
        return res;
    }
    public static bool IsGenericList(this object o)
    {
        Type oType = o.GetType();
        return oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof(List<>));
    }

    public static string FromList<T>(List<T> any, int? max = null)
    {
        string res = $"List [ \n";
        int count = 0;
        foreach (T generic in any)
        {
            res += $"  {count} = {generic}\n";
            count++;
            if (max != null) { if (count > max ) { break; } }
        }
        res += "]\n";
        return res;
    }

    public static string ByteArray(byte[] any)
    {
        return Convert.ToHexString(any);
    }
}