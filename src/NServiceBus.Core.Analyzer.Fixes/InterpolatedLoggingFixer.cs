namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InterpolatedLoggingFixer))]
    public class InterpolatedLoggingFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.DoNotUseStringInterpolationForLogMessage);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            var node = root.FindNode(context.Span).FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (node == null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Convert to template logging",
                    ct => FixInterpolatedLogging(context.Document, node, ct),
                    nameof(InterpolatedLoggingFixer)),
                context.Diagnostics);
        }

        static async Task<Document> FixInterpolatedLogging(Document doc, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var root = await doc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return doc;
            }

            if (!(invocation.ArgumentList.Arguments.First().Expression is InterpolatedStringExpressionSyntax interpolated))
            {
                return doc;
            }

            var args = new List<ExpressionSyntax>();
            int idx = 0;
            var formatBuilder = new StringBuilder();

            foreach (var part in interpolated.Contents)
            {
                switch (part)
                {
                    case InterpolatedStringTextSyntax text:
                        formatBuilder.Append(text.TextToken.Text);
                        break;
                    case InterpolationSyntax interp:
                        formatBuilder.Append($"{{{idx++}}}");
                        args.Add(interp.Expression);
                        break;
                    default:
                        break;
                }
            }

            var formatStr = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(formatBuilder.ToString()));

            var newArgs = new[]
                {
                    SyntaxFactory.Argument(formatStr)
                }
                .Concat(args.Select(SyntaxFactory.Argument));

            var expr = (MemberAccessExpressionSyntax)invocation.Expression;
            var methodName = expr.Name.Identifier.Text;
            var updatedExpr = expr.WithName(SyntaxFactory.IdentifierName(methodName + "Format"));

            var newInvocation = invocation
                .WithExpression(updatedExpr)
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(newArgs)));

            var newRoot = root.ReplaceNode(invocation, newInvocation);
            return doc.WithSyntaxRoot(newRoot);
        }
    }
}