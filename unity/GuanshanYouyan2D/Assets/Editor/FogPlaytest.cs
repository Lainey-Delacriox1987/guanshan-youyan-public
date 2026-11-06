using UnityEditor;
using UnityEngine;
using GuanshanYouyan.Story;
namespace GuanshanYouyan.Editor
{
    public static class FogPlaytest
    {
        public static string Start(bool ecology=false,bool receiptFirst=false)
        {
            if(!EditorApplication.isPlaying)throw new System.Exception("Enter Play mode");
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            foreach(var window in Resources.FindObjectsOfTypeAll<EditorWindow>())if(window.titleContent.text=="Game")window.Focus();
            var probe=new GameObject("Full Story UI Verification").AddComponent<FogWalkthrough>();
            probe.Begin(ecology,receiptFirst,"full-playtest-"+(Screen.height>Screen.width?"portrait":"desktop")+".json");return "Started full UI and virtual-touch walkthrough";
        }
    }
}
