using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CityGenerator))]
public class CityGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityGenerator generator = (CityGenerator)target;

        GUILayout.Space(10);
        GUI.backgroundColor = Color.cyan;

        if (GUILayout.Button("🛠️ Generate City"))
        {
            generator.Generate();
        }

        GUI.backgroundColor = Color.white;
    }
}
