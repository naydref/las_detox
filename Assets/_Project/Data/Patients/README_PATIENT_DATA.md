# Patient Definition Data (v0.1)

Static JSON definitions for the detox prototype. Runtime state, admissions, and UI are out of scope for this data slice.

## Folder layout

```text
Assets/_Project/Data/Patients/
├── Catalogs/detox_patients_catalog.json
├── Definitions/detox_patient_NNN.json
└── README_PATIENT_DATA.md
```

## Adding a patient

1. Create `Definitions/detox_patient_NNN.json` using the full field set from an existing definition.
2. Add the `id` to `Catalogs/detox_patients_catalog.json` (`patientIds` array — order is display/load order).
3. Assign the new JSON as a `TextAsset` on `PatientDataLoader` → `_definitionAssets` in `DetoxPrototype.unity` (`Systems`).

Required ID format: `detox_patient_NNN` (three digits), e.g. `detox_patient_004`.

Catalog rules: `schemaVersion` must be `1`, `patientIds` non-empty, no duplicate IDs.

## Technical enums (English)

**Substances** (`knownSubstances`, `primaryLongTermSubstance`): `alcohol`, `benzodiazepines`, `opioids`, `stimulants`, `cannabis`, `other`. There is no `mixed` substance — multi-substance history uses tags (e.g. `mixed_substance_history`).

**Sex:** `male`, `female`

**employmentStatus:** `employed`, `unemployed`, `self_employed`, `retired`, `student`, `disability`, `other`

**relationshipStatus:** `single`, `married`, `partnered`, `divorced`, `widowed`, `separated`, `other`

**housingStatus:** `stable`, `temporary`, `homeless`, `institutional`, `unknown`

**emergencyContact.relationship:** `spouse`, `partner`, `parent`, `sibling`, `child`, `friend`, `guardian`, `other`

**tags:** snake_case technical IDs, at least one per patient.

**visibleTraits / hiddenTraits:** English technical IDs, not Polish descriptions.

Narrative fields (`shortBio`, `historySummary`, `shortDescription`) are written in Polish.

## JsonUtility limitation (v0.1)

The loader uses `UnityEngine.JsonUtility`. The validator checks required sections and value ranges after deserialization, but **cannot** tell a missing numeric/boolean key from an explicit `0` / `0.0` / `false`. Keep JSON files complete; per-key validation may come later (JSON Schema or another parser).

## Out of scope

Patient runtime state, admissions, referrals, funding, save/load, and roster UI (Prototype 0.3B).
