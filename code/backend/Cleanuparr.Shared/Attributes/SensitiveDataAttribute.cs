namespace Cleanuparr.Shared.Attributes;

/// <summary>
/// Marks a property as containing sensitive data that should be encrypted when stored in configuration files.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SensitiveDataAttribute : Attribute 
{
}
