namespace Core.Web.Compression;

/// <summary>Opts an endpoint into a named HTTP response compression policy.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class HrkResponseCompressionAttribute(string policy = "Default") : Attribute
{
    public string Policy { get; } = policy;
    public bool Enabled { get; set; } = true;
}
