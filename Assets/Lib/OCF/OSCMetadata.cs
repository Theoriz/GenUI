using System;

public class OSCMetadata : Attribute
{
}

[AttributeUsage(AttributeTargets.Field)]
public class OSCProperty : OSCMetadata
{
}

[AttributeUsage(AttributeTargets.Method)]
public class OSCMethod : OSCMetadata
{
}