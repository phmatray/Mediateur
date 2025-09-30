using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Mediateur;

/// <summary>
/// Analyzer that detects common issues with mediator pattern usage.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MediatorAnalyzer : DiagnosticAnalyzer
{
    public const string MultipleHandlersDiagnosticId = "MEDGEN001";
    public const string MissingHandlerDiagnosticId = "MEDGEN002";

    private static readonly DiagnosticDescriptor MultipleHandlersRule = new(
        id: MultipleHandlersDiagnosticId,
        title: "Multiple handlers found for the same request",
        messageFormat: "Multiple handlers found for request type '{0}'. Only one handler is allowed per request type.",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each request type should have exactly one handler. Multiple handlers will result in undefined behavior.");

    private static readonly DiagnosticDescriptor MissingHandlerRule = new(
        id: MissingHandlerDiagnosticId,
        title: "Request type has no handler",
        messageFormat: "Request type '{0}' has no corresponding handler. This request cannot be dispatched.",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each request type should have at least one handler to process it.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MultipleHandlersRule, MissingHandlerRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        var requestInterfaceSymbol = context.Compilation.GetTypeByMetadataName("Mediateur.IRequest`1");
        var voidRequestInterfaceSymbol = context.Compilation.GetTypeByMetadataName("Mediateur.IRequest");
        var requestHandlerInterfaceSymbol = context.Compilation.GetTypeByMetadataName("Mediateur.IRequestHandler`2");

        if (requestInterfaceSymbol == null ||
            voidRequestInterfaceSymbol == null ||
            requestHandlerInterfaceSymbol == null)
        {
            return; // Mediateur types not available
        }

        // Find all request types
        var requestTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var handlersPerRequest = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

        foreach (var syntaxTree in context.Compilation.SyntaxTrees)
        {
            var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot(context.CancellationToken);

            foreach (var typeDeclaration in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>())
            {
                var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) as INamedTypeSymbol;
                if (typeSymbol == null)
                    continue;

                // Check if it's a request type
                if (ImplementsInterface(typeSymbol, requestInterfaceSymbol) ||
                    ImplementsInterface(typeSymbol, voidRequestInterfaceSymbol))
                {
                    requestTypes.Add(typeSymbol);
                }

                // Check if it's a request handler
                var handlerInterface = typeSymbol.AllInterfaces
                    .FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, requestHandlerInterfaceSymbol));

                if (handlerInterface != null && handlerInterface.TypeArguments.Length == 2)
                {
                    var requestType = handlerInterface.TypeArguments[0] as INamedTypeSymbol;
                    if (requestType != null)
                    {
                        if (!handlersPerRequest.ContainsKey(requestType))
                        {
                            handlersPerRequest[requestType] = new List<INamedTypeSymbol>();
                        }
                        handlersPerRequest[requestType].Add(typeSymbol);
                    }
                }
            }
        }

        // Check for multiple handlers (MEDGEN001)
        foreach (var kvp in handlersPerRequest)
        {
            if (kvp.Value.Count > 1)
            {
                foreach (var handler in kvp.Value)
                {
                    var location = handler.Locations.FirstOrDefault();
                    if (location != null)
                    {
                        var diagnostic = Diagnostic.Create(
                            MultipleHandlersRule,
                            location,
                            kvp.Key.ToDisplayString());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        // Check for requests without handlers (MEDGEN002)
        foreach (var requestType in requestTypes)
        {
            if (!handlersPerRequest.ContainsKey(requestType))
            {
                var location = requestType.Locations.FirstOrDefault();
                if (location != null)
                {
                    var diagnostic = Diagnostic.Create(
                        MissingHandlerRule,
                        location,
                        requestType.ToDisplayString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool ImplementsInterface(INamedTypeSymbol typeSymbol, INamedTypeSymbol interfaceSymbol)
    {
        return typeSymbol.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, interfaceSymbol) ||
            SymbolEqualityComparer.Default.Equals(i, interfaceSymbol));
    }
}
