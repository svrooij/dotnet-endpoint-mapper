using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Svrooij.EndpointMapper.Tests;

public class EndpointMapperAnalyzerTests
{
    [Test]
    public async Task Analyzer_should_be_constructed_without_parameters()
    {
        var analyzer = new EndpointMapperAnalyzer();
        await Assert.That(analyzer).IsNotNull();
    }
}