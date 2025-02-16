namespace Common.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class DryRunSafeguardAttribute : Attribute
{
}