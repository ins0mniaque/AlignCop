using Microsoft.VisualStudio.TestTools.UnitTesting;

using AlignCop.Analyzers.Rules;

namespace AlignCop.Analyzers.Tests.Rules;

using VerifyCS = CSharpCodeFixVerifier<AlignEnumValuesAnalyzer, AlignEnumValuesFixer>;

[TestClass]
public class AlignEnumValuesTests
{
    [TestMethod]
    public async Task NoEnum()
    {
        await VerifyCS.VerifyAnalyzerAsync(string.Empty).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SingleLineEnum()
    {
        var source = """
            namespace AlignEnumValues
            {
                enum EnumToAlign { None = 0, Value = 1, All = 2 }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
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

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
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

        await VerifyCS.VerifyCodeFixAsync(source, expected, fixedSource).ConfigureAwait(false);
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

        await VerifyCS.VerifyAnalyzerAsync(source).ConfigureAwait(false);
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

        await VerifyCS.VerifyCodeFixAsync(source, [expectedA, expectedB], fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MissingValuesEnum()
    {
        var source = """
            namespace AlignEnumValues
            {
                enum EnumToAlign
                {
                    None,
                    Value {|#0:= 1|},
                    All {|#1:= 2|},
                    MissingA,
                    UnalignedX {|#2:= 6|},
                    UnalignedXX {|#3:= 7|},
                    UnalignedXXX {|#4:= 8|},
                    MissingB
                }
            }
            """;

        var fixedSource = """
            namespace AlignEnumValues
            {
                enum EnumToAlign
                {
                    None,
                    Value = 1,
                    All   = 2,
                    MissingA,
                    UnalignedX   = 6,
                    UnalignedXX  = 7,
                    UnalignedXXX = 8,
                    MissingB
                }
            }
            """;

        var expectedA = VerifyCS.Diagnostic(RuleIdentifiers.AlignEnumValues)
                                .WithLocation(0)
                                .WithLocation(1)
                                .WithArguments("EnumToAlign");

        var expectedB = VerifyCS.Diagnostic(RuleIdentifiers.AlignEnumValues)
                                .WithLocation(2)
                                .WithLocation(3)
                                .WithLocation(4)
                                .WithArguments("EnumToAlign");

        await VerifyCS.VerifyCodeFixAsync(source, [expectedA, expectedB], fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SameLineValuesEnum()
    {
        var source = """
            namespace AlignEnumValues
            {
                enum EnumToAlign
                {
                    SameLineLeft = -2, SameLineRight = -1,
                    None {|#0:= 0|},
                    Value {|#1:= 1|},
                    All {|#2:= 2|},
                    SameLineA {|#3:= 3|}, SameLineB = 4,
                    UnalignedX {|#4:= 5|},
                    UnalignedXX {|#5:= 6|},
                    UnalignedXXX {|#6:= 7|},
                    SameLineC {|#7:= 8|}, SameLineD = 9
                }
            }
            """;

        var fixedSource = """
            namespace AlignEnumValues
            {
                enum EnumToAlign
                {
                    SameLineLeft = -2, SameLineRight = -1,
                    None      = 0,
                    Value     = 1,
                    All       = 2,
                    SameLineA = 3, SameLineB = 4,
                    UnalignedX   = 5,
                    UnalignedXX  = 6,
                    UnalignedXXX = 7,
                    SameLineC    = 8, SameLineD = 9
                }
            }
            """;

        var expectedA = VerifyCS.Diagnostic(RuleIdentifiers.AlignEnumValues)
                                .WithLocation(0)
                                .WithLocation(1)
                                .WithLocation(2)
                                .WithLocation(3)
                                .WithArguments("EnumToAlign");

        var expectedB = VerifyCS.Diagnostic(RuleIdentifiers.AlignEnumValues)
                                .WithLocation(4)
                                .WithLocation(5)
                                .WithLocation(6)
                                .WithLocation(7)
                                .WithArguments("EnumToAlign");

        await VerifyCS.VerifyCodeFixAsync(source, [expectedA, expectedB], fixedSource).ConfigureAwait(false);
    }
}