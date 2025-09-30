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

    private const string Category = "Design";
    private const string HelpLinkUri = "https://github.com/anthropics/mediateur/blob/main/docs/diagnostics/{0}.md";

    private static readonly DiagnosticDescriptor MultipleHandlersRule = new(
        id: MultipleHandlersDiagnosticId,
        title: "Multiple handlers found for the same request",
        messageFormat: "Multiple handlers found for request type '{0}'. Only one handler is allowed per request type.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each request type should have exactly one handler. Multiple handlers will result in undefined behavior.",
        helpLinkUri: string.Format(HelpLinkUri, MultipleHandlersDiagnosticId),
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    private static readonly DiagnosticDescriptor MissingHandlerRule = new(
        id: MissingHandlerDiagnosticId,
        title: "Request type has no handler",
        messageFormat: "Request type '{0}' has no corresponding handler. This request cannot be dispatched.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each request type should have at least one handler to process it.",
        helpLinkUri: string.Format(HelpLinkUri, MissingHandlerDiagnosticId),
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MultipleHandlersRule, MissingHandlerRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            var requestInterfaceSymbol = compilationStartContext.Compilation.GetTypeByMetadataName("Mediateur.IRequest`1");
            var voidRequestInterfaceSymbol = compilationStartContext.Compilation.GetTypeByMetadataName("Mediateur.IRequest");
            var requestHandlerInterfaceSymbol = compilationStartContext.Compilation.GetTypeByMetadataName("Mediateur.IRequestHandler`2");

            if (requestInterfaceSymbol == null ||
                voidRequestInterfaceSymbol == null ||
                requestHandlerInterfaceSymbol == null)
            {
                return; // Mediateur types not available
            }

            var requestTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var handlersPerRequest = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
            var lockObject = new object();

            // Register symbol action for named types
            compilationStartContext.RegisterSymbolAction(symbolContext =>
            {
                var namedTypeSymbol = (INamedTypeSymbol)symbolContext.Symbol;

                // Skip compiler-generated types
                if (namedTypeSymbol.IsImplicitlyDeclared)
                    return;

                lock (lockObject)
                {
                    // Check if it's a request type
                    if (ImplementsInterface(namedTypeSymbol, requestInterfaceSymbol) ||
                        ImplementsInterface(namedTypeSymbol, voidRequestInterfaceSymbol))
                    {
                        requestTypes.Add(namedTypeSymbol);
                    }

                    // Check if it's a request handler
                    var handlerInterface = namedTypeSymbol.AllInterfaces
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
                            handlersPerRequest[requestType].Add(namedTypeSymbol);
                        }
                    }
                }
            }, SymbolKind.NamedType);

            // Register compilation end action to report diagnostics
            compilationStartContext.RegisterCompilationEndAction(compilationEndContext =>
            {
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
                                compilationEndContext.ReportDiagnostic(diagnostic);
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
                            compilationEndContext.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            });
        });
    }

    private static bool ImplementsInterface(INamedTypeSymbol typeSymbol, INamedTypeSymbol interfaceSymbol)
    {
        return typeSymbol.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, interfaceSymbol) ||
            SymbolEqualityComparer.Default.Equals(i, interfaceSymbol));
    }
}
