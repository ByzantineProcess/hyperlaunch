
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Hyperlaunch.Utilities;

// i know how bad this class probably looks to experienced c# devs
// i don't care
// i like it

// if c# wanted to teach a different pattern for binding names to values
// they could have reworked JsonPropertyName first

// seriously that entire mechanism feels even jankier than this somehow

public class StringEnum : Attribute
{
    public string Content { get; }

    public StringEnum(string content)
    {
        Content = content;
    }

    #nullable enable
    public static string? Retrieve(object any)
    {
        // this crime against programming brought to you by https://stackoverflow.com/a/4778347
        Type type = any.GetType();
        string? enumName = Enum.GetName(any.GetType(), any);
        if (enumName == null) { return null; }
        MemberInfo? element = type.GetField(enumName);
        if (element == null) { return null; }
        StringEnum? attr = (StringEnum?)GetCustomAttribute(element, typeof(StringEnum));
        if (attr == null) { return null; }
        return attr.Content;
    }

    // attempt to match a string to its bound stringenum
    public static object? Match(string content, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type encls)
    {
        string[] names = Enum.GetNames(encls);
        foreach (string name in names)
        {
            MemberInfo? element = encls.GetField(name);
            if (element == null) { continue; }
            StringEnum? attr = (StringEnum?)GetCustomAttribute(element, typeof(StringEnum));
            if (attr == null) { continue; }
            if (attr.Content == content) { return Enum.Parse(encls, name); }
        }
        return null;
    }
}