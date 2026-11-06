using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GuanshanYouyan.Story;

namespace GuanshanYouyan.Editor
{
    public static class FogStoryRegression
    {
        static PortBook book;static List<object> results;static int assertions;
        static void Assert(bool yes,string name){assertions++;if(!yes)throw new Exception(name);}
        static void Case(string name,Action run)
        {int start=assertions;try{run();results.Add(new{name,success=true,assertions=assertions-start,error=""});}catch(Exception e){results.Add(new{name,success=false,assertions=assertions-start,error=e.ToString()});}}
        static void Drain(FogStory s){while(s.Current!=null)s.Next();}
        public static PortBook Book()=>JsonUtility.FromJson<PortBook>(File.ReadAllText("Assets/GuanshanYouyan/Data/StoryV2.json"));
        public static FogStory Fresh(){var s=new FogStory(book??Book());s.NewGame();Drain(s);return s;}
        public static void Go(FogStory s,string target,float x)
        {
            var queue=new Queue<string>();var parent=new Dictionary<string,string>();queue.Enqueue(s.State.room);parent[s.State.room]=null;
            while(queue.Count>0&&!parent.ContainsKey(target))
            {var id=queue.Dequeue();foreach(var next in s.Book.rooms.First(r=>r.id==id).spots.Where(p=>!string.IsNullOrEmpty(p.target)&&s.IsUnlocked(p.target)).Select(p=>p.target))if(!parent.ContainsKey(next)){parent[next]=id;queue.Enqueue(next);}}
            Assert(parent.ContainsKey(target),"reachable room "+target);
            var path=new List<string>();for(string at=target;parent[at]!=null;at=parent[at])path.Insert(0,at);
            foreach(var next in path){var exit=s.Room.spots.First(p=>p.target==next);s.Position(exit.x,false);Assert(s.Enter(next)==null,"door transition "+next);}
            s.Position(x,false);
        }
        static void Checkpoint(FogStory s,string at)
        {
            var other=new FogStory(s.Book);other.Restore(JsonUtility.ToJson(s.State));
            Assert(new HashSet<string>(s.State.facts).SetEquals(other.State.facts),at+" facts reload");
            Assert(s.State.pending==other.State.pending&&s.State.line==other.State.line,at+" pending dialogue reload");
            Assert(s.State.room==other.State.room&&Mathf.Abs(s.X-other.X)<.001f,at+" room position reload");
            Assert(s.State.ending==other.State.ending&&s.State.answers.Count==other.State.answers.Count,at+" report reload");
        }
        public static void Take(FogStory s,string id)
        {
            var a=s.Action(id);if(a.room=="*")s.Position(s.MentorX,false);else if(a.room!="")Go(s,a.room,a.x);
            Assert(s.Begin(id,a.choices.Length>0?a.correct:-1,a.evidence)==null,"begin "+id);Checkpoint(s,"before "+id);
            s.Next();if(s.Current!=null){Assert(a.gives.All(f=>!s.Has(f)),"unheard response no reward "+id);Checkpoint(s,"mid "+id);}
            Drain(s);Checkpoint(s,"after "+id);
        }
        public static void Relate(FogStory s,string id)
        {var r=s.Book.relations.First(v=>v.id==id);if(s.Act!=r.act)Go(s,s.Book.rooms.First(v=>v.act==r.act).id,3.4f);s.Position(s.MentorX,false);Assert(s.Relate(id,r.correct,r.evidence)==null,"relate "+id);Checkpoint(s,"relationship "+id);}
        public static void Act1(FogStory s,int order=0)
        {
            if(order==1)Take(s,"handover");if(order==2)Take(s,"seal");
            Take(s,"weight");Take(s,"reportWeight");Take(s,"cargo");if(!s.Has("handover"))Take(s,"handover");Relate(s,"portMismatch");Relate(s,"portDestination");Take(s,"departure");
        }
        public static void Act2(FogStory s)
        {Take(s,"workerIntro");Take(s,"parcels");Take(s,"camera");Take(s,"dispatch");Relate(s,"historicalRoute");Take(s,"worker");Take(s,"warehouseDeparture");}
        public static void Act3(FogStory s,bool phoneFirst=true,int ecology=0)
        {
            if(phoneFirst)Take(s,"denial");else Take(s,"courier");Take(s,"shopShare");Take(s,"account");Take(s,"shelf");if(phoneFirst)Take(s,"courier");else Take(s,"denial");
            if(ecology>0)Take(s,"wildlife");if(ecology>1)Take(s,"wildlifeSource");if(ecology>2)Take(s,"wildlifeRefer");
            Relate(s,"receiptContradiction");Relate(s,"participantScope");Take(s,"townDeparture");
        }
        public static void Inquiries(FogStory s){Take(s,"receiptInquiry");Take(s,"roleInquiry");Take(s,"scopeReview");Go(s,"archive",19.1f);}
        static void Report(FogStory s,int wrong=-1)
        {
            for(int i=0;i<4;i++){var r=s.Book.reports[i];Assert(s.AnswerReport(i,i==wrong?(r.correct+1)%3:r.correct,r.evidence)==null,"report dialogue "+i);Checkpoint(s,"report answer "+i);}
            int count=s.State.facts.Count;var error=s.SubmitReport();
            if(wrong>=0)
            {
                Assert(error==s.Book.reports[wrong].feedback&&s.State.ending=="review"&&s.State.reviewStep==wrong,"specific review feedback "+wrong);Checkpoint(s,"review ending");
                Assert(s.State.facts.Count>=count,"review retains materials");var r=s.Book.reports[wrong];Assert(s.AnswerReport(wrong,r.correct,r.evidence)==null,"correct returned segment");
                Assert(s.SubmitReport()==null,"retry report accepted");
            }
            else Assert(error==null,"report accepted");
            Assert(s.State.ending=="accepted"&&s.Has("reportSubmitted"),"good ending reached");Checkpoint(s,"accepted ending");
        }
        [MenuItem("关山有眼/验收完整故事逻辑")]
        public static string Run()
        {
            book=Book();results=new List<object>();assertions=0;
            Case("new mainline / no ecology / all checkpoint reloads",()=>{var s=Fresh();Act1(s);Act2(s);Act3(s);Inquiries(s);Report(s);Assert(!s.Has("wildlife")&&s.EndingText().Contains("未调查生态"),"no optional branch fabricated");Assert(s.Has("letterIdentity")&&s.EndingText().Contains("老赵"),"letter origin and reply recovered");});
            Case("guard-first route / receipt-first town",()=>{var s=Fresh();Act1(s,1);Act2(s);Act3(s,false);Inquiries(s);Report(s);});
            Case("seal-first route / observed ecology only",()=>{var s=Fresh();Act1(s,2);Act2(s);Act3(s,true,1);Inquiries(s);Report(s);Assert(s.Has("seal")&&s.Has("observed")&&!s.Has("referred")&&s.EndingText().Contains("待补充来源"),"observed ending accurate");});
            Case("ecology source asked but not referred",()=>{var s=Fresh();Act1(s);Act2(s);Act3(s,true,2);Inquiries(s);Report(s);Assert(s.Has("sourceAsked")&&!s.Has("referred")&&!s.EndingText().Contains("已另行提交核验"),"source-only ending accurate");});
            Case("full optional ecology / independent from mainline",()=>{var s=Fresh();Act1(s);Act2(s);Act3(s,false,3);Inquiries(s);Report(s);Assert(s.Has("referred")&&s.EndingText().Contains("已另行提交核验"),"referred ending accurate");});
            for(int i=0;i<4;i++){int wrong=i;Case("returned report segment "+i+" preserves and retries",()=>{var s=Fresh();Act1(s);Act2(s);Act3(s);Inquiries(s);Report(s,wrong);});}
            Case("cargo prerequisite / choice retry / idempotency",()=>{var s=Fresh();Go(s,"harbor",15.1f);Assert(s.Begin("cargo")!=null&&!s.Has("cargo"),"cargo blocks before weight");Take(s,"weight");Go(s,"harbor",15.1f);Assert(s.Begin("cargo")!=null,"weight alone insufficient");Take(s,"reportWeight");Take(s,"cargo");int facts=s.State.facts.Count;Assert(s.Begin("cargo")!=null&&s.State.facts.Count==facts,"repeat does not award");});
            Case("shop record access / phone and receipt independence",()=>{var s=Fresh();Act1(s);Act2(s);Go(s,"records",11.8f);Assert(s.Begin("account")!=null&&!s.Has("account"),"account blocks before consent");Take(s,"courier");Assert(!s.Has("denialRecorded")&&!s.Has("receiptContradiction"),"receipt alone not contradiction");Take(s,"denial");Take(s,"shopShare");Take(s,"account");Relate(s,"receiptContradiction");});
            Case("inquiry presentations / report cannot skip responses",()=>{var s=Fresh();Act1(s);Act2(s);Act3(s);Go(s,"archive",19.1f);Assert(s.SubmitReport()!=null&&s.State.ending=="","no ending before inquiries");Go(s,"interview",12.8f);Assert(s.Begin("roleInquiry",-1,new[]{"courier"})!=null&&!s.Has("roleQuestioned"),"three-source inquiry rejects one record");Assert(s.Begin("receiptInquiry",-1,new[]{"S1","courier"})==null,"valid inquiry begins");s.Next();Assert(!s.Has("receiptQuestioned"),"must hear response");Drain(s);Take(s,"roleInquiry");Go(s,"archive",19.1f);Assert(s.SubmitReport()!=null&&s.State.ending=="","scope review mandatory");});
            Case("map, remote event, unknown evidence, profile invariants",()=>{var s=Fresh();Assert(s.Enter("town",true)!=null,"locked map rejected");Assert(s.Enter("inspection",true)!=null,"unvisited fast travel rejected");Assert(s.Begin("account")!=null,"remote record event rejected");Go(s,"harbor",5.4f);Assert(s.Relate("portMismatch",1,new[]{"weight","cargo"})!=null,"unowned cards rejected");int before=s.State.facts.Count;_ = s.Hint(0);_ = s.Hint(1);_ = s.Hint(2);Assert(s.State.facts.Count==before,"hints do not grant progress");});
            Case("direct scene interactions need correct room but no walking proximity",()=>{var s=Fresh();Go(s,"harbor",.8f);Assert(s.Begin("intro")==null,"distant same-room conversation allowed");Drain(s);Assert(s.X<1,"direct conversation does not teleport player");Assert(s.Enter("inspection")==null,"visible room exit does not require approaching");Assert(s.Begin("weight",0)==null,"same-room inspection allowed from entrance");Drain(s);Assert(s.Begin("cargo")!=null,"another room remains inaccessible");Checkpoint(s,"direct interaction journal reload");});
            Case("save proof validation, corrupt fallback, old versions",()=>{var s=Fresh();var raw=JsonUtility.FromJson<FogSave>(JsonUtility.ToJson(s.State));raw.facts.AddRange(new[]{"roleQuestioned","act3Complete","reportSubmitted"});s.Restore(JsonUtility.ToJson(raw));Assert(!s.Has("roleQuestioned")&&!s.Has("act3Complete"),"orphan flags ignored");bool bad=false;try{s.Restore("{\"version\":2,\"facts\":[\"cargo\"]}");}catch(InvalidDataException){bad=true;}Assert(bad,"old prototype not implicitly treated as full game");string p=Path.GetFullPath("ImportReports/full-regression-save.json");var disk=new FogStory(book,p);disk.NewGame();Drain(disk);disk.Save();File.WriteAllText(p,"bad json");var other=new FogStory(book,p);Assert(other.Load()&&other.LoadNotice.Contains("恢复"),"backup recovers corrupt primary");foreach(var suffix in new[]{"",".bak",".previous",".tmp"})if(File.Exists(p+suffix))File.Delete(p+suffix);});
            Case("first-act prototype import preserves source and does not fabricate act2",()=>{var old=new PortSave{facts=new List<string>{"letterReceived","weight","inspectionReady","cargo","handover","portMismatch","portDestination","act1Complete"},completed=new List<string>{"prologue","weight","reportWeight","cargo","handover","departure"}};string json=JsonUtility.ToJson(old);var s=new FogStory(book);Assert(s.ImportPort(json)==null,"prototype imports");Assert(s.Has("act1Complete")&&!s.Has("act2Complete")&&!s.Has("worker"),"no later story fabricated");Checkpoint(s,"import complete");Assert(json==JsonUtility.ToJson(old),"original prototype object unchanged");});
            Case("partial prototype import resumes dialogue",()=>{var old=new PortSave{room="inspection",officeX=12.7f,facts=new List<string>{"letterReceived","weight"},completed=new List<string>{"prologue","weight"},pending="reportWeight",line=1};var s=new FogStory(book);Assert(s.ImportPort(JsonUtility.ToJson(old))==null,"partial prototype imports");Assert(s.State.pending=="reportWeight"&&s.State.line==1&&!s.Has("inspectionReady"),"partial response not awarded");Checkpoint(s,"import pending");});
            var serialized=Newtonsoft.Json.JsonConvert.SerializeObject(results);bool success=!serialized.Contains("\"success\":false");
            var report=new{success,assertions,cases=results.Count,results,scope="Full four-act logic and persistence; UI/platform evidence recorded separately",verifiedAt=DateTime.UtcNow.ToString("o")};
            var output=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);File.WriteAllText("ImportReports/full-story-regression.json",output);return output;
        }
    }
}
