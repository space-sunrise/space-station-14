using System.Linq;
using Content.Shared._Sunrise.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    private static HumanoidCharacterProfile ApplySunriseProfileData(
        HumanoidCharacterProfile humanoid,
        Profile profile,
        Sex sex)
    {
        var voice = profile.Voice;
        if (voice == string.Empty)
            voice = SunriseHumanoidProfileDefaults.DefaultSexVoice[sex];
        var jobAlternativeTitles = profile.JobAlternativeTitles.ToDictionary(
            job => new ProtoId<JobPrototype>(job.JobName),
            job => new LocId(job.Title));

        return humanoid
            .WithVoice(voice)
            .WithBodyType(profile.BodyType)
            .WithSize(profile.Width, profile.Height)
            .WithJobAlternativeTitles(jobAlternativeTitles);
    }

    private static void StoreSunriseProfileData(Profile profile, HumanoidCharacterProfile humanoid)
    {
        profile.Voice = humanoid.Voice;
        profile.BodyType = humanoid.BodyType;
        profile.Width = humanoid.Width;
        profile.Height = humanoid.Height;

        profile.JobAlternativeTitles.Clear();
        profile.JobAlternativeTitles.AddRange(
            humanoid.JobAlternativeTitles.Select(job => new JobAlternativeTitle
            {
                JobName = job.Key,
                Title = job.Value.Id,
            }));
    }
}
