using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GuanshanYouyan.Story;

namespace GuanshanYouyan.Editor
{
    public static class PortStoryRegression
    {
        static PortBook book;static List<string> passed;
        static void Check(bool ok,string name){if(!ok)throw new Exception("FAILED: "+name);passed.Add(name);}
        static void Finish(PortStory s){while(s.Current!=null)s.Next();}
        static void Go(PortStory s,string room,float x)
        {if(s.State.room!=room){s.Position(s.State.room=="harbor"?21.5f:2.4f,false);Check(s.Enter(room)==null,"door freely enters "+room);}s.Position(x,false);}
        static void Take(PortStory s,string id)
        {
            var a=s.Action(id);if(a.room=="*")s.Position(s.MentorX,false);else if(a.room!="")Go(s,a.room,a.x);
            Check(s.Begin(id,a.choices.Length==0?-1:a.correct)==null,"begin "+id);Checkpoint(s,"before "+id);Finish(s);Checkpoint(s,"after "+id);
        }
        static void Checkpoint(PortStory s,string name)
        {
            var other=new PortStory(book);other.Restore(JsonUtility.ToJson(s.State));
            Check(new HashSet<string>(s.State.facts).SetEquals(other.State.facts)&&other.State.pending==s.State.pending&&other.State.line==s.State.line,name+" reload preserves facts and dialogue");
        }
        static PortStory Fresh(){var s=new PortStory(book);s.NewGame();Finish(s);return s;}
        static void Complete(PortStory s)
        {
            if(!s.Has("weight"))Take(s,"weight");Take(s,"reportWeight");Take(s,"cargo");
            if(!s.Has("handover"))Take(s,"handover");
            s.Position(s.MentorX,false);
            Check(s.Relate("portMismatch",1,new[]{"weight","cargo"})==null,"in-scene anomaly presentation");
            Checkpoint(s,"anomaly relationship");
            Check(s.Relate("portDestination",0,new[]{"handover"})==null,"in-scene destination presentation");
            Checkpoint(s,"destination relationship");Take(s,"departure");
            Check(s.Has("act1Complete"),"first act completes without board or profiles");
        }
        [MenuItem("关山有眼/运行第一幕逻辑验收")]
        public static string Run()
        {
            book=JsonUtility.FromJson<PortBook>(File.ReadAllText("Assets/GuanshanYouyan/Data/PortAct1V2.json"));passed=new List<string>();
            var s=Fresh();Go(s,"harbor",15.1f);Check(s.Begin("cargo")!=null&&!s.Has("cargo"),"cargo blocked before weight and report");
            Take(s,"weight");Go(s,"harbor",15.1f);Check(s.Begin("cargo")!=null,"weight alone cannot unlock cargo");
            Complete(s);Check(!s.Has("seal"),"seal optional: no observation fabricated");
            s=Fresh();Take(s,"handover");Complete(s);Check(s.Has("act1Complete"),"guard-first route completes");
            s=Fresh();Take(s,"seal");Complete(s);Check(s.Has("seal"),"seal-first route completes and observation retained");
            s=Fresh();Check(s.Begin("departure")!=null,"cannot depart early");Go(s,"inspection",8.1f);
            Check(s.Begin("weight",1)!=null&&!s.Has("weight"),"wrong weight comparison has no reward");
            Check(s.Begin("weight",0)==null,"weight retry accepted");s.Next();Checkpoint(s,"mid-dialogue");
            Check(!s.Has("weight"),"dialogue not yet heard cannot award record");Finish(s);int count=s.State.facts.Count;
            Check(s.Begin("weight",0)!=null&&s.State.facts.Count==count,"repeated actions idempotent");
            Go(s,"harbor",5.4f);Check(s.Relate("portMismatch",1,new[]{"weight","cargo"})!=null,"unowned evidence rejected");
            Take(s,"reportWeight");Take(s,"cargo");s.Position(s.MentorX,false);
            Check(s.Relate("portMismatch",2,new[]{"weight","cargo"})!=null&&!s.Has("portMismatch"),"unsupported assertion rejected");
            Check(s.Relate("portMismatch",1,new[]{"weight"})!=null,"insufficient evidence rejected");
            Check(s.Relate("portMismatch",1,new[]{"weight","cargo"})==null,"failed presentation recoverable");
            Take(s,"handover");s.Position(20,false);Check(s.Relate("portDestination",0,new[]{"handover"})!=null,"remote relation bypass rejected");
            Go(s,"inspection",8.1f);Check(s.Begin("handover")!=null,"remote material event rejected");
            Check(s.Enter("town")!=null,"unimplemented regions cannot be entered");
            s=Fresh();var corrupt=JsonUtility.FromJson<PortSave>(JsonUtility.ToJson(s.State));corrupt.facts.AddRange(new[]{"cargo","portMismatch","act1Complete"});
            s.Restore(JsonUtility.ToJson(corrupt));Check(!s.Has("cargo")&&!s.Has("portMismatch")&&!s.Has("act1Complete"),"restore removes orphaned progress flags");
            bool rejected=false;try{s.Restore("{\"version\":1}");}catch(InvalidDataException){rejected=true;}Check(rejected,"old narrative save rejected without migration");
            string path=Path.GetFullPath("ImportReports/port-test-save.json");s=new PortStory(book,path);s.NewGame();Finish(s);s.Save();
            File.WriteAllText(path,"not valid json");var reloaded=new PortStory(book,path);Check(reloaded.Load()&&reloaded.LoadNotice.Contains("恢复"),"corrupt primary uses backup");
            foreach(var suffix in new[]{"",".bak",".tmp",".previous"})if(File.Exists(path+suffix))File.Delete(path+suffix);
            Directory.CreateDirectory("ImportReports");var report=new {success=true,date=DateTime.UtcNow.ToString("o"),checks=passed.Count,passed=passed.ToArray(),scope="First act offline logic; no physical mobile or user comprehension claim"};
            var json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);File.WriteAllText("ImportReports/port-story-regression.json",json);return json;
        }
    }
}
