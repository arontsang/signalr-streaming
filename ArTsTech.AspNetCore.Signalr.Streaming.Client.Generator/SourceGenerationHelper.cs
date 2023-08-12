namespace ArTsTech.AspNetCore.Signalr.Streaming.Client.Generator;

public static class SourceGenerationHelper
{
	public const string NameSpace = "ArTsTech.AspNetCore.Signalr.Streaming.Client";

	public const string AttributeName = "ProxyConnectionAttribute";
	
	public const string Attribute = @$"
namespace {NameSpace}
{{
    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
    public class {AttributeName} : System.Attribute
    {{
    }}
}}";
}