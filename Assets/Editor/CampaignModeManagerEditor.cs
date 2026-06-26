
using StorytellersTable.Campaign.Modes;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CampaignModeManager), true)]
public class CampaignModeManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(10);

        CampaignModeManager manager = (CampaignModeManager)target;

        if (GUILayout.Button("Swith to Play mode"))
        {
            manager.SwitchMode(CampaignModeType.Play);
        }
        if (GUILayout.Button("Swith to Map Edit mode"))
        {
            manager.SwitchMode(CampaignModeType.MapEdit);
        }
        if (GUILayout.Button("Swith to Entity Edit mode"))
        {
            manager.SwitchMode(CampaignModeType.EntityEdit);
        }
    }
}
