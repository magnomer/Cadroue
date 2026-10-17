using System;
using System.Linq;
using System.Reflection;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TEnvoyGate
{
    private static readonly Type[] tEnvoyPlainTypes = [typeof(string), typeof(object), typeof(bool), typeof(void)];

    [Fact]
    public void EnvoyPort_ConductInterface_Seated()
    {
        Assert.True(typeof(CEnvoy).IsInterface);
        Assert.Equal(typeof(CAtelier).Assembly, typeof(CEnvoy).Assembly);
    }

    [Fact]
    public void EnvoyMembers_KeysAndAnswers_PlainTypesOnly()
    {
        MethodInfo[] methods = typeof(CEnvoy).GetMethods();

        Assert.NotEmpty(methods);
        Assert.All(methods, method =>
        {
            Assert.Contains(method.ReturnType, tEnvoyPlainTypes);
            Assert.All(method.GetParameters(), parameter => Assert.Contains(parameter.ParameterType, tEnvoyPlainTypes));
        });
    }

    [Fact]
    public void EnvoyMembers_AskAndShow_Named()
    {
        string[] names = typeof(CEnvoy).GetMethods().Select(static method => method.Name).Distinct().Order().ToArray();

        Assert.Equal(
            ["CEnvoyConfirm", "CEnvoyDestructiveConfirm", "CEnvoyFailureShow", "CEnvoyWarningShow"],
            names);
    }

    [Fact]
    public void EnvoyConfirm_AnswerOnly_Returned()
    {
        Assert.All(
            typeof(CEnvoy).GetMethods()
                .Where(static method => method.Name.EndsWith("Confirm", StringComparison.Ordinal)),
            static method => Assert.Equal(typeof(bool), method.ReturnType));
    }
}
