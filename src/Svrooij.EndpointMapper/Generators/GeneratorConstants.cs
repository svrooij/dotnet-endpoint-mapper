namespace Svrooij.EndpointMapper.Generators;

internal static class GeneratorConstants
{
    internal const string GeneratorName = "Svrooij.EndpointMapper";

    internal static readonly string GeneratorVersion =
        typeof(GeneratorConstants).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    internal static string ExcludeFromCodeCoverageAttributes(string indent = "") =>
        $"{indent}[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\n" +
        $"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{GeneratorVersion}\")]";
}
