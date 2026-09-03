using System.Collections.Generic;
using System.Linq;
using Content.Shared._Sunrise.Records;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Content.Tests.Shared._Sunrise.Records;

[TestFixture]
public sealed class StructuredCharacterRecordsTest
{
    [Test]
    public void LegacyMedicalRecordIsPreservedInNotes()
    {
        const string legacy = "Legacy allergy record";

        var record = StructuredCharacterRecords.ReadMedical(legacy);

        Assert.That(record.Notes, Is.EqualTo(legacy));
    }

    [Test]
    public void MaximumLegacyRecordIsNotTruncatedDuringMigration()
    {
        var legacy = new string('x', StructuredCharacterRecords.MaxNotesTextLength);

        var restored = StructuredCharacterRecords.ReadSecurity(legacy);

        Assert.That(restored.Notes, Is.EqualTo(legacy));
    }

    [Test]
    public void MedicalRecordRoundTripsDelimiters()
    {
        var original = new MedicalRecordData
        {
            EmergencyContact = "Ivan: 12;34",
            Notes = "Line 1\nLine 2",
        };

        var restored = StructuredCharacterRecords.ReadMedical(
            StructuredCharacterRecords.WriteMedical(original));

        Assert.Multiple(() =>
        {
            Assert.That(restored.EmergencyContact, Is.EqualTo(original.EmergencyContact));
            Assert.That(restored.Notes, Is.EqualTo(original.Notes));
        });
    }

    [Test]
    public void EmploymentRecordRoundTripsEducationEntries()
    {
        var original = new EmploymentRecordData
        {
            AcademicTitle = RecordAcademicTitle.Professor,
            AcademicTitleField = "Navigation and traffic control",
            Licenses = "Shuttle piloting",
            Education = new List<EducationRecordData>
            {
                new()
                {
                    Specialty = "Space navigation",
                    Degree = RecordAcademicDegree.Master,
                    Institution = "Inter-Species University of Orion",
                    DiplomaDate = "12.06.2868",
                },
                new()
                {
                    Specialty = "Engineering",
                    Degree = RecordAcademicDegree.Bachelor,
                },
            },
        };

        var restored = StructuredCharacterRecords.ReadEmployment(
            StructuredCharacterRecords.WriteEmployment(original));

        Assert.Multiple(() =>
        {
            Assert.That(restored.AcademicTitle, Is.EqualTo(RecordAcademicTitle.Professor));
            Assert.That(restored.AcademicTitleField, Is.EqualTo(original.AcademicTitleField));
            Assert.That(restored.Education, Has.Count.EqualTo(2));
            Assert.That(restored.Education[0].Specialty, Is.EqualTo("Space navigation"));
            Assert.That(restored.Education[0].Degree, Is.EqualTo(RecordAcademicDegree.Master));
            Assert.That(restored.Education[1].Degree, Is.EqualTo(RecordAcademicDegree.Bachelor));
        });
    }

    [Test]
    public void DamagedStructuredRecordRemainsVisible()
    {
        const string damaged = "SUNRISE_SECURITY_V1:2;5:test";

        var record = StructuredCharacterRecords.ReadSecurity(damaged);

        Assert.That(record.Notes, Is.EqualTo(damaged));
    }

    [Test]
    public void LegacyResidenceIsPreservedAsAddressDetails()
    {
        const string legacy = "Alpha Centauri, residential block 12";

        var residence = StructuredCharacterRecords.ReadResidence(legacy);

        Assert.That(residence.Details, Is.EqualTo(legacy));
    }

    [Test]
    public void StructuredResidenceRoundTrips()
    {
        var original = new ResidenceRecordData
        {
            Region = "Frontier District",
            Planet = "New Moscow",
            Street = "Block 7",
            Unit = "Apartment 42",
            Details = "Courtyard entrance",
        };

        var restored = StructuredCharacterRecords.ReadResidence(
            StructuredCharacterRecords.WriteResidence(original));

        Assert.Multiple(() =>
        {
            Assert.That(restored.Region, Is.EqualTo(original.Region));
            Assert.That(restored.Planet, Is.EqualTo(original.Planet));
            Assert.That(restored.Street, Is.EqualTo(original.Street));
            Assert.That(restored.Unit, Is.EqualTo(original.Unit));
            Assert.That(restored.Details, Is.EqualTo(original.Details));
        });
    }

    [Test]
    public void SecurityRecordDoesNotAlterNestedResidencePayload()
    {
        const string residence = "SUNRISE_ADDRESS_V1:5;0:10:[bold]Luna0:0:0:";
        var storage = StructuredCharacterRecords.WriteSecurity(new SecurityRecordData
        {
            Residence = residence,
        });

        var restored = StructuredCharacterRecords.ReadSecurity(storage);

        Assert.That(restored.Residence, Is.EqualTo(residence));
    }

    [Test]
    public void PrintedSecurityRecordUsesMarkupInsteadOfSerializedStorage()
    {
        var storage = StructuredCharacterRecords.WriteSecurity(new SecurityRecordData
        {
            Residence = StructuredCharacterRecords.WriteResidence(new ResidenceRecordData
            {
                Planet = "Luna",
            }),
            Notes = "Verified by security staff",
        });

        var printed = StructuredRecordFormatter.FormatSecurity(storage, key => key, "180 cm", "75 kg");

        Assert.Multiple(() =>
        {
            Assert.That(printed, Does.Not.Contain("SUNRISE_SECURITY_V1"));
            Assert.That(printed, Does.Not.Contain("SUNRISE_ADDRESS_V1"));
            Assert.That(printed, Does.Contain("Luna"));
            Assert.That(FormattedMessage.TryFromMarkup(printed, out _), Is.True);
        });
    }

    [Test]
    public void PrintedRecordEscapesAuthorMarkup()
    {
        var storage = StructuredCharacterRecords.WriteMedical(new MedicalRecordData
        {
            Notes = "[bold]author markup[/bold]",
        });

        var printed = StructuredRecordFormatter.FormatMedical(storage, key => key, "180 cm", "75 kg", "none");

        Assert.Multiple(() =>
        {
            Assert.That(printed, Does.Not.Contain("[bold]author markup[/bold]"));
            Assert.That(printed, Does.Contain("author markup"));
            Assert.That(FormattedMessage.TryFromMarkup(printed, out _), Is.True);
        });
    }
}
