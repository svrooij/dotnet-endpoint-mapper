using System.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Svrooij.EndpointMapper;

[Generator]
public sealed class EndpointMapperGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // "Generate" code that is included in the Code folder.
    // Every file in there will be a .g.cs file in the project where this source generator is used.
    context.RegisterPostInitializationOutput(static ctx =>
    {
      var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
      foreach (var resourceName in resources)
      {
        if (resourceName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
        {
          using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
          if (stream is not null)
          {
            using var reader = new System.IO.StreamReader(stream);
            var sourceText = reader.ReadToEnd();
            ctx.AddSource(resourceName, sourceText);
          }
        }
      }
    });
    
    // var endpointMapperDeclarations = context.SyntaxProvider
    //   .CreateSyntaxProvider(
    //     predicate: static (s, _) => EndpointMapperSyntaxReceiver.IsSyntaxTargetForGeneration(s),
    //     transform: static (ctx, _) => EndpointMapperSyntaxReceiver.GetSemanticTargetForGeneration(ctx))
    //   .Where(static m => m is not null);

    // var compilationAndMappings = context.CompilationProvider.Combine(endpointMapperDeclarations.Collect());

    // context.RegisterSourceOutput(compilationAndMappings, static (spc, source) =>
    // {
    //   var (compilation, mappings) = source;
    //   EndpointMapperGeneratorImplementation.Execute(compilation, mappings!, spc);
    // });
  }
}