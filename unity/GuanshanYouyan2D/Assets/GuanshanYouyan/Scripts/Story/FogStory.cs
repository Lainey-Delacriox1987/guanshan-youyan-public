using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GuanshanYouyan.Story
{
    [Serializable] public class RoomPosition {public string room;public float x;}
    [Serializable] public class StoryProof {public string kind,id,room;public float x;public int choice;public string[] cards;}
    [Serializable] public class ReportAnswer {public int step,claim;public string[] cards;}
    [Serializable] public class FogSave
    {
        public int version=2,schema=3;public string bookId="mist-story-v2-full",room="harbor",pending="",ending="";
        public int line,pendingChoice=-1,reviewStep;public string[] pendingCards=new string[0];
        public float musicGain=1;public bool facingLeft;public string savedAtUtc="";
        public bool openingDrivePending;public int openingDriveShot;
        public List<string> facts=new List<string>(),completed=new List<string>(),visited=new List<string>{"harbor"};
        public List<RoomPosition> positions=new List<RoomPosition>{new RoomPosition{room="harbor",x=4.1f}};
        public List<StoryProof> journal=new List<StoryProof>();public List<ReportAnswer> answers=new List<ReportAnswer>();
    }
    public sealed class FogStory
    {
        [Serializable] class SaveHeader{public int version,schema;public string bookId;}
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void FogSyncSave();
#endif
        public static void FlushStorage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FogSyncSave();
#endif
        }
        public readonly PortBook Book;public FogSave State{get;private set;}=new FogSave();
        public readonly string SavePath;public string LoadNotice{get;private set;}="";public string SaveError{get;private set;}="";bool replaying;string expectedSave;
        readonly Dictionary<string,StoryRoom> rooms;
        readonly Dictionary<string,StoryAction> actions;
        public FogStory(PortBook book,string path=null){Book=book;SavePath=path;ValidateBook();rooms=book.rooms.ToDictionary(r=>r.id);actions=book.actions.ToDictionary(a=>a.id);expectedSave=FogSaveStorage.Read(path);}
        public void ValidateBook()
        {
            if(Book.id!="mist-story-v2-full"||Book.rooms.Length!=8||Book.chapters.Length!=4||Book.reports.Length!=4)throw new InvalidDataException("Incomplete full-story data");
            var ids=Book.actions.Select(a=>a.id).ToArray();if(ids.Distinct().Count()!=ids.Length)throw new InvalidDataException("Duplicate action ID");
            foreach(var a in Book.actions)if(a.lines==null||a.lines.Length==0||a.gives==null||a.required==null||a.choices==null)throw new InvalidDataException("Invalid action "+a.id);
        }
        public StoryRoom Room=>rooms[State.room];
        public float X{get{for(int i=0;i<State.positions.Count;i++)if(State.positions[i].room==State.room)return State.positions[i].x;return Room.spawn;}}
        public float MentorX=>Room.mentorX;public int Act=>Room.act;
        public bool Has(string id)=>State.facts.Contains(id);
        public StoryAction Action(string id)=>actions[id];
        public int UnlockedAct=>Has("act3Complete")?3:Has("act2Complete")?2:Has("act1Complete")?1:0;
        public bool IsUnlocked(string room)=>Book.rooms.Any(r=>r.id==room&&r.act<=UnlockedAct);
        public void Position(float x,bool left)
        {
            RoomPosition p=null;for(int i=0;i<State.positions.Count;i++)if(State.positions[i].room==State.room){p=State.positions[i];break;}
            if(p==null){p=new RoomPosition{room=State.room};State.positions.Add(p);}
            p.x=Mathf.Clamp(x,.7f,23.3f);State.facingLeft=left;
        }
        public string Name(string id)
        {
            var m=Book.materials.FirstOrDefault(v=>v.id==id);if(m!=null)return m.title;
            var h=Book.hints.FirstOrDefault(v=>v.required==id);if(h!=null)return h.type;
            var names=new Dictionary<string,string>{{"letterReceived","来信摘要"},{"letterIdentity","老赵说明来信来源"},{"shopRecordsShared","先向苏禾说明来意"},{"observed","观察待核物件"},{"sourceAsked","问清物件来源"},{"referred","另行提交核验"},{"workerMet","与老赵初次交谈"}};
            return names.TryGetValue(id,out var text)?text:id;
        }
        public string Missing(IEnumerable<string> required)
        {var ids=required.Where(id=>!Has(id)).ToArray();return ids.Length==0?null:"还需要："+string.Join("、",ids.Select(Name))+"。";}
        // Direct scene clicks require the correct room, while walking distance is optional.
        public string At(string room,float x)=>State.room!=room?"请先进入对应的调查现场。":null;
        string ActionGuard(StoryAction a,int choice,IEnumerable<string> cards)
        {
            if(a.room!="")
            {
                if(Act!=a.act)return "这段新问询需要在对应调查阶段的现场进行。";
                var e=a.room=="*"?At(State.room,MentorX):At(a.room,a.x);if(e!=null)return e;
            }
            var missing=Missing(a.required);if(missing!=null)return missing;
            if(a.choices.Length>0&&choice!=a.correct)return a.feedback;
            if(a.evidence!=null&&a.evidence.Length>0)return Evidence(a.evidence,cards);
            return null;
        }
        string Evidence(IEnumerable<string> needed,IEnumerable<string> selected)
        {
            var cards=(selected??new string[0]).Distinct().ToArray();
            if(cards.Any(id=>!Has(id)))return "不能出示尚未取得的材料。";
            if(!new HashSet<string>(needed).SetEquals(cards))return "这组材料还不能对应当前问题。请核对实际的陈述、单号和记录来源。";
            return null;
        }
        public string Begin(string id,int choice=-1,IEnumerable<string> cards=null)
        {
            if(!string.IsNullOrEmpty(State.pending))return "请先完成当前现场对话。";
            var a=Action(id);if(State.completed.Contains(id))return "这段记录已归档，可在手册复看。";
            var error=ActionGuard(a,choice,cards);if(error!=null)return error;
            State.pending=id;State.line=0;State.pendingChoice=choice;State.pendingCards=(cards??new string[0]).Distinct().ToArray();Save();return null;
        }
        public DialogueLine Current=>string.IsNullOrEmpty(State.pending)?null:Action(State.pending).lines[State.line];
        void Fact(string id){if(!Has(id))State.facts.Add(id);}
        void Proof(string kind,string id,int choice=-1,IEnumerable<string> cards=null)
        {if(!replaying)State.journal.Add(new StoryProof{kind=kind,id=id,room=State.room,x=X,choice=choice,cards=(cards??new string[0]).ToArray()});}
        void Arrive(string room)
        {State.room=room;if(!State.visited.Contains(room))State.visited.Add(room);Position(X,State.facingLeft);}
        public void Next()
        {
            if(Current==null)return;var a=Action(State.pending);
            if(++State.line<a.lines.Length){Save();return;}
            CompleteAction(a,State.pendingChoice,State.pendingCards);State.pending="";State.line=0;State.pendingChoice=-1;State.pendingCards=new string[0];Save();
        }
        void CompleteAction(StoryAction a,int choice,IEnumerable<string> cards)
        {
            foreach(var f in a.gives)Fact(f);if(!State.completed.Contains(a.id))State.completed.Add(a.id);
            Proof("action",a.id,choice,cards);
            if(!string.IsNullOrEmpty(a.destination))Arrive(a.destination);
        }
        public string Relate(string id,int claim,IEnumerable<string> cards)
        {
            if(Current!=null)return "请先听完当前对话。";var r=Book.relations.First(v=>v.id==id);
            if(Act!=r.act)return "请在对应阶段向现场许岚说明。";
            var error=At(State.room,MentorX)??Missing(r.required)??Evidence(r.evidence,cards);if(error!=null)return error;
            if(claim!=r.correct)return r.feedback;
            if(!Has(id)){Fact(id);Proof("relation",id,claim,cards);}Save();return null;
        }
        public string Enter(string target,bool map=false)
        {
            if(Current!=null)return "请先完成现场对话。";
            if(!IsUnlocked(target))return "这个区域还没有完成前置调查，请先向许岚核对当前问题。";
            if(map){if(!State.visited.Contains(target))return "地图只能返回亲自到访过的区域。";}
            else
            {
                var exits=Room.spots.Where(s=>s.target==target).ToArray();if(exits.Length==0)return "这里没有直达该区域的通道。";
            }
            Proof(map?"map":"travel",target);Arrive(target);Save();return null;
        }
        public string ReportGuard()
        {
            if(Current!=null)return "请先听完询问回应。";
            if(State.room!="archive")return "请到案卷室向许岚开始最终复盘。";
            return At(State.room,MentorX)??Missing(new[]{"act1Complete","act2Complete","act3Complete","receiptQuestioned","roleQuestioned","scopeReviewed"});
        }
        public string AnswerReport(int step,int claim,IEnumerable<string> cards)
        {
            var e=ReportGuard();if(e!=null)return e;if(step<0||step>=4)return "请选择当前复盘段落。";
            var r=Book.reports[step];if(claim<0||claim>=r.claims.Length)return "请先说明你的判断。";
            e=Missing(r.required)??Evidence(r.evidence,cards);if(e!=null)return e;
            if(step>0&&!State.answers.Any(a=>a.step==step-1))return "先完成上一段复盘。";
            var a=State.answers.FirstOrDefault(v=>v.step==step);if(a==null){a=new ReportAnswer{step=step};State.answers.Add(a);}a.claim=claim;a.cards=cards.Distinct().ToArray();
            State.ending="";State.facts.Remove("reportSubmitted");Proof("answer",step.ToString(),claim,cards);Save();return null;
        }
        public string SubmitReport()
        {
            var e=ReportGuard();if(e!=null)return e;
            if(Enumerable.Range(0,4).Any(i=>!State.answers.Any(a=>a.step==i)))return "四段建议还未完成，请继续与许岚核对。";
            foreach(var a in State.answers){e=Missing(Book.reports[a.step].required)??Evidence(Book.reports[a.step].evidence,a.cards);if(e!=null)return e;}
            var bad=Enumerable.Range(0,4).Where(i=>State.answers.First(a=>a.step==i).claim!=Book.reports[i].correct).DefaultIfEmpty(-1).First();
            Fact("reportSubmitted");State.reviewStep=bad<0?0:bad;State.ending=bad<0?"accepted":"review";Proof("submit","report");Save();
            return bad<0?null:Book.reports[bad].feedback;
        }
        public string EndingText()
        {
            if(State.ending=="review")return "许岚没有签字。她圈出一句话：“这是推测，还是材料已经说明的事实？”\n\n"+Book.reports[State.reviewStep].feedback+"\n\nQJ-017的现有处置没有因这份练习报告改变。已有材料与询问记录保留，回到这段复盘修正。";
            if(State.ending!="accepted")return "调查仍在继续。";
            string eco=Has("referred")?"相关观察与来源问题已另行提交核验；尚无材质、物种或来源的确认结果。":Has("observed")?"生态旁支已记录观察，仍待补充来源核验；未写成已经完成鉴定。":"本次未调查生态旁支，没有作出物种判断或已移交核验的记载。";
            return "QJ-017 的异常与 SD-0615 的历史流转资料分卷整理。陈渡的实际参与、申报差异原因，以及其他人员各自的知情与责任，列入后续核查。\n\n许岚把报告交还：“这一次，你把知道的写清楚，也把不知道的留下了。”\n\n按线索受理流程，工作人员向老赵送达简短回复：“你反映的情况已收到并开展核查。感谢你提供客观记录。”回复不包含未公开的调查材料。\n\n"+eco+"\n\n港口的雾没有全散。你合上案卷，知道下一次应该从哪里看起。";
        }
        public string Hint(int level)
        {
            if(State.ending=="review")return Book.reports[State.reviewStep].feedback;
            if(State.ending=="accepted")return "本次调查建议已整理完毕，可回看材料或重新开始。";
            var h=Book.hints.FirstOrDefault(v=>v.act==Act&&!Has(v.required));
            if(h==null)return "这一阶段已核对，走近出口继续调查，或在手册回看资料。";
            return level==0?h.question:level==1?h.type:h.specific;
        }
        public void NewGame()
        {
            if(SavePath!=null){expectedSave=FogSaveStorage.Read(SavePath);if(expectedSave!=null)FogSaveStorage.Write(SavePath+".previous",expectedSave,force:true);}
            State=new FogSave();Begin("prologue");
        }
        public bool Save(bool overwrite=false)
        {
            if(SavePath==null||replaying)return true;
            try
            {
                string raw=JsonUtility.ToJson(State,true);
                if(!overwrite&&raw==expectedSave&&FogSaveStorage.Read(SavePath)==expectedSave){SaveError="";return true;}
                string previousTime=State.savedAtUtc;State.savedAtUtc=DateTime.UtcNow.ToString("o");raw=JsonUtility.ToJson(State,true);
                try{FogSaveStorage.Write(SavePath,raw,expectedSave,overwrite);}catch{State.savedAtUtc=previousTime;throw;}
                expectedSave=raw;SaveError="";return true;
            }
            catch(Exception e){SaveError=e.Message;return false;}
        }
        public bool Load()
        {
            return LoadSlot(0,false);
        }
        public string SlotPath(int slot)=>slot==-1?SavePath+".previous":slot==0?SavePath:slot>=1&&slot<=3?SavePath+".slot"+slot:throw new ArgumentOutOfRangeException(nameof(slot));
        public FogSaveRecord ReadSlot(int slot)
        {
            var record=new FogSaveRecord();if(SavePath==null)return record;string path=SlotPath(slot);
            foreach(string p in new[]{path,path+".bak"})
            {
                try
                {
                    string json=FogSaveStorage.Read(p);if(json==null)continue;var check=new FogStory(Book);check.Restore(json);
                    record.Json=json;record.Backup=p!=path;record.Summary=check.ProgressSummary();
                    if(record.Backup)record.Summary+="\n将从上一份完整备份恢复";record.Error="";return record;
                }
                catch(Exception){record.Error="存档无法读取，原记录已保留。";record.Summary=record.Error;}
            }
            return record;
        }
        public string ProgressSummary()
        {
            string time=DateTime.TryParse(State.savedAtUtc,null,System.Globalization.DateTimeStyles.RoundtripKind,out var date)?date.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"):"旧存档 · 时间未记录";
            string status=State.ending=="accepted"?"调查建议已提交":State.ending=="review"?"报告待修正":Current!=null?"对话进行中":"调查进行中";
            return "第"+(Act+1)+"幕 · "+Room.title+"\n"+time+" · "+State.facts.Count+"项调查记录 · "+status;
        }
        public bool SaveSlot(int slot)
        {
            if(slot<1||slot>3)throw new ArgumentOutOfRangeException(nameof(slot));
            try{var snapshot=JsonUtility.FromJson<FogSave>(JsonUtility.ToJson(State));snapshot.savedAtUtc=DateTime.UtcNow.ToString("o");FogSaveStorage.Write(SlotPath(slot),JsonUtility.ToJson(snapshot,true),force:true);SaveError="";return true;}
            catch(Exception e){SaveError=e.Message;return false;}
        }
        public bool LoadSlot(int slot,bool commit=true)
        {
            var record=ReadSlot(slot);if(!record.Valid){LoadNotice=record.Error!=""?record.Error:"还没有可读取的存档。";return false;}
            Restore(record.Json);expectedSave=FogSaveStorage.Read(SavePath);SaveError="";
            LoadNotice=record.Backup?"已从上一份完整存档恢复。":"";
            if(commit&&!Save(true)){LoadNotice="进度已读取，但自动存档未写入："+SaveError;}return true;
        }
        public void Restore(string json)
        {
            var header=JsonUtility.FromJson<SaveHeader>(json);if(header==null||header.version!=2||header.schema!=3||header.bookId!=Book.id)throw new InvalidDataException("Unsupported narrative save header");
            var saved=JsonUtility.FromJson<FogSave>(json);
            if(saved==null||saved.version!=2||saved.schema!=3||saved.bookId!=Book.id||saved.journal==null||saved.positions==null)throw new InvalidDataException("Unsupported narrative save");
            var old=State;State=new FogSave();replaying=true;
            try
            {
                foreach(var p in saved.journal)
                {
                    if(!IsUnlocked(p.room)||!float.IsFinite(p.x)||p.x<.7f||p.x>23.3f)throw new InvalidDataException("Invalid event source");
                    State.room=p.room;Position(p.x,false);string e=null;
                    switch(p.kind)
                    {
                        case "action":var a=Action(p.id);e=ActionGuard(a,p.choice,p.cards);if(e==null&&!State.completed.Contains(p.id))CompleteAction(a,p.choice,p.cards);break;
                        case "relation":e=Relate(p.id,p.choice,p.cards);break;
                        case "travel":e=Enter(p.id);break;
                        case "map":e=Enter(p.id,true);break;
                        case "answer":e=AnswerReport(int.Parse(p.id),p.choice,p.cards);break;
                        case "submit":e=SubmitReport();if(State.ending=="review")e=null;break;
                        default:e="Unknown event";break;
                    }
                    if(e!=null)throw new InvalidDataException("Invalid journal event: "+p.id+": "+e);
                }
                State.journal=saved.journal;
                foreach(var p in saved.positions.Where(p=>IsUnlocked(p.room)&&float.IsFinite(p.x))){State.room=p.room;Position(p.x,false);}
                State.room=IsUnlocked(saved.room)&&State.visited.Contains(saved.room)?saved.room:Book.rooms.First(r=>r.act==UnlockedAct).id;
                State.musicGain=float.IsFinite(saved.musicGain)?Mathf.Clamp01(saved.musicGain):1;State.facingLeft=saved.facingLeft;State.savedAtUtc=saved.savedAtUtc??"";
                State.openingDrivePending=saved.openingDrivePending&&State.room=="harbor"&&Has("letterReceived")&&!Has("introDone")&&string.IsNullOrEmpty(saved.pending);
                State.openingDriveShot=State.openingDrivePending?Mathf.Clamp(saved.openingDriveShot,0,2):0;
                if(!string.IsNullOrEmpty(saved.pending))
                {
                    var a=Book.actions.FirstOrDefault(v=>v.id==saved.pending);
                    if(a!=null&&!State.completed.Contains(a.id)&&saved.line>=0&&saved.line<a.lines.Length&&ActionGuard(a,saved.pendingChoice,saved.pendingCards)==null)
                    {State.pending=a.id;State.line=saved.line;State.pendingChoice=saved.pendingChoice;State.pendingCards=saved.pendingCards??new string[0];}
                }
            }
            catch{State=old;throw;}
            finally{replaying=false;}
        }
        public string ImportPort(string json)
        {
            // Copy known first-act work without modifying that prototype's save or any H5 keys.
            var port=new PortStory(JsonUtility.FromJson<PortBook>(JsonUtility.ToJson(Book)));port.Restore(json);
            var legacy=port.State;var old=State;State=new FogSave();replaying=true;
            try
            {
                foreach(var id in legacy.completed)
                {
                    foreach(var r in Book.relations.Where(r=>r.act==0&&legacy.facts.Contains(r.id)&&Missing(r.required)==null))
                    {if(Has(r.id))continue;ImportMove("harbor",5.4f);replaying=false;var e=Relate(r.id,r.correct,r.evidence);replaying=true;if(e!=null)throw new InvalidDataException(e);}
                    var a=Book.actions.FirstOrDefault(v=>v.id==id&&v.act==0);if(a==null)continue;
                    string source=a.room==""||a.room=="*"?"harbor":a.room;ImportMove(source,a.room=="*"?Book.rooms.First(r=>r.id==source).mentorX:a.room==""?4.1f:a.x);
                    var error=ActionGuard(a,a.choices.Length>0?a.correct:-1,new string[0]);if(error!=null)throw new InvalidDataException(error);
                    replaying=false;CompleteAction(a,a.choices.Length>0?a.correct:-1,new string[0]);replaying=true;
                }
                ImportMove(Has("act1Complete")?"warehouse":legacy.room,Has("act1Complete")?3.4f:legacy.room=="harbor"?legacy.harborX:legacy.officeX);State.facingLeft=legacy.facingLeft;
                if(!State.visited.Contains(State.room))State.visited.Add(State.room);State.musicGain=legacy.musicGain;
                if(!Has("letterReceived")){State.room="harbor";replaying=false;Begin("prologue");replaying=true;}
                else if(!string.IsNullOrEmpty(legacy.pending))
                {
                    var pending=Book.actions.FirstOrDefault(v=>v.id==legacy.pending&&v.act==0);
                    if(pending!=null&&ActionGuard(pending,pending.choices.Length>0?pending.correct:-1,new string[0])==null)
                    {State.pending=pending.id;State.line=Mathf.Clamp(legacy.line,0,pending.lines.Length-1);State.pendingChoice=pending.choices.Length>0?pending.correct:-1;}
                }
                replaying=false;Save();return null;
            }
            catch(Exception e){State=old;return "首幕存档无法转换，原存档已保留："+e.Message;}
            finally{replaying=false;}
        }
        void ImportMove(string target,float x)
        {
            while(State.room!=target)
            {
                string next=Room.spots.Any(s=>s.target==target)?target:"harbor";
                var exit=Room.spots.First(s=>s.target==next);Position(exit.x,false);replaying=false;var error=Enter(next);replaying=true;if(error!=null)throw new InvalidDataException(error);
            }
            Position(x,false);
        }
    }
}
