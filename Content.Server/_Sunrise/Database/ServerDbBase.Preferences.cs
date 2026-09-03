using System.Linq;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    private static void StoreSunriseProfileData(Profile profile, HumanoidCharacterProfile humanoid)
    {
        profile.Voice = humanoid.Voice;
        profile.BodyType = humanoid.BodyType;
        profile.Width = humanoid.Width;
        profile.Height = humanoid.Height;

        // Sunrise added start — сохраняем досье персонажа
        profile.Patronymic = humanoid.Patronymic;
        profile.BirthDay = humanoid.BirthDay;
        profile.BirthMonth = humanoid.BirthMonth;
        profile.MedicalRecord = humanoid.MedicalRecord;
        profile.SecurityRecord = humanoid.SecurityRecord;
        profile.EmploymentRecord = humanoid.EmploymentRecord;
        // Sunrise added end

        profile.JobAlternativeTitles.Clear();
        profile.JobAlternativeTitles.AddRange(
            humanoid.JobAlternativeTitles.Select(job => new JobAlternativeTitle
            {
                JobName = job.Key,
                Title = job.Value.Id,
            }));
    }
}
