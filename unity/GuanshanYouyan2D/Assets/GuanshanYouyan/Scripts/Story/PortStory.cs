using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GuanshanYouyan.Story
{
    [Serializable] public class DialogueLine { public string speaker, text; }
    [Serializable] public class StoryAction
    {
        public string id, title, room; public float x;
        public string[] required, gives, choices; public int correct;
        public DialogueLine[] lines;
        public int act, actor; public string mode, inspect, feedback, destination; public string[] evidence;
    }
    [Serializable] public class MaterialRecord { public string id, title, kind, text; public int act; }
    [Serializable] public class StoryRelation
    {
        public string id, title, success, feedback; public string[] required, evidence, claims; public int correct;
        public int act, actor; public string room, target;
    }
    [Serializable] public class StorySpot {public string id,title,kind,target;public float x;public int actor;}
    [Serializable] public class StoryActor {public int actor;public float x;}
    [Serializable] public class StoryRoom { public string id, title, artKey; public float floor, spawn,mentorX,strip;public int act;public StorySpot[] spots;public StoryActor[] actors; }
    [Serializable] public class StoryHint { public string required, question, type, specific; public int act; }
    [Serializable] public class StoryChapter {public string title,time,question;}
    [Serializable] public class StoryReport {public string id,title,question,feedback;public string[] required,evidence,claims;public int correct;}
    [Serializable] public class StoryKnowledge {public string title,unlock,text,source,url;}
    [Serializable] public class PortBook
    {
        public int version, chapter; public string title, subtitle;
        public StoryRoom[] rooms; public StoryAction[] actions; public StoryRelation[] relations;
        public MaterialRecord[] materials; public StoryHint[] hints;
        public string id;public StoryChapter[] chapters;public StoryReport[] reports;
        public StoryKnowledge[] knowledge;
    }
    [Serializable] public class PortSave
    {
        public int version = 2; public string room = "harbor"; public float harborX = 4.1f, officeX = 3.4f;
        public bool facingLeft; public List<string> facts = new List<string>(), completed = new List<string>();
        public string pending = ""; public int line; public float musicGain = 1;
    }
    // Every path into an investigation, including UI calls and restored dialogue, uses these guards.
    public sealed class PortStory
    {
        public readonly PortBook Book; public PortSave State { get; private set; }
        public string LoadNotice { get; private set; } = "";
        public string SavePath { get; private set; }
        public PortStory(PortBook book, string path = null)
        { Book = book; SavePath = path; State = new PortSave(); }
        public bool Has(string id) => State.facts.Contains(id);
        public StoryAction Action(string id) => Book.actions.First(a => a.id == id);
        public float X => State.room == "harbor" ? State.harborX : State.officeX;
        public void Position(float x, bool left)
        { x = Mathf.Clamp(x, .7f, 23.3f); if (State.room == "harbor") State.harborX=x; else State.officeX=x; State.facingLeft=left; }
        public string Missing(string[] required)
        {
            var missing = required.Where(id => !Has(id)).ToArray();
            if (missing.Length == 0) return null;
            return "还需要：" + string.Join("、",missing.Select(Name)) + "。";
        }
        public string Name(string id)
        {
            var material=Book.materials.FirstOrDefault(m=>m.id==id); if(material!=null)return material.title;
            var hint=Book.hints.FirstOrDefault(h=>h.required==id); if(hint!=null)return hint.type;
            return id=="letterReceived"?"听完来信摘要":id;
        }
        public string At(string room,float x)
        { return State.room!=room || Mathf.Abs(X-x)>1.25f ? "请先走近现场的调查对象。" : null; }
        public float MentorX => State.room=="harbor"?5.4f:12.7f;
        public string Begin(string id, int choice=-1)
        {
            if (!string.IsNullOrEmpty(State.pending)) return "请先完成或收起当前对话。";
            var a=Action(id);
            if(State.completed.Contains(id))return "这段记录已归档，可在调查手册复看。";
            if(a.room=="*") { var err=At(State.room,MentorX); if(err!=null)return err; }
            else if(a.room!="") {var err=At(a.room,a.x);if(err!=null)return err;}
            var missing=Missing(a.required); if(missing!=null)return missing;
            if(a.choices.Length>0 && choice!=a.correct)return "先核对票号和计量口径，再计算两份净重的差值。";
            State.pending=id;State.line=0;Save();return null;
        }
        public DialogueLine Current => string.IsNullOrEmpty(State.pending)?null:Action(State.pending).lines[State.line];
        public void Next()
        {
            if(string.IsNullOrEmpty(State.pending))return;
            var a=Action(State.pending);
            if(++State.line>=a.lines.Length)
            {
                foreach(var id in a.gives)if(!Has(id))State.facts.Add(id);
                if(!State.completed.Contains(a.id))State.completed.Add(a.id);
                State.pending="";State.line=0;
            }
            Save();
        }
        public string Relate(string id,int claim,IEnumerable<string> selected)
        {
            if(!string.IsNullOrEmpty(State.pending))return "先完成当前对话。";
            var err=At(State.room,MentorX);if(err!=null)return err;
            var r=Book.relations.First(a=>a.id==id);var missing=Missing(r.required);if(missing!=null)return missing;
            var cards=selected.Distinct().ToArray();
            if(cards.Any(s=>!Has(s)))return "不能出示尚未取得的资料。";
            if(!new HashSet<string>(r.evidence).SetEquals(cards))return "这些材料还不能对应这个问题。请回看来源、票号和字段，再选能直接支持判断的记录。";
            if(claim!=r.correct)return r.feedback;
            if(!Has(id))State.facts.Add(id);Save();return null;
        }
        public string Enter(string room)
        {
            if(!string.IsNullOrEmpty(State.pending))return "请先收起现场对话。";
            if(room!="harbor"&&room!="inspection")return "本片段可探索港口与查验办公室。";
            var err=At(State.room,State.room=="harbor"?21.5f:2.4f);if(err!=null)return err;
            State.room=room;Save();return null;
        }
        public string Hint(int level)
        {var h=Book.hints.FirstOrDefault(a=>!Has(a.required));return h==null?"第一幕已完成。记录和来信仍可在手册中复看。":level==0?h.question:level==1?h.type:h.specific;}
        public void NewGame()
        {
            if(SavePath!=null&&File.Exists(SavePath))File.Copy(SavePath,SavePath+".previous",true);
            State=new PortSave();Begin("prologue");
        }
        public void Save()
        {
            if(SavePath==null)return;
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            var tmp=SavePath+".tmp";File.WriteAllText(tmp,JsonUtility.ToJson(State,true));
            if(File.Exists(SavePath))File.Replace(tmp,SavePath,SavePath+".bak");else File.Move(tmp,SavePath);
        }
        public bool Load()
        {
            if(SavePath==null)return false;
            foreach(var file in new[]{SavePath,SavePath+".bak"})
            {
                if(!File.Exists(file))continue;
                try { Restore(File.ReadAllText(file)); if(file.EndsWith(".bak"))LoadNotice="已从上一份完整存档恢复。";return true; }
                catch(Exception){LoadNotice="存档无法读取，已保留原文件。";}
            }
            return false;
        }
        public void Restore(string json)
        {
            var s=JsonUtility.FromJson<PortSave>(json);
            if(s==null||s.version!=2||s.facts==null||s.completed==null)throw new InvalidDataException("Unsupported story save");
            // Rebuild from completed source events, not untrusted flags from the old narrative.
            var facts=new HashSet<string>();var complete=new List<string>();
            foreach(var id in s.completed.Distinct())
            {
                var a=Book.actions.FirstOrDefault(x=>x.id==id);
                // Relations can occur between source actions. Only rebuild them from valid saved prerequisites.
                foreach(var r in Book.relations)if(s.facts.Contains(r.id)&&r.required.All(facts.Contains))facts.Add(r.id);
                if(a!=null&&a.required.All(facts.Contains)){complete.Add(id);foreach(var g in a.gives)facts.Add(g);}
            }
            foreach(var r in Book.relations)if(s.facts.Contains(r.id)&&r.required.All(facts.Contains))facts.Add(r.id);
            s.completed=complete;s.facts=facts.ToList();s.room=s.room=="inspection"?"inspection":"harbor";
            s.harborX=Mathf.Clamp(s.harborX,.7f,23.3f);s.officeX=Mathf.Clamp(s.officeX,.7f,23.3f);s.musicGain=Mathf.Clamp01(s.musicGain);
            var pending=Book.actions.FirstOrDefault(a=>a.id==s.pending);
            if(pending==null||complete.Contains(s.pending)||!pending.required.All(facts.Contains)||s.line<0||s.line>=pending.lines.Length)
            {s.pending="";s.line=0;}
            else if(pending.room!="" && (pending.room=="*" ? Mathf.Abs((s.room=="harbor"?s.harborX:s.officeX)-(s.room=="harbor"?5.4f:12.7f))>1.25f : pending.room!=s.room||Mathf.Abs((s.room=="harbor"?s.harborX:s.officeX)-pending.x)>1.25f))
            {s.pending="";s.line=0;}
            State=s;
        }
    }
}
