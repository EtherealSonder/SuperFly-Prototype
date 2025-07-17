using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildingSpawner))]
public class BuildingSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BuildingSpawner spawner = (BuildingSpawner)target;

        if (GUILayout.Button("Generate Building"))
        {
            spawner.Generate();
        }
    }
}
