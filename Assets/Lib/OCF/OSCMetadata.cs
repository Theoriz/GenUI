using System;

public class OSCMetadata : Attribute
{
    public readonly string address;
    public OSCMetadata(string address)
    {
        this.address = address;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class OSCProperty : OSCMetadata
{
    public OSCProperty(string address) : base(address)
    {
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class OSCMethod : OSCMetadata
{
    public OSCMethod(string address) : base(address)
    {
    }
}