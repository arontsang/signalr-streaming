using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Client.Generator;

[Generator]
public class ProxyConnectionGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Add the marker attribute to the compilation
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
			"ProxyConnectionAttribute.g.cs", 
			SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));

		// Do a simple filter for enums
		IncrementalValuesProvider<PropertyDeclarationSyntax> enumDeclarations = context.SyntaxProvider
			.CreateSyntaxProvider(
				predicate: static (s, _) => IsSyntaxTargetForGeneration(s), // select enums with attributes
				transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // sect the enum with the [EnumExtensions] attribute
			.Where(static m => m is not null)!; // filter out attributed enums that we don't care about

		// Combine the selected enums with the `Compilation`
		IncrementalValueProvider<(Compilation, ImmutableArray<PropertyDeclarationSyntax>)> compilationAndEnums
			= context.CompilationProvider.Combine(enumDeclarations.Collect());

		// Generate the source using the compilation and enums
		context.RegisterSourceOutput(compilationAndEnums,
			static (spc, source) => Execute(source.Item1, source.Item2, spc));
	}
	
	static bool IsSyntaxTargetForGeneration(SyntaxNode node)
		=> node is PropertyDeclarationSyntax { AttributeLists.Count: > 0 };
	
	static PropertyDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
	{
		// we know the node is a EnumDeclarationSyntax thanks to IsSyntaxTargetForGeneration
		var propertyDeclarationSyntax = (PropertyDeclarationSyntax)context.Node;

		// loop through all the attributes on the method
		foreach (AttributeListSyntax attributeListSyntax in propertyDeclarationSyntax.AttributeLists)
		{
			foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
			{
				if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
				{
					// weird, we couldn't get the symbol, ignore it
					continue;
				}

				INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
				string fullName = attributeContainingTypeSymbol.ToDisplayString();

				// Is the attribute the [EnumExtensions] attribute?
				if (fullName == $"{SourceGenerationHelper.NameSpace}.{SourceGenerationHelper.AttributeName}")
				{
					// return the enum
					return propertyDeclarationSyntax;
				}
			}
		}

		// we didn't find the attribute we were looking for
		return null;
	}
	
	static void Execute(Compilation compilation, ImmutableArray<PropertyDeclarationSyntax> enums, SourceProductionContext context)
	{
		if (enums.IsDefaultOrEmpty)
		{
			// nothing to do yet
			return;
		}

		// I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
		IEnumerable<PropertyDeclarationSyntax> distinctEnums = enums.Distinct();

		// Convert each EnumDeclarationSyntax to an EnumToGenerate
		List<EnumToGenerate> enumsToGenerate = GetTypesToGenerate(compilation, distinctEnums, context.CancellationToken);

		// If there were errors in the EnumDeclarationSyntax, we won't create an
		// EnumToGenerate for it, so make sure we have something to generate
		if (enumsToGenerate.Count > 0)
		{
			// generate the source code and add it to the output
			string result = SourceGenerationHelper.GenerateExtensionClass(enumsToGenerate);
			context.AddSource("EnumExtensions.g.cs", SourceText.From(result, Encoding.UTF8));
		}
	}
}