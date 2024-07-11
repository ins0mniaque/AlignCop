using Microsoft.VisualStudio.TestTools.UnitTesting;

using AlignCop.Analyzers.Rules;

namespace AlignCop.Analyzers.Tests.Rules;

using VerifyCS = CSharpCodeFixVerifier<AlignVariableAssignmentsAnalyzer, AlignVariableAssignmentsFixer>;

[TestClass]
public class AlignVariableAssignmentsTests
{
    [TestMethod]
    public async Task NoAssignments()
    {
        await VerifyCS.VerifyAnalyzerAsync(string.Empty);
    }

    [TestMethod]
    public async Task SingleLineAssignment()
    {
        var source = """
            namespace AlignVariableAssignments
            {
                class ClassToAlign
                {
                    void MethodToAlign()
                    {
                        var assigned = 0;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task AlignedLocalAssignments()
    {
        var source = """
            namespace AlignVariableAssignments
            {
                class ClassToAlign
                {
                    void MethodToAlign()
                    {
                        var assignedFirst  = 0;
                        var assignedSecond = 0;
                        var assignedThird  = 0;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task UnalignedLocalAssignments()
    {
        var source = """
            namespace AlignVariableAssignments
            {
                class ClassToAlign
                {
                    void MethodToAlign()
                    {
                        var assignedFirst {|#0:= 0|};
                        var assignedSecond {|#1:= 0|};
                        var assignedThird {|#2:= 0|};
                    }
                }
            }
            """;

        var fixedSource = """
            namespace AlignVariableAssignments
            {
                class ClassToAlign
                {
                    void MethodToAlign()
                    {
                        var assignedFirst  = 0;
                        var assignedSecond = 0;
                        var assignedThird  = 0;
                    }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(RuleIdentifiers.AlignVariableAssignments)
                               .WithLocation(0)
                               .WithLocation(1)
                               .WithLocation(2);

        await VerifyCS.VerifyCodeFixAsync(source, expected, fixedSource);
    }
}