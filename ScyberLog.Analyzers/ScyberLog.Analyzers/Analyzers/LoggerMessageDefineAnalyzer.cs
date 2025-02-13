// Contains code from https://github.com/dotnet/roslyn-analyzers/blob/main/src/NetAnalyzers/Core/Microsoft.NetCore.Analyzers/Runtime/LoggerMessageDefineAnalyzer.cs
// Original and new code licensed under the MIT license.

using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace ScyberLog.Analyzers
{
    //https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/code-quality-rule-options#category-of-rules
    internal static class RuleCategory
    {
        public const string Design = "Design";
        public const string Documentation = "Documentation";
        public const string Globalization = "Globalization";
        public const string Interoperability = "Interoperability";
        public const string Maintainability = "Maintainability";
        public const string Naming = "Naming";
        public const string Performance = "Performance";
        public const string SingleFile = "SingleFile";
        public const string Reliability = "Reliability";
        public const string Security = "Security";
        public const string Usage = "Usage";
    }

    public static class LoggingTypeNames
    {
        public const string MicrosoftExtensionsLoggingILogger = "Microsoft.Extensions.Logging.ILogger";
        public const string MicrosoftExtensionsLoggingLoggerExtensions = "Microsoft.Extensions.Logging.LoggerExtensions";
        public const string MicrosoftExtensionsLoggingLoggerMessage = "Microsoft.Extensions.Logging.LoggerMessage";
        public const string MicrosoftExtensionsLoggingLoggerMessageAttribute = "Microsoft.Extensions.Logging.LoggerMessageAttribute";
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class LoggerMessageDefineAnalyzer : DiagnosticAnalyzer
    {
        private static LocalizableString GetLocalizableString(string key) => new LocalizableResourceString(key, Resources.ResourceManager, typeof(Resources));

        private static readonly DiagnosticDescriptor InvalidTemplateStringRule = new DiagnosticDescriptor(
            id: "AA0008",
            title: GetLocalizableString(nameof(Resources.AnalyzerTitle)),
            messageFormat: GetLocalizableString(nameof(Resources.AnalyzerMessageFormat)),
            RuleCategory.Usage,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetLocalizableString(nameof(Resources.AnalyzerDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return [InvalidTemplateStringRule]; } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(ctx =>
            {
                var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(ctx.Compilation);

                //handle logging method invocations
                if (wellKnownTypeProvider.TryGetOrCreateTypeByMetadataName(LoggingTypeNames.MicrosoftExtensionsLoggingLoggerExtensions, out var loggerExtensionsType) &&
                    wellKnownTypeProvider.TryGetOrCreateTypeByMetadataName(LoggingTypeNames.MicrosoftExtensionsLoggingILogger, out var loggerType) &&
                    wellKnownTypeProvider.TryGetOrCreateTypeByMetadataName(LoggingTypeNames.MicrosoftExtensionsLoggingLoggerMessage, out var loggerMessageType))
                {
                    ctx.RegisterOperationAction(x => AnalyzeInvocation(x, loggerType, loggerExtensionsType, loggerMessageType), OperationKind.Invocation);
                }
            });
        }

        private void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol loggerType, INamedTypeSymbol loggerExtensionsType, INamedTypeSymbol loggerMessageType)
        {
            var invocation = (IInvocationOperation)context.Operation;

            var methodSymbol = invocation.TargetMethod;
            var containingType = methodSymbol.ContainingType;

            //Ensure the method call belongs to one of the logging types
            if (!containingType.Equals(loggerType, SymbolEqualityComparer.Default) &&
                !containingType.Equals(loggerExtensionsType, SymbolEqualityComparer.Default) &&
                !containingType.Equals(loggerMessageType, SymbolEqualityComparer.Default))
            {
                return;
            }

            if (!FindLogParameters(methodSymbol, out var messageArgument))
            {
                return;
            }

            var arg = invocation.Arguments.FirstOrDefault(argument =>
            {
                var parameter = argument.Parameter;
                return SymbolEqualityComparer.Default.Equals(parameter, messageArgument);
            });

            if (arg?.Value is not null)
            {
                AnalyzeFormatArgument(context, formatExpression: arg?.Value);
            }
        }

        private void AnalyzeFormatArgument(OperationAnalysisContext context, IOperation formatExpression)
        {
            var text = TryGetFormatText(formatExpression);
            if (text == null)
            {
                return;
            }

            LogValuesFormatter formatter;
            try
            {
                formatter = new LogValuesFormatter(text);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return;
            }

            if (!IsValidMessageTemplate(formatter.OriginalFormat))
            {
                var diagnostic = Diagnostic.Create(InvalidTemplateStringRule, formatExpression.Syntax.GetLocation(), text);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private string TryGetFormatText(IOperation argumentExpression)
        {
            if (argumentExpression is null)
            {
                return null;
            }

            switch (argumentExpression)
            {
                case IOperation { ConstantValue: { HasValue: true, Value: string constantValue } }:
                    return constantValue;
                case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } binary:
                    var leftText = TryGetFormatText(binary.LeftOperand);
                    var rightText = TryGetFormatText(binary.RightOperand);

                    if (leftText != null && rightText != null)
                    {
                        return leftText + rightText;
                    }

                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Is the message template valid? (no unclosed braces, no braces without an opening, and no unescaped braces)
        /// </summary>
        /// <param name="messageTemplate">The message template to check for validity.</param>
        /// <returns>When true braces are valid, false otherwise.</returns>
        public static bool IsValidMessageTemplate(string messageTemplate)
        {
            if (messageTemplate is null)
            {
                return false;
            }

            int index = 0;
            bool leftBrace = false;

            while (index < messageTemplate.Length)
            {
                if (messageTemplate[index] == '{')
                {
                    if (index < messageTemplate.Length - 1 && messageTemplate[index + 1] == '{')
                    {
                        index++;
                    }
                    else if (leftBrace)
                    {
                        return false;
                    }
                    else
                    {
                        leftBrace = true;
                    }
                }
                else if (messageTemplate[index] == '}')
                {
                    if (leftBrace)
                    {
                        leftBrace = false;
                    }
                    else if (index < messageTemplate.Length - 1 && messageTemplate[index + 1] == '}')
                    {
                        index++;
                    }
                    else
                    {
                        return false;
                    }
                }

                index++;
            }

            return !leftBrace;
        }

        private static bool FindLogParameters(IMethodSymbol methodSymbol, out IParameterSymbol message)
        {
            message = null;
            foreach (var parameter in methodSymbol.Parameters)
            {
                if (parameter.Type.SpecialType == SpecialType.System_String &&
                    (string.Equals(parameter.Name, "message", StringComparison.Ordinal) ||
                    string.Equals(parameter.Name, "messageFormat", StringComparison.Ordinal) ||
                    string.Equals(parameter.Name, "formatString", StringComparison.Ordinal)))
                {
                    message = parameter;
                }
                // When calling logger.BeginScope("{Param}") generic overload would be selected
                else if (parameter.Type.SpecialType == SpecialType.System_String &&
                    methodSymbol.Name.Equals("BeginScope") &&
                    string.Equals(parameter.Name, "state", StringComparison.Ordinal))
                {
                    message = parameter;
                }
            }

            return message != null;
        }
    }
}