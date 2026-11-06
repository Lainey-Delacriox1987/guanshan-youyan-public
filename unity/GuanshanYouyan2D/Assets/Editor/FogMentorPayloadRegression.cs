using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using GuanshanYouyan.Story;

namespace GuanshanYouyan.Editor
{
    public static class FogMentorPayloadRegression
    {
        public static string Run()
        {
            FogStoryRegression.Run(); // Initializes the existing proven causal walkthrough helpers.
            var checks=new List<string>();var snapshots=new List<object>();
            Action<bool,string> check=(yes,name)=>{if(!yes)throw new Exception(name);checks.Add(name);};
            var holder=new GameObject("Mentor serialization verification"){hideFlags=HideFlags.HideAndDontSave};
            try
            {
                var client=holder.AddComponent<MentorChatClient>();var story=FogStoryRegression.Fresh();
                Action<int,string> capture=(act,required)=>{
                    check(story.Enter("harbor",true)==null,"visited harbor reachable at act "+act);
                    check(story.Act==0&&story.UnlockedAct==act,"room age differs from current progress "+act);
                    var before=JsonUtility.ToJson(story.State);var payload=JObject.Parse(client.BuildRequestJson(story,"这些已取得的资料怎样核对？"));
                    check((int)payload["chapter"]==act,"payload keeps investigation stage on old-room revisit "+act);
                    var materials=payload["materials"].Values<string>().ToArray();var events=payload["events"].Values<string>().ToArray();
                    check(materials.All(story.Has)&&events.All(story.Has),"payload contains owned information only "+act);
                    check(!materials.Contains("wildlife"),"unobserved optional item remains unknown "+act);
                    if(required!="")check(materials.Contains(required),"owned later material retained: "+required);
                    check(payload.Properties().Select(p=>p.Name).OrderBy(n=>n).SequenceEqual(new[]{"chapter","events","materials","question"}),"no whole-save/position/private-note fields "+act);
                    check(before==JsonUtility.ToJson(story.State),"serializing context does not mutate progress "+act);
                    snapshots.Add(new{investigationAct=act,room=story.State.room,payload});
                };
                capture(0,"");FogStoryRegression.Act1(story);capture(1,"handover");
                check(story.Enter("warehouse",true)==null,"return to warehouse");FogStoryRegression.Act2(story);capture(2,"dispatch");
                check(story.Enter("town",true)==null,"return to town");FogStoryRegression.Act3(story);capture(3,"account");
                var report=new{success=true,checks,snapshots,scope="Actual Unity request serialization after four-act causal progression and old-room revisits. No model service or generated answer.",verifiedAt=DateTime.UtcNow.ToString("o")};
                var json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);File.WriteAllText("ImportReports/mentor-payload-regression.json",json);return json;
            }
            finally{UnityEngine.Object.DestroyImmediate(holder);}
        }
    }
}
