// ===== SchoolDatabase.cs =====
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SchoolDatabase", menuName = "Combat/School Database")]
public class SchoolDatabase : ScriptableObject
{
    public List<SchoolData> schools = new List<SchoolData>();

    public SchoolData GetSchool(SchoolType type)
    {
        return schools.Find(s => s.schoolType == type);
    }
}
