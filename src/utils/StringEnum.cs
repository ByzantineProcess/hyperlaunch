
using System;
using System.Reflection;

namespace Hyperlaunch.Utilities;

public class StringEnum : Attribute
{
    public string Content { get; }

    public StringEnum(string content)
    {
        Content = content;
    }

    public static string Retrieve(object any)
    {
        // this crime against programming brought to you by https://stackoverflow.com/a/4778347
        MemberInfo element = any.GetType().GetField(Enum.GetName(any.GetType(), any));
        StringEnum attr = (StringEnum)GetCustomAttribute(element, typeof(StringEnum));
        return attr.Content;
    }
}