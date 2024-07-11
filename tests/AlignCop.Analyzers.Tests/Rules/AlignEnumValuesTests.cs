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
                    None  = 0,
                    Value = 1,
                    All   = 2
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
                    None {|#0:= 0|},
                    Value {|#1:= 1|},
                    All {|#2:= 2|}
                }
            }
            """;

        var fixedSource = """
            namespace AlignEnumValues
            {
                enum EnumToAlign
                {
                    None  = 0,
                    Value = 1,
                    All   = 2
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

    [TestMethod]
    public async Task AlignedMultiBlockEnum()
    {
        var source = """
            namespace AlignEnumValues
            {
                enum EnumToAlign
                {
                    None  = 0,
                    Value = 1,
                    All   = 2,

                    AlignedA = 3,
                    AlignedB = 4,
                    AlignedC = 5,

                    UnalignedX   = 6,
                    UnalignedXX  = 7,
                    UnalignedXXX = 8
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task UnalignedMultiBlockEnum()
    {
        var source = """
            namespace AlignEnumValues
            {
                enum EnumToAlign
                {
                    None {|#0:= 0|},
                    Value {|#1:= 1|},
                    All {|#2:= 2|},

                    AlignedA = 3,
                    AlignedB = 4,
                    AlignedC = 5,

                    UnalignedX {|#3:= 6|},
                    UnalignedXX {|#4:= 7|},
                    UnalignedXXX {|#5:= 8|}
                }
            }
            """;

        var fixedSource = """
            namespace AlignEnumValues
            {
                enum EnumToAlign
                {
                    None  = 0,
                    Value = 1,
                    All   = 2,

                    AlignedA = 3,
                    AlignedB = 4,
                    AlignedC = 5,

                    UnalignedX   = 6,
                    UnalignedXX  = 7,
                    UnalignedXXX = 8
                }
            }
            """;

        var expectedA = VerifyCS.Diagnostic(RuleIdentifiers.AlignEnumValues)
                                .WithLocation(0)
                                .WithLocation(1)
                                .WithLocation(2)
                                .WithArguments("EnumToAlign");

        var expectedB = VerifyCS.Diagnostic(RuleIdentifiers.AlignEnumValues)
                                .WithLocation(3)
                                .WithLocation(4)
                                .WithLocation(5)
                                .WithArguments("EnumToAlign");

        await VerifyCS.VerifyCodeFixAsync(source, new[] { expectedA, expectedB }, fixedSource);
    }
}