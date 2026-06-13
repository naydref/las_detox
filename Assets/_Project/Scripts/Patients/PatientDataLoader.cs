using System.Collections.Generic;
using UnityEngine;

namespace LasDetox.Patients
{
    public class PatientDataLoader : MonoBehaviour
    {
        [SerializeField] private TextAsset _catalogAsset;
        [SerializeField] private TextAsset[] _definitionAssets;

        private readonly List<PatientDefinitionDto> _validPatients = new();
        private readonly List<string> _loadErrors = new();

        public IReadOnlyList<PatientDefinitionDto> ValidPatients => _validPatients;
        public IReadOnlyList<string> LoadErrors => _loadErrors;

        private void Awake()
        {
            LoadPatients();
        }

        private void LoadPatients()
        {
            _validPatients.Clear();
            _loadErrors.Clear();

            if (_catalogAsset == null)
            {
                LogAndRecordError("[PatientData] Catalog TextAsset is not assigned.");
                LogSummary();
                return;
            }

            PatientCatalogDto catalog;
            try
            {
                catalog = JsonUtility.FromJson<PatientCatalogDto>(_catalogAsset.text);
            }
            catch (System.Exception exception)
            {
                LogAndRecordError($"[PatientData] Failed to deserialize catalog: {exception.Message}");
                LogSummary();
                return;
            }

            var catalogErrors = new List<string>();
            if (!PatientDefinitionValidator.TryValidateCatalog(catalog, catalogErrors))
            {
                foreach (var error in catalogErrors)
                    LogAndRecordError($"[PatientData] {error}");

                LogSummary();
                return;
            }

            var definitionsById = BuildDefinitionLookup();

            foreach (var patientId in catalog.patientIds)
            {
                if (string.IsNullOrWhiteSpace(patientId))
                    continue;

                if (!definitionsById.TryGetValue(patientId, out var definition))
                {
                    LogAndRecordError($"[PatientData] {patientId}: No definition file matched this catalog id.");
                    continue;
                }

                var validationErrors = new List<string>();
                if (!PatientDefinitionValidator.TryValidateDefinition(definition, patientId, validationErrors))
                {
                    foreach (var error in validationErrors)
                        LogAndRecordError($"[PatientData] {error}");

                    continue;
                }

                _validPatients.Add(definition);
            }

            LogSummary();
        }

        private Dictionary<string, PatientDefinitionDto> BuildDefinitionLookup()
        {
            var definitionsById = new Dictionary<string, PatientDefinitionDto>();

            if (_definitionAssets == null || _definitionAssets.Length == 0)
            {
                LogAndRecordError("[PatientData] No definition TextAssets are assigned.");
                return definitionsById;
            }

            for (var i = 0; i < _definitionAssets.Length; i++)
            {
                var asset = _definitionAssets[i];
                if (asset == null)
                {
                    LogAndRecordError($"[PatientData] Definition asset at index {i} is not assigned.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(asset.text))
                {
                    LogAndRecordError($"[PatientData] Definition asset '{asset.name}' is empty.");
                    continue;
                }

                PatientDefinitionDto definition;
                try
                {
                    definition = JsonUtility.FromJson<PatientDefinitionDto>(asset.text);
                }
                catch (System.Exception exception)
                {
                    LogAndRecordError($"[PatientData] Definition asset '{asset.name}' failed to deserialize: {exception.Message}");
                    continue;
                }

                if (definition == null)
                {
                    LogAndRecordError($"[PatientData] Definition asset '{asset.name}' deserialized to null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.id))
                {
                    LogAndRecordError($"[PatientData] Definition asset '{asset.name}' has an empty id.");
                    continue;
                }

                if (definitionsById.ContainsKey(definition.id))
                {
                    LogAndRecordError($"[PatientData] {definition.id}: Duplicate definition id; keeping first entry.");
                    continue;
                }

                definitionsById.Add(definition.id, definition);
            }

            return definitionsById;
        }

        private void LogAndRecordError(string message)
        {
            _loadErrors.Add(message);
            Debug.LogError(message, this);
        }

        private void LogSummary()
        {
            Debug.Log(
                $"[PatientData] Loaded {_validPatients.Count} patients, {_loadErrors.Count} errors.",
                this);
        }
    }
}
