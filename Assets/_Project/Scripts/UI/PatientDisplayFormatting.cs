using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LasDetox.Patients;

namespace LasDetox.UI
{
    public static class PatientDisplayFormatting
    {
        private static readonly Dictionary<string, string> SexLabels = new(StringComparer.Ordinal)
        {
            { "male", "Mężczyzna" },
            { "female", "Kobieta" },
        };

        private static readonly Dictionary<string, string> EmploymentStatusLabels = new(StringComparer.Ordinal)
        {
            { "employed", "Zatrudniony" },
            { "unemployed", "Bezrobotny" },
            { "self_employed", "Samozatrudniony" },
            { "retired", "Emeryt" },
            { "student", "Student" },
            { "disability", "Rentownik" },
            { "other", "Inny" },
        };

        private static readonly Dictionary<string, string> RelationshipStatusLabels = new(StringComparer.Ordinal)
        {
            { "single", "Kawaler/Panna" },
            { "married", "Żonaty/Zamężna" },
            { "partnered", "W związku partnerskim" },
            { "divorced", "Po rozwodzie" },
            { "widowed", "Wdowiec/Wdowa" },
            { "separated", "W separacji" },
            { "other", "Inny" },
        };

        private static readonly Dictionary<string, string> HousingStatusLabels = new(StringComparer.Ordinal)
        {
            { "stable", "Stabilne" },
            { "temporary", "Tymczasowe" },
            { "homeless", "Bezdomny" },
            { "institutional", "Instytucjonalne" },
            { "unknown", "Nieznane" },
        };

        private static readonly Dictionary<string, string> SubstanceLabels = new(StringComparer.Ordinal)
        {
            { "alcohol", "Alkohol" },
            { "benzodiazepines", "Benzodiazepiny" },
            { "opioids", "Opioidy" },
            { "stimulants", "Stymulanty" },
            { "cannabis", "Marihuana" },
            { "other", "Inne" },
        };

        private static readonly Dictionary<string, string> EmergencyRelationshipLabels = new(StringComparer.Ordinal)
        {
            { "spouse", "Małżonek/małżonka" },
            { "partner", "Partner/partnerka" },
            { "parent", "Rodzic" },
            { "sibling", "Rodzeństwo" },
            { "child", "Dziecko" },
            { "friend", "Przyjaciel/przyjaciółka" },
            { "guardian", "Opiekun prawny" },
            { "other", "Inny" },
        };

        private static readonly Dictionary<string, string> TraitLabels = new(StringComparer.Ordinal)
        {
            { "loud", "Głośny" },
            { "restless", "Niespokojny" },
            { "quiet", "Cichy" },
            { "withdrawn", "Wycofany" },
            { "charming", "Czarujący" },
            { "cooperative", "Współpracujący" },
        };

        private static readonly Dictionary<string, string> ConditionLabels = new(StringComparer.Ordinal)
        {
            { "hypertension", "Nadciśnienie" },
            { "anxiety_disorder", "Zaburzenia lękowe" },
        };

        private static readonly Dictionary<string, string> AllergyLabels = new(StringComparer.Ordinal)
        {
            { "penicillin", "Penicylina" },
            { "latex", "Lateks" },
        };

        public static string FormatFullName(IdentityDto identity)
        {
            if (identity == null)
                return string.Empty;

            return $"{identity.firstName} {identity.lastName}".Trim();
        }

        public static string FormatSex(string value) => MapOrHumanize(value, SexLabels);

        public static string FormatEmploymentStatus(string value) => MapOrHumanize(value, EmploymentStatusLabels);

        public static string FormatRelationshipStatus(string value) => MapOrHumanize(value, RelationshipStatusLabels);

        public static string FormatHousingStatus(string value) => MapOrHumanize(value, HousingStatusLabels);

        public static string FormatSubstance(string value) => MapOrHumanize(value, SubstanceLabels);

        public static string FormatEmergencyRelationship(string value) => MapOrHumanize(value, EmergencyRelationshipLabels);

        public static string FormatTrait(string value) => MapOrHumanize(value, TraitLabels);

        public static string FormatCondition(string value) => MapOrHumanize(value, ConditionLabels);

        public static string FormatAllergy(string value) => MapOrHumanize(value, AllergyLabels);

        public static string FormatBool(bool value) => value ? "Tak" : "Nie";

        public static string FormatDateOfBirth(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Brak";

            if (DateTime.TryParseExact(
                    value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                return date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            }

            return value;
        }

        public static string FormatOptionalString(string value, string emptyLabel = "Brak")
        {
            return string.IsNullOrWhiteSpace(value) ? emptyLabel : value;
        }

        public static string FormatStringList(string[] values, Func<string, string> formatter, string emptyLabel = "Brak")
        {
            if (values == null || values.Length == 0)
                return emptyLabel;

            var parts = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
                parts[i] = formatter(values[i]);

            return string.Join(", ", parts);
        }

        public static string FormatListRow(PatientDefinitionDto patient)
        {
            if (patient == null)
                return string.Empty;

            var city = patient.administrativeProfile?.city ?? string.Empty;
            var substance = FormatSubstance(patient.addictionHistory?.primaryLongTermSubstance);
            return $"{FormatFullName(patient.identity)}\n{city}\n{substance}";
        }

        public static string FormatEmergencyContact(EmergencyContactDto contact)
        {
            if (contact == null ||
                (string.IsNullOrWhiteSpace(contact.name) &&
                 string.IsNullOrWhiteSpace(contact.relationship) &&
                 string.IsNullOrWhiteSpace(contact.phone)))
            {
                return null;
            }

            return
                $"{contact.name}\nRelacja: {FormatEmergencyRelationship(contact.relationship)}\nTelefon: {contact.phone}";
        }

        public static string FormatDetails(PatientDefinitionDto patient)
        {
            if (patient == null)
                return "Brak danych pacjenta.";

            var builder = new StringBuilder();

            AppendSection(builder, "DANE OSOBOWE");
            AppendLine(builder, "Imię i nazwisko", FormatFullName(patient.identity));
            AppendLine(builder, "Data urodzenia", FormatDateOfBirth(patient.identity?.dateOfBirth));
            AppendLine(builder, "Płeć", FormatSex(patient.identity?.sex));
            AppendLine(builder, "Miasto", FormatOptionalString(patient.administrativeProfile?.city));

            var emergencyContact = FormatEmergencyContact(patient.administrativeProfile?.emergencyContact);
            AppendLine(
                builder,
                "Kontakt alarmowy",
                emergencyContact ?? "Brak kontaktu alarmowego");

            AppendSection(builder, "TŁO");
            AppendLine(builder, "Zawód", FormatOptionalString(patient.background?.occupation));
            AppendLine(builder, "Status zatrudnienia", FormatEmploymentStatus(patient.background?.employmentStatus));
            AppendLine(builder, "Status relacji", FormatRelationshipStatus(patient.background?.relationshipStatus));
            AppendLine(builder, "Sytuacja mieszkaniowa", FormatHousingStatus(patient.background?.housingStatus));
            AppendLine(builder, "Krótki opis", FormatOptionalString(patient.background?.shortBio, string.Empty));

            AppendSection(builder, "HISTORIA UZALEŻNIENIA");
            AppendLine(
                builder,
                "Znane substancje",
                FormatStringList(patient.addictionHistory?.knownSubstances, FormatSubstance));
            AppendLine(
                builder,
                "Główna długoterminowa substancja",
                FormatSubstance(patient.addictionHistory?.primaryLongTermSubstance));
            AppendLine(builder, "Lata używania", patient.addictionHistory?.yearsOfUse.ToString(CultureInfo.InvariantCulture));
            AppendLine(builder, "Liczba poprzednich detoksów", patient.addictionHistory?.previousDetoxCount.ToString(CultureInfo.InvariantCulture));
            AppendLine(builder, "Liczba poprzednich terapii", patient.addictionHistory?.previousTherapyCount.ToString(CultureInfo.InvariantCulture));
            AppendLine(builder, "Najdłuższa abstynencja (dni)", patient.addictionHistory?.longestAbstinenceDays.ToString(CultureInfo.InvariantCulture));
            AppendLine(
                builder,
                "Czy wcześniejsze leczenie ukończone",
                FormatBool(patient.addictionHistory?.previousTreatmentCompleted ?? false));
            AppendLine(builder, "Podsumowanie historii", FormatOptionalString(patient.addictionHistory?.historySummary, string.Empty));

            AppendSection(builder, "DANE MEDYCZNE");
            AppendLine(
                builder,
                "Choroby przewlekłe",
                FormatStringList(patient.medicalBackground?.chronicConditions, FormatCondition));
            AppendLine(
                builder,
                "Alergie",
                FormatStringList(patient.medicalBackground?.allergies, FormatAllergy));

            AppendSection(builder, "PREZENTACJA");
            AppendLine(builder, "Krótki opis", FormatOptionalString(patient.presentation?.shortDescription, string.Empty));
            AppendLine(
                builder,
                "Widoczne cechy",
                FormatStringList(patient.presentation?.visibleTraits, FormatTrait));

            return builder.ToString().TrimEnd();
        }

        private static void AppendSection(StringBuilder builder, string title)
        {
            if (builder.Length > 0)
                builder.AppendLine();

            builder.AppendLine(title);
        }

        private static void AppendLine(StringBuilder builder, string label, string value)
        {
            builder.Append(label);
            builder.Append(": ");
            builder.AppendLine(string.IsNullOrWhiteSpace(value) ? "Brak" : value);
        }

        private static string MapOrHumanize(string value, Dictionary<string, string> labels)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Brak";

            if (labels.TryGetValue(value, out var label))
                return label;

            return HumanizeIdentifier(value);
        }

        private static string HumanizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Brak";

            var parts = value.Split('_');
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0)
                    continue;

                parts[i] = char.ToUpper(parts[i][0], CultureInfo.InvariantCulture) +
                           parts[i][1..].ToLower(CultureInfo.InvariantCulture);
            }

            return string.Join(' ', parts);
        }
    }
}
