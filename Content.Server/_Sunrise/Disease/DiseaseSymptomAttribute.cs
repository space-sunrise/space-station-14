// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

namespace Content.Server._Sunrise.Disease;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DiseaseSymptomAttribute : Attribute
{
    public string Id { get; }

    public DiseaseSymptomAttribute(string id)
    {
        Id = id;
    }
}