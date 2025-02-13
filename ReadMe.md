# ScyberLog.Analyzers
This repository contains a single analyzer to determine if a call to one of the logging methods defined on `Microsoft.Extensions.Logging.ILogger` has an invalid messaage template string. If the template is invalid, it will throw a runtime FormatException which is obviously bad because:
1. Your logging framework should not be crashing your app.
1. Much logging only ocurrs on the sad path, inside exception handlers, which is a place you do not want additional exceptions thrown.
1. Some log calls may only occur rarely (as on particular exceptions), setting a time bomb in your application.
1. You lose any information about the thing you were trying to log.

By default this diagnostic has its severity set to `Error` but can be downgraded to `Warning` by adding the following to your .editorconfig file:

```
# AA0008: Invalid log message template format.
dotnet_diagnostic.AA0008.severity = warning
```

Note that there is a built-in diagnostic (CA2023) for this in the upcoming NetAnalyzers release, at which point this package will be obsolete.

<!--

# Links
[Offending Change](https://source.dot.net/#Microsoft.Extensions.Logging.Abstractions/LogValuesFormatter.cs,47)
[Tutorial: Write your first analyzer and code fix](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
[Writing a Roslyn Analyzer](https://www.meziantou.net/writing-a-roslyn-analyzer.htm)
[Testing a Roslyn Analyzer](https://www.meziantou.net/how-to-test-a-roslyn-analyzer.htm)
[Localizing Analyzers](https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md)
[Analyzer Release Tracking](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md)
[Roslyn Docs](https://github.com/dotnet/roslyn/tree/main/docs)
[LoggerMessageDefineAnalyzer](https://github.com/dotnet/roslyn-analyzers/blob/main/src/NetAnalyzers/Core/Microsoft.NetCore.Analyzers/Runtime/LoggerMessageDefineAnalyzer.cs)
[LoggerMessageDefineTests](https://github.com/dotnet/roslyn-analyzers/blob/main/src/NetAnalyzers/UnitTests/Microsoft.NetCore.Analyzers/Runtime/LoggerMessageDefineTests.cs)
[Categories of Code Quality Rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/code-quality-rule-options#category-of-rules)
[Setting Rule Severity](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#severity-level)
-->