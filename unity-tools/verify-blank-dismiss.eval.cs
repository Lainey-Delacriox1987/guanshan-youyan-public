var game=UnityEngine.Object.FindFirstObjectByType<GuanshanYouyan.Story.FogGame>();
if(!UnityEditor.EditorApplication.isPlaying||game?.Story==null)throw new System.Exception("Play mode required");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
var type=game.GetType();var original=game.Story;var oldHome=type.GetField("home",flags).GetValue(game);
var fixture=new GuanshanYouyan.Story.FogStory(original.Book);
fixture.Restore(System.IO.File.ReadAllText("ImportReports/ending-browser-fixture.json"));
type.GetProperty("Story").SetValue(game,fixture);type.GetField("home",flags).SetValue(game,false);
var failures=new System.Collections.Generic.List<string>();var screens=new System.Collections.Generic.List<string>();int checks=0;
void Call(string name,params object[] args)=>type.GetMethod(name,flags).Invoke(game,args);
void Check(bool pass,string name){checks++;if(!pass)failures.Add(name);}
UnityEngine.RectTransform Modal()=>(UnityEngine.RectTransform)type.GetField("modal",flags).GetValue(game);
void Outside(){
    var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=new UnityEngine.Vector2(UnityEngine.Screen.width*.02f,UnityEngine.Screen.height*.5f),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
    var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);
    var target=hits.Count==0?null:UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject);
    Check(target==Modal().gameObject,game.CurrentView+" outside routes to backdrop");
    if(target!=null)UnityEngine.EventSystems.ExecuteEvents.Execute(target,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
}
var cases=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,System.Action>>();
void Add(string name,System.Action show)=>cases.Add(new System.Collections.Generic.KeyValuePair<string,System.Action>(name,show));
Add("initial material",()=>Call("ReviewAction","prologue"));Add("cargo",()=>Call("ShowCargo"));Add("weight",()=>Call("ShowWeight"));
foreach(var entry in fixture.Book.actions){var a=entry;Add("review:"+a.id,()=>Call("ReviewAction",a.id));if(!string.IsNullOrEmpty(a.inspect))Add("inspect:"+a.id,()=>Call("ShowInspection",a.id));}
foreach(var entry in fixture.Book.knowledge){var card=entry;Add("knowledge:"+card.title,()=>Call("ShowKnowledge",card));}
Add("notebook",game.ShowNotebook);Add("map",()=>Call("ShowMap"));Add("mentor",()=>Call("ShowMentor"));Add("inquiry",()=>Call("ShowInquiries"));
Add("npc",()=>Call("ShowNpc",6));Add("profile",()=>Call("ShowProfile",6));Add("people",()=>Call("ShowFinalIntro"));
for(int i=0;i<4;i++){int step=i;Add("report:"+step,()=>Call("ShowReportStep",step));}
Add("message",()=>game.Message("初始材料","已收集的材料原文"));Add("speaker message",()=>game.Message("许岚","资料核对提示"));
Add("save confirmation",()=>Call("Confirm","确认测试","空白处等同取消",(System.Action)(()=>{}),(System.Action)(()=>{})));
System.Collections.IEnumerator Run(){
  string error=null;
  try{
    UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1600,900,"Blank dismiss QA");
  }catch(System.Exception ex){error=ex.ToString();}
  yield return null;yield return null;
  foreach(var item in cases){
    try{item.Value();}catch(System.Exception ex){error=ex.ToString();break;}
    yield return null;yield return null;UnityEngine.Canvas.ForceUpdateCanvases();
    var modal=Modal();Check(modal!=null,item.Key+" opens");if(modal==null)continue;
    Check(System.Linq.Enumerable.All(modal.GetComponentsInChildren<TMPro.TMP_Text>(),t=>!t.text.Contains("收起")),item.Key+" no close labels");
    Check(modal.Find("Close")==null&&modal.Find("SaveClose")==null,item.Key+" no close controls");
    var scroll=modal.GetComponentInChildren<UnityEngine.UI.ScrollRect>();var panel=scroll!=null?(UnityEngine.RectTransform)scroll.transform:modal.Find("SinglePagePanel") as UnityEngine.RectTransform;
    if(panel!=null){var shield=panel.GetComponent<UnityEngine.UI.Button>();Check(shield!=null,item.Key+" content shield");if(shield!=null){shield.onClick.Invoke();Check(Modal()==modal,item.Key+" inside blank stays");}}
    Outside();Check(!game.IsModal,item.Key+" outside dismisses");Check((int)type.GetField("ignoreScenePointerFrame",flags).GetValue(game)==UnityEngine.Time.frameCount,item.Key+" blocks scene click through");screens.Add(item.Key);
  }
  bool callback=false;game.Message("后续流程","关闭后回到话题",()=>callback=true);yield return null;yield return null;
  UnityEngine.Canvas.ForceUpdateCanvases();Outside();Check(callback,"message callback preserved");
  fixture.State.pending="intro";fixture.State.line=0;Call("ShowDialogue");yield return null;yield return null;
  try{
    UnityEngine.Canvas.ForceUpdateCanvases();Outside();Check(!game.IsModal&&fixture.State.pending=="intro"&&fixture.State.line==0,"dialogue pauses without advancing");
    Call("ShowDialogue");Check(game.CurrentView=="Dialogue","paused dialogue reopens");
    type.GetProperty("Story").SetValue(game,original);type.GetField("home",flags).SetValue(game,oldHome);Call("ClearModal");
  }catch(System.Exception ex){error=ex.ToString();type.GetProperty("Story").SetValue(game,original);type.GetField("home",flags).SetValue(game,oldHome);Call("ClearModal");}
  System.IO.File.WriteAllText("ImportReports/blank-dismiss-editor-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null&&failures.Count==0,checks,failures,error,screens,scope="Isolated fixture, live UI raycasts and callbacks; player saves untouched"},Newtonsoft.Json.Formatting.Indented));
}
game.StartCoroutine(Run());return "Blank dismiss screen audit started";
