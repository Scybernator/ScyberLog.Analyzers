//Original Source: https://github.com/dotnet/roslyn-analyzers/blob/main/src/NetAnalyzers/UnitTests/Microsoft.NetCore.Analyzers/Runtime/LoggerMessageDefineTests.cs
//Original and Modified code is under the MIT license;

using Test.Utilities;
using VerifyCS = ScyberLog.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<ScyberLog.Analyzers.LoggerMessageDefineAnalyzer>;

namespace ScyberLog.Analyzers.Tests
{
    public class LoggerMessageDefineTests
    {
        private async Task TriggerCodeAsync(string expression)
        {
            string code = @$"
using Microsoft.Extensions.Logging;
public class Program
{{
    public const string Const = ""const"";
    public static void Main()
    {{
        ILogger logger = null;
        
            {expression}
    }}
}}";
            await new VerifyCS.Test
            {
                TestCode = code,
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithMELogging,
            }.RunAsync();
        }

        //These tests can be dodgy for test setup reasons (failing to find required libraries, etc)
        //but once the succeed once they always pass.  Running a single test is more reliable than
        //running them all.
        [Theory]
        [InlineData(@"LoggerMessage.DefineScope<int>({|AA0008:""{{One}""|});")]
        [InlineData(@"LoggerMessage.DefineScope<int>({|AA0008:""}{One}""|});")]
        [InlineData(@"LoggerMessage.DefineScope<int>({|AA0008:""{One{Two}""|});")]
        [InlineData(@"LoggerMessage.DefineScope<int>({|AA0008:""{One}{""|});")]
        [InlineData(@"LoggerMessage.DefineScope<int>({|AA0008:""{One}}""|});")]
        [InlineData(@"LoggerMessage.DefineScope<int>({|AA0008:""{{{One}}""|});")]
        [InlineData(@"LoggerMessage.DefineScope<int>({|AA0008:""}}{One}}""|});")]
        [InlineData(@"LoggerMessage.DefineScope<int>({|AA0008:""{{{One}{""|});")]
        [InlineData(@"LoggerMessage.DefineScope<int>({|AA0008:""}}{One}{""|});")]
        [InlineData(@"LoggerMessage.DefineScope<int, int>({|AA0008:""}}{One} {Two}{""|});")]
        public async Task AA0008IsProducedWhenBracesAreInvalid(string format)
        {
            await TriggerCodeAsync(format);
        }

        [Theory]
        [InlineData(@"LoggerMessage.DefineScope<int>(""Some logged value: {One}}} with an escaped brace"");")]
        [InlineData(@"LoggerMessage.DefineScope<int, int>(""}}Some logged value: {One}}} with an {Two}{{ escaped brace"");")]
        [InlineData(@"LoggerMessage.DefineScope<int, int>(""{{Some logged {{value: {One}}} with an {Two}{{{{ escaped brace{{}}"");")]
        public async Task AA0008IsNotProducedWhenBracesAreEscapedAndOtherwiseValid(string format)
        {
            await TriggerCodeAsync(format);
        }

        [Theory]
        [InlineData(@"logger.Log(LogLevel.Error, {|AA0008:""{""|});")]
        [InlineData(@"logger.LogInformation({|AA0008:""{""|});")]
        [InlineData(@"logger.LogDebug({|AA0008:""{""|});")]
        [InlineData(@"logger.LogTrace({|AA0008:""{""|});")]
        [InlineData(@"logger.LogWarning({|AA0008:""{""|});")]
        [InlineData(@"logger.LogError({|AA0008:""{""|});")]
        [InlineData(@"logger.LogCritical({|AA0008:""{""|});")]
        public async Task AA008IsProducedForInvalidLoggerExtension(string format)
        {
            await TriggerCodeAsync(format);
        }

        [Theory]
        [InlineData(@"LoggerMessage.Define(LogLevel.Error, new EventId(8), {|AA0008:""{""|});")]
        public async Task AA008IsProducedForInvalidLoggerMessageDefine(string format)
        {
            await TriggerCodeAsync(format);
        }

        [Theory]
        [InlineData("", true)]
        [InlineData(null, false)]
        [InlineData("{One}", true)]
        [InlineData("{One{", false)]
        [InlineData("{{One}", false)]
        [InlineData("{{One}}", true)]
        public void IsValidMessageTemplate_ShouldReturnExpectedResult(string messageTemplate, bool expectedResult)
        {
            var result = LoggerMessageDefineAnalyzer.IsValidMessageTemplate(messageTemplate);

            Assert.Equal(expectedResult, result);
        }
    }
}
