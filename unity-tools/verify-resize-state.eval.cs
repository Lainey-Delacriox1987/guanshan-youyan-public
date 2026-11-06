var game=UnityEngine.Object.FindFirstObjectByType<GuanshanYouyan.Story.FogGame>();
if(!UnityEditor.EditorApplication.isPlaying||game?.Story==null)throw new System.Exception("Play mode required");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
var type=game.GetType();var original=game.Story;var oldHome=type.GetField("home",flags).GetValue(game);
var fixture=new GuanshanYouyan.Story.FogStory(original.Book);fixture.Restore(System.IO.File.ReadAllText("ImportReports/blank-dismiss-browser-fixture.json"));
type.GetProperty("Story").SetValue(game,fixture);type.GetField("home",flags).SetValue(game,false);
var failures=new System.Collections.Generic.List<string>();int checks=0;
void Call(string name,params object[] args)=>type.GetMethod(name,flags).Invoke(game,args);
void Check(bool value,string name){checks++;if(!value)failures.Add(name);}
UnityEngine.RectTransform Modal()=>(UnityEngine.RectTransform)type.GetField("modal",flags).GetValue(game);
try{
  fixture.State.pending="intro";fixture.State.line=0;
  game.ShowNotebook();Call("Relayout");Check(game.CurrentView=="Notebook","pending dialogue does not replace notebook on relayout");
  Call("ShowMap");Call("Relayout");Check(game.CurrentView=="Map","pending dialogue does not replace map on relayout");
  Call("ClearModal");Call("Relayout");Check(!game.IsModal&&fixture.State.pending=="intro","paused dialogue stays paused on relayout");
  bool callback=false;game.Message("核对提示","保留关闭后的流程",()=>callback=true);var message=Modal();Call("Relayout");
  Check(Modal()==message&&game.CurrentView=="Message","message instance survives relayout");Call("DismissModal");Check(callback,"message callback survives relayout");
  bool cancelled=false;Call("Confirm","覆盖测试","空白处取消",(System.Action)(()=>{}),(System.Action)(()=>cancelled=true));var confirmation=Modal();Call("Relayout");
  Check(Modal()==confirmation&&game.CurrentView=="SaveConfirmation","confirmation is not implicitly accepted or dismissed on resize");Call("DismissModal");Check(cancelled,"confirmation cancel callback retained");
  fixture.State.pending="";game.ShowSaves();Call("Relayout");Check(game.CurrentView=="SaveSlots","save slots remain save slots on relayout");
  fixture.Restore(System.IO.File.ReadAllText("ImportReports/ending-browser-fixture.json"));
  fixture.State.room="harbor";Call("ShowRelation","portMismatch");Call("Relayout");Check(game.CurrentView=="RelationClaims","relation choices survive relayout");
  type.GetField("home",flags).SetValue(game,true);Call("Confirm","封面确认","不返回封面自动继续",(System.Action)(()=>{}),(System.Action)(()=>{}));var homeConfirmation=Modal();Call("Relayout");Check(Modal()==homeConfirmation,"home confirmation does not restart cover on resize");
}
finally{type.GetProperty("Story").SetValue(game,original);type.GetField("home",flags).SetValue(game,oldHome);Call("ClearModal");}
var result=new{success=failures.Count==0,checks,failures,scope="Live isolated modal state and callback audit; player saves untouched"};
System.IO.File.WriteAllText("ImportReports/release-resize-state-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
