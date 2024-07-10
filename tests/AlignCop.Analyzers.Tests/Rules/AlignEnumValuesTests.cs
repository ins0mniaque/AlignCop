using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AlignCop.Analyzers.Tests;

using VerifyCS = CSharpCodeFixVerifier<AlignEnumValuesAnalyzer, AlignEnumValuesFixer>;

[TestClass]
public class AlignEnumValuesTests
{
    [TestMethod]
    public async Task NoEnum()
    {
        await VerifyCS.VerifyAnalyzerAsync(string.Empty);
    }

    [TestMethod]
    public async Task AlignedSingleBlockEnum()
    {
        var source = """
            namespace AlignEnumValues
            {
                enum EnumToAlign
                {
                    Value  = 0,
                    ValueB = 1,
                    All    = 2
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task UnalignedSingleBlockEnum()
    {
        var source = """
            namespace AlignEnumValues
            {
                enum EnumToAlign
                {
                    {|#0:Value = 0|},
                    {|#1:ValueB = 1|},
                    {|#2:All = 2|}
                }
            }
            """;

        var fixedSource = """
            namespace AlignEnumValues
            {
                enum EnumToAlign
                {
                    Value  = 0,
                    ValueB = 1,
                    All    = 2
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(RuleIdentifiers.AlignEnumValues)
                               .WithLocation(0)
                               .WithLocation(1)
                               .WithLocation(2)
                               .WithArguments("EnumToAlign");

        await VerifyCS.VerifyCodeFixAsync(source, expected, fixedSource);
    }
}