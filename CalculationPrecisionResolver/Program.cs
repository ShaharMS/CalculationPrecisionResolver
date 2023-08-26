using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Text;

namespace CalculationPrecisionResolver
{
    [Generator]
    public class ComparisonConverterGenerator : ISourceGenerator
    {
        public double Epsilon = 0.0001;
        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // Retrieve the syntax tree of each compilation unit (source file)
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                // Use the syntax root to traverse and modify the syntax tree
                var root = syntaxTree.GetRoot();
                var newRoot = ConvertComparisons(root);

                // Replace the syntax root with the modified one
                Console.WriteLine(syntaxTree.FilePath);
                var sourceText = SourceText.From(newRoot.ToFullString(), Encoding.UTF8);
                context.AddSource(
                    syntaxTree.FilePath.Length != 0 ? syntaxTree.FilePath : "Local",
                    sourceText
                );
            }
        }

        private SyntaxNode ConvertComparisons(SyntaxNode node)
        {
            return node.ReplaceNodes(node.DescendantNodes(), (original, rewritten) =>
            {
                if (rewritten is BinaryExpressionSyntax binaryExpr &&
                    binaryExpr.Left is ExpressionSyntax typeSyntax &&
                    binaryExpr.Right is ExpressionSyntax literal)
                {
                    var epsilon = SyntaxFactory.ParseExpression($"{Epsilon}");
                    var leftOperand = binaryExpr.Left;
                    var rightOperand = binaryExpr.Right;
                    var operatorToken = binaryExpr.OperatorToken;

                    BinaryExpressionSyntax newComparison;
                    if (operatorToken.Text == "==")
                    {
                        var subtracted = SyntaxFactory.ParseExpression($"({leftOperand}) - ({literal})");
                        var mathAbs = SyntaxFactory.InvocationExpression(
                            SyntaxFactory.ParseExpression("Math.Abs"),
                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(subtracted)
                            ))
                        );
                        newComparison = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression, mathAbs, epsilon);
                    }
                    else if (operatorToken.Text == ">=")
                    {
                        if (leftOperand is BinaryExpressionSyntax leftBinary && leftBinary.OperatorToken.Text == "+")
                        {
                            leftOperand = SyntaxFactory.BinaryExpression(
                                SyntaxKind.AddExpression,
                                leftBinary.Left,
                                SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, leftBinary.Right, epsilon)
                            );
                        }
                        else
                        {
                            leftOperand = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, leftOperand, epsilon);
                        }
                        newComparison = SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanExpression, leftOperand, rightOperand);
                    }
                    else if (operatorToken.Text == "<=")
                    {
                        if (leftOperand is BinaryExpressionSyntax leftBinary && leftBinary.OperatorToken.Text == "+")
                        {
                            leftOperand = SyntaxFactory.BinaryExpression(
                                SyntaxKind.AddExpression,
                                leftBinary.Left,
                                SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, leftBinary.Right, epsilon)
                            );
                        }
                        else
                        {
                            leftOperand = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, leftOperand, epsilon);
                        }
                        newComparison = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression, leftOperand, rightOperand);
                    }
                    else
                    {
                        // Keep the original comparison for other cases
                        newComparison = binaryExpr;
                    }

                    return newComparison;
                }

                return rewritten;
            });
        }
    }
}