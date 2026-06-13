using System;

namespace LasDetox.Patients
{
    [Serializable]
    public class PatientDefinitionDto
    {
        public int schemaVersion;
        public string id;
        public string visualProfileId;
        public IdentityDto identity;
        public AdministrativeProfileDto administrativeProfile;
        public BackgroundDto background;
        public AddictionHistoryDto addictionHistory;
        public MedicalBackgroundDto medicalBackground;
        public PersonalityDto personality;
        public BehaviorProfileDto behaviorProfile;
        public PresentationDto presentation;
        public string[] tags;
    }

    [Serializable]
    public class IdentityDto
    {
        public string firstName;
        public string lastName;
        public string dateOfBirth;
        public string sex;
    }

    [Serializable]
    public class AdministrativeProfileDto
    {
        public string city;
        public EmergencyContactDto emergencyContact;
    }

    [Serializable]
    public class EmergencyContactDto
    {
        public string name;
        public string relationship;
        public string phone;
    }

    [Serializable]
    public class BackgroundDto
    {
        public string occupation;
        public string employmentStatus;
        public string relationshipStatus;
        public string housingStatus;
        public string shortBio;
    }

    [Serializable]
    public class AddictionHistoryDto
    {
        public string[] knownSubstances;
        public string primaryLongTermSubstance;
        public int yearsOfUse;
        public int previousDetoxCount;
        public int previousTherapyCount;
        public int longestAbstinenceDays;
        public bool previousTreatmentCompleted;
        public string historySummary;
    }

    [Serializable]
    public class MedicalBackgroundDto
    {
        public string[] chronicConditions;
        public string[] allergies;
    }

    [Serializable]
    public class PersonalityDto
    {
        public float sociability;
        public float impulsivity;
        public float ruleCompliance;
        public float conflictTendency;
        public float manipulativeness;
        public float helpSeeking;
    }

    [Serializable]
    public class BehaviorProfileDto
    {
        public float queuePatience;
        public float authorityTolerance;
        public float groupParticipation;
        public float isolationPreference;
        public float contrabandRisk;
        public float earlyDischargeRisk;
    }

    [Serializable]
    public class PresentationDto
    {
        public string shortDescription;
        public string[] visibleTraits;
        public string[] hiddenTraits;
    }
}
