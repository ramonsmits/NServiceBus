namespace NServiceBus.Core.Analyzer
{
    using System;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InterpolatedLoggingAnalyzer : DiagnosticAnalyzer
    {
        static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.DoNotUseStringInterpolationForLogMessage,
            "Interpolated string in logging",
            "Use numbered template logging instead of interpolation",
            "Logging",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        static readonly string[] LogMethods =
        {
            "Info", "Debug", "Warn", "Error", "Fatal"
        };

        void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
        {
            if (!(ctx.Node is InvocationExpressionSyntax invocation))
            {
                return;
            }

            if (!(invocation.Expression is MemberAccessExpressionSyntax expr))
            {
                return;
            }

            if (Array.IndexOf(LogMethods, expr.Name.Identifier.Text) < 0)
            {
                return;
            }

            var firstArg = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
            if (firstArg is InterpolatedStringExpressionSyntax)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(Rule, firstArg.GetLocation()));
            }
        }
    }
}