using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GuanshanYouyan.Story;

namespace GuanshanYouyan.Editor
{
    public static class FogSaveRegression
    {
        static int checks;static List<string> passed;
        static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);passed.Add(name);}
        public static string Run()
        {
            checks=0;passed=new List<string>();
            string folder=Path.GetFullPath("ImportReports/save-regression-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            var book=FogStoryRegression.Book();string path=Path.Combine(folder,"game.json");var s=new FogStory(book,path);
            s.NewGame();s.Next();int line=s.State.line;float x=s.X;
            Check(s.Save()&&s.SaveError=="","auto commits");Check(s.SaveSlot(1),"manual slot 1 saves mid dialogue");
            string original=File.ReadAllText(s.SlotPath(1));s.Next();Check(s.SaveSlot(2),"manual slot 2 saves independently");
            Check(File.ReadAllText(s.SlotPath(1))==original,"slot 2 does not alter slot 1");
            while(s.Current!=null)s.Next();Check(s.SaveSlot(3),"manual slot 3 saves completed dialogue");
            Check(s.LoadSlot(1)&&s.State.line==line&&s.Current!=null&&Mathf.Abs(s.X-x)<.001f,"manual read restores pending dialogue and position");
            Check(!string.IsNullOrEmpty(s.State.savedAtUtc)&&s.ReadSlot(1).Summary.Contains("幕"),"slot has timestamp and progress summary");
            var fresh=new FogStory(book,path);Check(fresh.Load()&&fresh.State.line==line,"new instance resumes chosen slot from auto");
            string prior=File.ReadAllText(path);fresh.NewGame();Check(File.ReadAllText(path+".previous")==prior,"new game preserves previous run");
            Check(File.ReadAllText(s.SlotPath(1))==original,"new game preserves manual slot");
            var stale=new FogStory(book,path);Check(stale.Load(),"second window loads baseline");fresh.Next();string latest=File.ReadAllText(path);
            stale.Next();Check(stale.SaveError!=""&&File.ReadAllText(path)==latest,"stale window cannot overwrite newer auto");
            Check(stale.SaveSlot(3),"stale window can rescue current progress to manual slot");
            s.SaveSlot(1);File.WriteAllText(s.SlotPath(1),"corrupt");Check(s.ReadSlot(1).Valid&&s.ReadSlot(1).Backup,"corrupt slot falls back to validated backup");
            string broken=Path.Combine(folder,"broken.json");File.WriteAllText(broken,"invalid");var bad=new FogStory(book,broken);Check(!bad.Load()&&File.ReadAllText(broken)=="invalid","invalid save rejected and preserved");
            string blocker=Path.Combine(folder,"blocked");File.WriteAllText(blocker,"file");var denied=new FogStory(book,Path.Combine(blocker,"game.json"));denied.NewGame();Check(denied.SaveError!="","storage failure is visible instead of success");
            string report="ImportReports/save-regression.json";File.WriteAllText(report,Newtonsoft.Json.JsonConvert.SerializeObject(new{success=true,checks,passed},Newtonsoft.Json.Formatting.Indented));return Path.GetFullPath(report);
        }
    }
}
