using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LasDetox.Patients
{
    public static class PatientDefinitionValidator
    {
        private const int ExpectedSchemaVersion = 1;
        private static readonly Regex PatientIdPattern = new(@"^detox_patient_\d{3}$", RegexOptions.Compiled);
        private static readonly Regex DateOfBirthPattern = new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);
        private static readonly Regex TagPattern = new(@"^[a-z][a-z0-9_]*$", RegexOptions.Compiled);

        private static readonly HashSet<string> AllowedSex = new(StringComparer.Ordinal)
        {
            "male", "female"
        };

        private static readonly HashSet<string> AllowedRelationships = new(StringComparer.Ordinal)
        {
            "spouse", "partner", "parent", "sibling", "child", "friend", "guardian", "other"
        };

        private static readonly HashSet<string> AllowedEmploymentStatus = new(StringComparer.Ordinal)
        {
            "employed", "unemployed", "self_employed", "retired", "student", "disability", "other"
        };

        private static readonly HashSet<string> AllowedRelationshipStatus = new(StringComparer.Ordinal)
        {
            "single", "married", "partnered", "divorced", "widowed", "separated", "other"
        };

        private static readonly HashSet<string> AllowedHousingStatus = new(StringComparer.Ordinal)
        {
            "stable", "temporary", "homeless", "institutional", "unknown"
        };

        private static readonly HashSet<string> AllowedSubstances = new(StringComparer.Ordinal)
        {
            "alcohol", "benzodiazepines", "opioids", "stimulants", "cannabis", "other"
        };

        public static bool TryValidateCatalog(PatientCatalogDto catalog, List<string> errors)
        {
            if (catalog == null)
            {
                errors.Add("Catalog is null or could not be deserialized.");
                return false;
            }

            if (catalog.schemaVersion != ExpectedSchemaVersion)
            {
                errors.Add($"Catalog schemaVersion must be {ExpectedSchemaVersion}, got {catalog.schemaVersion}.");
                return false;
            }

            if (catalog.patientIds == null || catalog.patientIds.Length == 0)
            {
                errors.Add("Catalog patientIds is empty.");
                return false;
            }

            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < catalog.patientIds.Length; i++)
            {
                var patientId = catalog.patientIds[i];
                if (string.IsNullOrWhiteSpace(patientId))
                {
                    errors.Add($"Catalog patientIds[{i}] is empty.");
                    continue;
                }

                if (!PatientIdPattern.IsMatch(patientId))
                    errors.Add($"Catalog patientIds[{i}] has invalid format: '{patientId}'.");

                if (!seenIds.Add(patientId))
                    errors.Add($"Catalog contains duplicate patient id '{patientId}'.");
            }

            return errors.Count == 0;
        }

        public static bool TryValidateDefinition(PatientDefinitionDto definition, string expectedCatalogId, List<string> errors)
        {
            var contextId = string.IsNullOrWhiteSpace(expectedCatalogId)
                ? definition?.id ?? "(unknown)"
                : expectedCatalogId;

            if (definition == null)
            {
                errors.Add($"{contextId}: Definition is null or could not be deserialized.");
                return false;
            }

            if (definition.schemaVersion != ExpectedSchemaVersion)
            {
                errors.Add($"{contextId}: schemaVersion must be {ExpectedSchemaVersion}, got {definition.schemaVersion}.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(definition.id))
            {
                errors.Add($"{contextId}: id is empty.");
                return false;
            }

            contextId = definition.id;

            if (!string.IsNullOrWhiteSpace(expectedCatalogId) &&
                !string.Equals(definition.id, expectedCatalogId, StringComparison.Ordinal))
            {
                errors.Add($"{contextId}: id '{definition.id}' does not match catalog entry '{expectedCatalogId}'.");
                return false;
            }

            if (definition.identity == null)
            {
                errors.Add($"{contextId}: identity is required.");
                return false;
            }

            if (definition.administrativeProfile == null)
            {
                errors.Add($"{contextId}: administrativeProfile is required.");
                return false;
            }

            if (definition.background == null)
            {
                errors.Add($"{contextId}: background is required.");
                return false;
            }

            if (definition.addictionHistory == null)
            {
                errors.Add($"{contextId}: addictionHistory is required.");
                return false;
            }

            if (definition.medicalBackground == null)
            {
                errors.Add($"{contextId}: medicalBackground is required.");
                return false;
            }

            if (definition.personality == null)
            {
                errors.Add($"{contextId}: personality is required.");
                return false;
            }

            if (definition.behaviorProfile == null)
            {
                errors.Add($"{contextId}: behaviorProfile is required.");
                return false;
            }

            if (definition.presentation == null)
            {
                errors.Add($"{contextId}: presentation is required.");
                return false;
            }

            ValidateIdentity(definition.identity, contextId, errors);
            ValidateAdministrativeProfile(definition.administrativeProfile, contextId, errors);
            ValidateBackground(definition.background, contextId, errors);
            ValidateAddictionHistory(definition.addictionHistory, contextId, errors);
            ValidateMedicalBackground(definition.medicalBackground, contextId, errors);
            ValidatePersonality(definition.personality, contextId, errors);
            ValidateBehaviorProfile(definition.behaviorProfile, contextId, errors);
            ValidatePresentation(definition.presentation, contextId, errors);
            ValidateTags(definition.tags, contextId, errors);

            return errors.Count == 0;
        }

        private static void ValidateIdentity(IdentityDto identity, string contextId, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(identity.firstName))
                errors.Add($"{contextId}: identity.firstName is required.");

            if (string.IsNullOrWhiteSpace(identity.lastName))
                errors.Add($"{contextId}: identity.lastName is required.");

            if (string.IsNullOrWhiteSpace(identity.dateOfBirth))
            {
                errors.Add($"{contextId}: identity.dateOfBirth is required.");
            }
            else if (!DateOfBirthPattern.IsMatch(identity.dateOfBirth))
            {
                errors.Add($"{contextId}: identity.dateOfBirth must match YYYY-MM-DD.");
            }
            else if (!DateTime.TryParseExact(
                         identity.dateOfBirth,
                         "yyyy-MM-dd",
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.None,
                         out _))
            {
                errors.Add($"{contextId}: identity.dateOfBirth is not a valid calendar date.");
            }

            if (string.IsNullOrWhiteSpace(identity.sex))
            {
                errors.Add($"{contextId}: identity.sex is required.");
            }
            else if (!AllowedSex.Contains(identity.sex))
            {
                errors.Add($"{contextId}: identity.sex '{identity.sex}' is not allowed.");
            }
        }

        private static void ValidateAdministrativeProfile(
            AdministrativeProfileDto profile,
            string contextId,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(profile.city))
                errors.Add($"{contextId}: administrativeProfile.city is required.");

            if (IsEmergencyContactEmpty(profile.emergencyContact))
                return;

            if (string.IsNullOrWhiteSpace(profile.emergencyContact.name))
                errors.Add($"{contextId}: emergencyContact.name is required when emergencyContact is present.");

            if (string.IsNullOrWhiteSpace(profile.emergencyContact.relationship))
            {
                errors.Add($"{contextId}: emergencyContact.relationship is required when emergencyContact is present.");
            }
            else if (!AllowedRelationships.Contains(profile.emergencyContact.relationship))
            {
                errors.Add($"{contextId}: emergencyContact.relationship '{profile.emergencyContact.relationship}' is not allowed.");
            }

            if (string.IsNullOrWhiteSpace(profile.emergencyContact.phone))
                errors.Add($"{contextId}: emergencyContact.phone is required when emergencyContact is present.");
        }

        private static bool IsEmergencyContactEmpty(EmergencyContactDto contact)
        {
            if (contact == null)
                return true;

            return string.IsNullOrWhiteSpace(contact.name) &&
                   string.IsNullOrWhiteSpace(contact.relationship) &&
                   string.IsNullOrWhiteSpace(contact.phone);
        }

        private static void ValidateBackground(BackgroundDto background, string contextId, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(background.employmentStatus))
            {
                errors.Add($"{contextId}: background.employmentStatus is required.");
            }
            else if (!AllowedEmploymentStatus.Contains(background.employmentStatus))
            {
                errors.Add($"{contextId}: background.employmentStatus '{background.employmentStatus}' is not allowed.");
            }

            if (string.IsNullOrWhiteSpace(background.relationshipStatus))
            {
                errors.Add($"{contextId}: background.relationshipStatus is required.");
            }
            else if (!AllowedRelationshipStatus.Contains(background.relationshipStatus))
            {
                errors.Add($"{contextId}: background.relationshipStatus '{background.relationshipStatus}' is not allowed.");
            }

            if (string.IsNullOrWhiteSpace(background.housingStatus))
            {
                errors.Add($"{contextId}: background.housingStatus is required.");
            }
            else if (!AllowedHousingStatus.Contains(background.housingStatus))
            {
                errors.Add($"{contextId}: background.housingStatus '{background.housingStatus}' is not allowed.");
            }

            if (string.IsNullOrWhiteSpace(background.shortBio))
                errors.Add($"{contextId}: background.shortBio is required.");
        }

        private static void ValidateAddictionHistory(AddictionHistoryDto history, string contextId, List<string> errors)
        {
            if (history.knownSubstances == null || history.knownSubstances.Length == 0)
            {
                errors.Add($"{contextId}: addictionHistory.knownSubstances must contain at least one entry.");
            }
            else
            {
                for (var i = 0; i < history.knownSubstances.Length; i++)
                {
                    var substance = history.knownSubstances[i];
                    if (string.IsNullOrWhiteSpace(substance))
                    {
                        errors.Add($"{contextId}: addictionHistory.knownSubstances[{i}] is empty.");
                        continue;
                    }

                    if (!AllowedSubstances.Contains(substance))
                        errors.Add($"{contextId}: addictionHistory.knownSubstances[{i}] '{substance}' is not allowed.");
                }
            }

            if (string.IsNullOrWhiteSpace(history.primaryLongTermSubstance))
            {
                errors.Add($"{contextId}: addictionHistory.primaryLongTermSubstance is required.");
            }
            else if (!AllowedSubstances.Contains(history.primaryLongTermSubstance))
            {
                errors.Add($"{contextId}: addictionHistory.primaryLongTermSubstance '{history.primaryLongTermSubstance}' is not allowed.");
            }
            else if (history.knownSubstances != null &&
                     Array.IndexOf(history.knownSubstances, history.primaryLongTermSubstance) < 0)
            {
                errors.Add($"{contextId}: primaryLongTermSubstance must be included in knownSubstances.");
            }

            if (history.yearsOfUse < 0)
                errors.Add($"{contextId}: addictionHistory.yearsOfUse must be >= 0.");

            if (history.previousDetoxCount < 0)
                errors.Add($"{contextId}: addictionHistory.previousDetoxCount must be >= 0.");

            if (history.previousTherapyCount < 0)
                errors.Add($"{contextId}: addictionHistory.previousTherapyCount must be >= 0.");

            if (history.longestAbstinenceDays < 0)
                errors.Add($"{contextId}: addictionHistory.longestAbstinenceDays must be >= 0.");

            if (string.IsNullOrWhiteSpace(history.historySummary))
                errors.Add($"{contextId}: addictionHistory.historySummary is required.");
        }

        private static void ValidateMedicalBackground(MedicalBackgroundDto medical, string contextId, List<string> errors)
        {
            if (medical.chronicConditions == null)
                errors.Add($"{contextId}: medicalBackground.chronicConditions is required (may be empty array).");

            if (medical.allergies == null)
                errors.Add($"{contextId}: medicalBackground.allergies is required (may be empty array).");
        }

        private static void ValidatePersonality(PersonalityDto personality, string contextId, List<string> errors)
        {
            ValidateUnitFloat(personality.sociability, $"{contextId}: personality.sociability", errors);
            ValidateUnitFloat(personality.impulsivity, $"{contextId}: personality.impulsivity", errors);
            ValidateUnitFloat(personality.ruleCompliance, $"{contextId}: personality.ruleCompliance", errors);
            ValidateUnitFloat(personality.conflictTendency, $"{contextId}: personality.conflictTendency", errors);
            ValidateUnitFloat(personality.manipulativeness, $"{contextId}: personality.manipulativeness", errors);
            ValidateUnitFloat(personality.helpSeeking, $"{contextId}: personality.helpSeeking", errors);
        }

        private static void ValidateBehaviorProfile(BehaviorProfileDto behavior, string contextId, List<string> errors)
        {
            ValidateUnitFloat(behavior.queuePatience, $"{contextId}: behaviorProfile.queuePatience", errors);
            ValidateUnitFloat(behavior.authorityTolerance, $"{contextId}: behaviorProfile.authorityTolerance", errors);
            ValidateUnitFloat(behavior.groupParticipation, $"{contextId}: behaviorProfile.groupParticipation", errors);
            ValidateUnitFloat(behavior.isolationPreference, $"{contextId}: behaviorProfile.isolationPreference", errors);
            ValidateUnitFloat(behavior.contrabandRisk, $"{contextId}: behaviorProfile.contrabandRisk", errors);
            ValidateUnitFloat(behavior.earlyDischargeRisk, $"{contextId}: behaviorProfile.earlyDischargeRisk", errors);
        }

        private static void ValidatePresentation(PresentationDto presentation, string contextId, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(presentation.shortDescription))
                errors.Add($"{contextId}: presentation.shortDescription is required.");

            if (presentation.visibleTraits == null || presentation.visibleTraits.Length == 0)
            {
                errors.Add($"{contextId}: presentation.visibleTraits must contain at least one entry.");
            }
            else
            {
                for (var i = 0; i < presentation.visibleTraits.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(presentation.visibleTraits[i]))
                        errors.Add($"{contextId}: presentation.visibleTraits[{i}] is empty.");
                }
            }
        }

        private static void ValidateTags(string[] tags, string contextId, List<string> errors)
        {
            if (tags == null || tags.Length == 0)
            {
                errors.Add($"{contextId}: tags must contain at least one entry.");
                return;
            }

            for (var i = 0; i < tags.Length; i++)
            {
                var tag = tags[i];
                if (string.IsNullOrWhiteSpace(tag))
                {
                    errors.Add($"{contextId}: tags[{i}] is empty.");
                    continue;
                }

                if (!TagPattern.IsMatch(tag))
                    errors.Add($"{contextId}: tags[{i}] '{tag}' must be snake_case.");
            }
        }

        private static void ValidateUnitFloat(float value, string fieldLabel, List<string> errors)
        {
            if (value < 0f || value > 1f)
                errors.Add($"{fieldLabel} must be in range 0.0–1.0, got {value.ToString(CultureInfo.InvariantCulture)}.");
        }
    }
}
