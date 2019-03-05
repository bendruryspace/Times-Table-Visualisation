using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TimeTable))]
public class TimesTableEditor : Editor
{
    public override void OnInspectorGUI()
    {        
        TimeTable timesTableScript = (TimeTable)target;
        if (GUILayout.Button("Capture"))
        {
            timesTableScript.Capture();
        }

        DrawDefaultInspector();
    }
}
