using Content.Shared._Sunrise.SunriseCCVars;
using NUnit.Framework;
using Robust.Shared.Configuration;

namespace Content.Tests.Shared._Sunrise.CCVar;

[TestFixture]
public sealed class MakuraAuthCVarTests
{
    [Test]
    public void InternalApiUrlIsConfidential()
    {
        Assert.That(SunriseCCVars.MakuraAuthInternalApiUrl.Flags.HasFlag(CVar.CONFIDENTIAL), Is.True);
    }
}
