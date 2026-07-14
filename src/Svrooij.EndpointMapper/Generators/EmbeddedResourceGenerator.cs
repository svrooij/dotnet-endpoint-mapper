using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Svrooij.EndpointMapper;

/// <summary>
/// Generates code from embedded resources in the Code folder.
/// Every file in the Code folder ending with .g.cs will be included in the generated output.
/// </summary>
[Generator]
public sealed class EmbeddedResourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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
                        sourceText = sourceText.Replace("\"1.0.0\"", $"\"{Generators.GeneratorConstants.GeneratorVersion}\"");
                        ctx.AddSource(resourceName, sourceText);
                    }
                }
            }
        });
    }
}
