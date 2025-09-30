using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Mediateur.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mediateur;

/// <summary>
/// Incremental source generator for the Mediator pattern.
/// Discovers request/notification handlers and generates compile-time routing.
/// </summary>
[Generator]
public class MediatorGenerator : IIncrementalGenerator
{
    private const string MediateurNamespace = "Mediateur";
    private const string IRequestHandlerInterfaceName = "IRequestHandler";
    private const string INotificationHandlerInterfaceName = "INotificationHandler";
    private const string PipelineAttributeName = "PipelineAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover request handlers
        var requestHandlersProvider = context.SyntaxProvider
            .CreateSyntaxProvider<RequestHandlerInfo?>(
                predicate: static (s, _) => s is ClassDeclarationSyntax { BaseList: not null },
                transform: static (ctx, ct) => GetRequestHandlerInfo(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        // Discover notification handlers
        var notificationHandlersProvider = context.SyntaxProvider
            .CreateSyntaxProvider<NotificationHandlerInfo?>(
                predicate: static (s, _) => s is ClassDeclarationSyntax { BaseList: not null },
                transform: static (ctx, ct) => GetNotificationHandlerInfo(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        // Combine and generate code
        var combined = requestHandlersProvider
            .Collect()
            .Combine(notificationHandlersProvider.Collect());

        context.RegisterSourceOutput(combined, static (ctx, data) => GenerateCode(ctx, data));
    }

    private static RequestHandlerInfo? GetRequestHandlerInfo(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Get the semantic model and class symbol
        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
            return null;

        // Check if this class implements IRequestHandler<,> or IRequestHandler<>
        foreach (var @interface in classSymbol.AllInterfaces)
        {
            if (@interface.ContainingNamespace?.Name != MediateurNamespace)
                continue;

            var interfaceName = @interface.Name;

            // Check for IRequestHandler<TRequest, TResponse> or IRequestHandler<TRequest>
            if (interfaceName == IRequestHandlerInterfaceName && @interface.TypeArguments.Length > 0)
            {
                var requestType = @interface.TypeArguments[0] as INamedTypeSymbol;
                if (requestType == null)
                    continue;

                var responseType = @interface.TypeArguments.Length == 2
                    ? @interface.TypeArguments[1]
                    : context.SemanticModel.Compilation.GetTypeByMetadataName($"{MediateurNamespace}.Unit");

                if (responseType == null)
                    continue;

                var isVoidRequest = @interface.TypeArguments.Length == 1;

                var handlerInfo = new RequestHandlerInfo(classSymbol, requestType, responseType, isVoidRequest);

                // Extract pipeline attributes
                ExtractPipelineAttributes(classSymbol, handlerInfo);

                return handlerInfo;
            }
        }

        return null;
    }

    private static NotificationHandlerInfo? GetNotificationHandlerInfo(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Get the semantic model and class symbol
        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
            return null;

        // Check if this class implements INotificationHandler<>
        foreach (var @interface in classSymbol.AllInterfaces)
        {
            if (@interface.ContainingNamespace?.Name != MediateurNamespace)
                continue;

            if (@interface.Name == INotificationHandlerInterfaceName && @interface.TypeArguments.Length == 1)
            {
                var notificationType = @interface.TypeArguments[0] as INamedTypeSymbol;
                if (notificationType == null)
                    continue;

                return new NotificationHandlerInfo(classSymbol, notificationType);
            }
        }

        return null;
    }

    private static void ExtractPipelineAttributes(INamedTypeSymbol classSymbol, RequestHandlerInfo handlerInfo)
    {
        foreach (var attribute in classSymbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
                continue;

            // Check if this attribute inherits from PipelineAttribute
            var baseType = attributeClass.BaseType;
            while (baseType != null)
            {
                if (baseType.Name == PipelineAttributeName && baseType.ContainingNamespace?.Name == MediateurNamespace)
                {
                    // Extract Order property if specified
                    var order = 0;
                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Order" && namedArg.Value.Value is int orderValue)
                        {
                            order = orderValue;
                            break;
                        }
                    }

                    handlerInfo.Pipelines.Add(new PipelineInfo(attributeClass, order, attribute));
                    break;
                }

                baseType = baseType.BaseType;
            }
        }

        // Sort pipelines by order, then by declaration order (which is implicit in the list)
        handlerInfo.Pipelines.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    private static void GenerateCode(
        SourceProductionContext context,
        (ImmutableArray<RequestHandlerInfo> RequestHandlers, ImmutableArray<NotificationHandlerInfo> NotificationHandlers) data)
    {
        var requestHandlers = data.RequestHandlers;
        var notificationHandlers = data.NotificationHandlers;

        if (requestHandlers.IsEmpty && notificationHandlers.IsEmpty)
            return;

        // Generate Mediator implementation
        var mediatorCode = GenerateMediator(requestHandlers, notificationHandlers);
        context.AddSource("Mediator.g.cs", SourceText.From(mediatorCode, Encoding.UTF8));

        // Generate pipeline wrappers for each request handler with pipelines
        foreach (var handler in requestHandlers.Where(h => h.Pipelines.Count > 0))
        {
            var pipelineCode = GeneratePipelineWrappers(handler);
            context.AddSource($"Pipeline.{handler.SafeRequestTypeName}.g.cs", SourceText.From(pipelineCode, Encoding.UTF8));
        }

        // Generate DI registration
        var diCode = GenerateDependencyInjection(requestHandlers, notificationHandlers);
        context.AddSource("ServiceCollectionExtensions.g.cs", SourceText.From(diCode, Encoding.UTF8));
    }

    private static string GenerateMediator(
        ImmutableArray<RequestHandlerInfo> requestHandlers,
        ImmutableArray<NotificationHandlerInfo> notificationHandlers)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine("namespace Mediateur;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated mediator implementation with compile-time routing.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("internal sealed partial class Mediator : IMediator");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IServiceProvider _serviceProvider;");
        sb.AppendLine();
        sb.AppendLine("    public Mediator(IServiceProvider serviceProvider)");
        sb.AppendLine("    {");
        sb.AppendLine("        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate Send<TResponse> method
        sb.AppendLine("    public async ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        ArgumentNullException.ThrowIfNull(request);");
        sb.AppendLine();
        sb.AppendLine("        return request switch");
        sb.AppendLine("        {");

        foreach (var handler in requestHandlers.Where(h => !h.IsVoidRequest))
        {
            sb.AppendLine($"            {handler.RequestTypeName} typedRequest => (TResponse)(object)(await SendTyped{handler.SafeRequestTypeName}(typedRequest, cancellationToken)),");
        }

        sb.AppendLine("            _ => throw new InvalidOperationException($\"No handler registered for request type {request.GetType().FullName}\")");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate Send (void) method
        sb.AppendLine("    public async ValueTask Send(IRequest request, CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        ArgumentNullException.ThrowIfNull(request);");
        sb.AppendLine();
        sb.AppendLine("        switch (request)");
        sb.AppendLine("        {");

        foreach (var handler in requestHandlers.Where(h => h.IsVoidRequest))
        {
            sb.AppendLine($"            case {handler.RequestTypeName} typedRequest:");
            sb.AppendLine($"                await SendTyped{handler.SafeRequestTypeName}(typedRequest, cancellationToken);");
            sb.AppendLine("                return;");
        }

        sb.AppendLine("            default:");
        sb.AppendLine("                throw new InvalidOperationException($\"No handler registered for request type {request.GetType().FullName}\");");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate specific dispatch methods for each request type
        foreach (var handler in requestHandlers)
        {
            if (handler.IsVoidRequest)
            {
                sb.AppendLine($"    private async ValueTask SendTyped{handler.SafeRequestTypeName}({handler.RequestTypeName} request, CancellationToken cancellationToken)");
                sb.AppendLine("    {");

                if (handler.Pipelines.Count > 0)
                {
                    // Build pipeline chain
                    var innerHandler = $"({handler.HandlerTypeName})_serviceProvider.GetService(typeof({handler.HandlerTypeName}))!";
                    var currentHandler = innerHandler;

                    // Wrap from innermost to outermost (reverse order)
                    for (int i = handler.Pipelines.Count - 1; i >= 0; i--)
                    {
                        var pipeline = handler.Pipelines[i];
                        var wrapperTypeName = $"__Log_{handler.SafeRequestTypeName}";
                        if (pipeline.IsValidateAttribute)
                            wrapperTypeName = $"__Validate_{handler.SafeRequestTypeName}";
                        currentHandler = $"new {wrapperTypeName}({currentHandler}, _serviceProvider)";
                    }

                    sb.AppendLine($"        var handler = {currentHandler};");
                }
                else
                {
                    sb.AppendLine($"        var handler = ({handler.HandlerTypeName})_serviceProvider.GetService(typeof({handler.HandlerTypeName}))!;");
                }

                sb.AppendLine("        await handler.Handle(request, cancellationToken);");
                sb.AppendLine("    }");
            }
            else
            {
                sb.AppendLine($"    private ValueTask<{handler.ResponseTypeName}> SendTyped{handler.SafeRequestTypeName}({handler.RequestTypeName} request, CancellationToken cancellationToken)");
                sb.AppendLine("    {");

                if (handler.Pipelines.Count > 0)
                {
                    // Build pipeline chain
                    var innerHandler = $"({handler.HandlerTypeName})_serviceProvider.GetService(typeof({handler.HandlerTypeName}))!";
                    var currentHandler = innerHandler;

                    // Wrap from innermost to outermost (reverse order)
                    for (int i = handler.Pipelines.Count - 1; i >= 0; i--)
                    {
                        var pipeline = handler.Pipelines[i];
                        var wrapperTypeName = $"__Log_{handler.SafeRequestTypeName}";
                        if (pipeline.IsValidateAttribute)
                            wrapperTypeName = $"__Validate_{handler.SafeRequestTypeName}";
                        currentHandler = $"new {wrapperTypeName}({currentHandler}, _serviceProvider)";
                    }

                    sb.AppendLine($"        var handler = {currentHandler};");
                }
                else
                {
                    sb.AppendLine($"        var handler = ({handler.HandlerTypeName})_serviceProvider.GetService(typeof({handler.HandlerTypeName}))!;");
                }

                sb.AppendLine("        return handler.Handle(request, cancellationToken);");
                sb.AppendLine("    }");
            }
            sb.AppendLine();
        }

        // Generate Publish method for notifications
        sb.AppendLine("    public async ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)");
        sb.AppendLine("        where TNotification : INotification");
        sb.AppendLine("    {");
        sb.AppendLine("        ArgumentNullException.ThrowIfNull(notification);");
        sb.AppendLine();

        if (notificationHandlers.Length > 0)
        {
            sb.AppendLine("        switch (notification)");
            sb.AppendLine("        {");

            // Group handlers by notification type
            var handlersByNotification = notificationHandlers.GroupBy(h => h.NotificationTypeName);

            foreach (var group in handlersByNotification)
            {
                var notificationTypeName = group.Key;
                sb.AppendLine($"            case {notificationTypeName} typedNotification:");

                foreach (var handler in group)
                {
                    sb.AppendLine($"                await (({handler.HandlerTypeName})_serviceProvider.GetService(typeof({handler.HandlerTypeName}))!).Handle(typedNotification, cancellationToken);");
                }

                sb.AppendLine("                break;");
            }

            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GeneratePipelineWrappers(RequestHandlerInfo handler)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Diagnostics;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine("namespace Mediateur;");
        sb.AppendLine();

        foreach (var pipeline in handler.Pipelines)
        {
            var pipelineName = pipeline.IsLogAttribute ? "Log" : (pipeline.IsValidateAttribute ? "Validate" : "Unknown");
            var wrapperTypeName = $"__{pipelineName}_{handler.SafeRequestTypeName}";
            var handlerInterfaceName = handler.IsVoidRequest
                ? $"IRequestHandler<{handler.RequestTypeName}>"
                : $"IRequestHandler<{handler.RequestTypeName}, {handler.ResponseTypeName}>";

            sb.AppendLine($"file sealed class {wrapperTypeName} : {handlerInterfaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    private readonly {handlerInterfaceName} _next;");
            sb.AppendLine("    private readonly IServiceProvider _serviceProvider;");
            sb.AppendLine();
            sb.AppendLine($"    public {wrapperTypeName}({handlerInterfaceName} next, IServiceProvider serviceProvider)");
            sb.AppendLine("    {");
            sb.AppendLine("        _next = next ?? throw new ArgumentNullException(nameof(next));");
            sb.AppendLine("        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Generate Handle method based on pipeline type
            if (pipeline.IsLogAttribute)
            {
                GenerateLogPipelineHandle(sb, handler);
            }
            else if (pipeline.IsValidateAttribute)
            {
                GenerateValidatePipelineHandle(sb, handler);
            }
            else
            {
                // Default pass-through
                GeneratePassThroughHandle(sb, handler);
            }

            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void GenerateLogPipelineHandle(StringBuilder sb, RequestHandlerInfo handler)
    {
        var returnType = handler.IsVoidRequest ? "Unit" : handler.ResponseTypeName;

        sb.AppendLine($"    public async ValueTask<{returnType}> Handle({handler.RequestTypeName} request, CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        sb.AppendLine("        var sw = Stopwatch.StartNew();");
        sb.AppendLine($"        var requestType = \"{handler.RequestTypeName}\";");
        sb.AppendLine();
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            Console.WriteLine($\"[LOG] Executing {requestType}...\");");
        sb.AppendLine("            var result = await _next.Handle(request, cancellationToken);");
        sb.AppendLine("            Console.WriteLine($\"[LOG] Completed {requestType} in {sw.ElapsedMilliseconds}ms\");");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception ex)");
        sb.AppendLine("        {");
        sb.AppendLine("            Console.WriteLine($\"[LOG] Failed {requestType} in {sw.ElapsedMilliseconds}ms: {ex.Message}\");");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void GenerateValidatePipelineHandle(StringBuilder sb, RequestHandlerInfo handler)
    {
        var returnType = handler.IsVoidRequest ? "Unit" : handler.ResponseTypeName;

        sb.AppendLine($"    public ValueTask<{returnType}> Handle({handler.RequestTypeName} request, CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        sb.AppendLine("        // TODO: Add validation logic (DataAnnotations, FluentValidation, etc.)");
        sb.AppendLine("        // For now, just pass through");
        sb.AppendLine("        return _next.Handle(request, cancellationToken);");
        sb.AppendLine("    }");
    }

    private static void GeneratePassThroughHandle(StringBuilder sb, RequestHandlerInfo handler)
    {
        var returnType = handler.IsVoidRequest ? "Unit" : handler.ResponseTypeName;

        sb.AppendLine($"    public ValueTask<{returnType}> Handle({handler.RequestTypeName} request, CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        sb.AppendLine("        return _next.Handle(request, cancellationToken);");
        sb.AppendLine("    }");
    }

    private static string GenerateDependencyInjection(
        ImmutableArray<RequestHandlerInfo> requestHandlers,
        ImmutableArray<NotificationHandlerInfo> notificationHandlers)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace Mediateur;");
        sb.AppendLine();
        sb.AppendLine("public static class ServiceCollectionExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Adds the generated mediator and all discovered handlers to the service collection.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static IServiceCollection AddCompiledMediator(this IServiceCollection services)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Register mediator");
        sb.AppendLine("        services.AddSingleton<IMediator, Mediator>();");
        sb.AppendLine();
        sb.AppendLine("        // Register request handlers");

        foreach (var handler in requestHandlers)
        {
            sb.AppendLine($"        services.AddTransient(typeof({handler.HandlerTypeName}));");
        }

        if (notificationHandlers.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("        // Register notification handlers");

            foreach (var handler in notificationHandlers)
            {
                sb.AppendLine($"        services.AddTransient(typeof({handler.HandlerTypeName}));");
            }
        }

        sb.AppendLine();
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
