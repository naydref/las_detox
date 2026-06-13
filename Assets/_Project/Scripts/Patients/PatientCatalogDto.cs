using System;

namespace LasDetox.Patients
{
    [Serializable]
    public class PatientCatalogDto
    {
        public int schemaVersion;
        public string[] patientIds;
    }
}
