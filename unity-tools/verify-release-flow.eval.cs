var game=UnityEngine.Object.FindFirstObjectByType<GuanshanYouyan.Story.FogGame>();
if(!UnityEditor.EditorApplication.isPlaying||game?.Story==null)throw new System.Exception("Play mode required");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
var type=game.GetType();var original=game.Story;var oldHome=type.GetField("home",flags).GetValue(game);
var fixture=GuanshanYouyan.Editor.FogStoryRegression.Fresh();type.GetProperty("Story").SetValue(game,fixture);type.GetField("home",flags).SetValue(game,false);
var checks=new System.Collections.Generic.List<string>();string phase="start";
void Call(string name,params object[] args)=>type.GetMethod(name,flags).Invoke(game,args);
void Check(bool ok,string name){if(!ok)throw new System.Exception(phase+": "+name);checks.Add(name);}
System.Collections.IEnumerator Click(string name){
    var go=UnityEngine.GameObject.Find(name);Check(go!=null,"control exists: "+name);
    var sr=go.GetComponentInParent<UnityEngine.UI.ScrollRect>();
    if(sr!=null){sr.StopMovement();UnityEngine.Canvas.ForceUpdateCanvases();var local=sr.viewport.InverseTransformPoint(go.transform.position);sr.content.anchoredPosition+=new UnityEngine.Vector2(0,sr.viewport.rect.center.y-local.y);}
    yield return null;yield return null;UnityEngine.Canvas.ForceUpdateCanvases();
    var rt=(UnityEngine.RectTransform)go.transform;var point=UnityEngine.RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));
    var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
    var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);
    var handler=hits.Count==0?null:UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject);
    Check(handler==go,"raycast reaches "+name);Check(go.GetComponent<UnityEngine.UI.Button>().interactable,"enabled: "+name);
    UnityEngine.EventSystems.ExecuteEvents.Execute(go,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);yield return null;
}
System.Collections.IEnumerator Blank(){
    yield return null;yield return null;UnityEngine.Canvas.ForceUpdateCanvases();
    var modal=(UnityEngine.RectTransform)type.GetField("modal",flags).GetValue(game);Check(modal!=null,"blank-dismiss modal exists");
    var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=new UnityEngine.Vector2(UnityEngine.Screen.width*.02f,UnityEngine.Screen.height*.5f),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
    var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);
    var handler=hits.Count==0?null:UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject);Check(handler==modal.gameObject,"blank routes to backdrop");
    UnityEngine.EventSystems.ExecuteEvents.Execute(handler,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);yield return null;
}
System.Collections.IEnumerator Lines(){int limit=20;while(fixture.Current!=null&&limit-->0)yield return Click("NextButton");Check(fixture.Current==null,"dialogue completes");}
System.Collections.IEnumerator Spot(string id){phase=fixture.State.room+"/"+id;game.Interact(id);yield return null;}
System.Collections.IEnumerator Message(){Check(game.CurrentView=="Message","feedback appears");yield return Blank();}
System.Collections.IEnumerator Inspect(string id){var a=fixture.Action(id);yield return Spot(id);Check(game.CurrentView=="Inspection_"+id,"inspection opens "+id);yield return Click(a.choices.Length>0?"ActionChoice_"+a.correct:"Record_"+id);yield return Lines();}
System.Collections.IEnumerator Relation(string id){
    var r=System.Linq.Enumerable.First(fixture.Book.relations,v=>v.id==id);yield return Click("RelationTopic_"+id);yield return Click("Claim"+r.correct);
    foreach(var card in r.evidence)yield return Click("Evidence_"+card);yield return Click("PresentButton");yield return Message();Check(fixture.Has(id),"relationship recorded "+id);
}
System.Collections.IEnumerator Travel(string id,string target){yield return Spot(id);yield return Lines();Check(fixture.State.room==target,"chapter arrives "+target);yield return Message();}
System.Collections.IEnumerator Door(string id,string target){yield return Spot(id);Check(fixture.State.room==target,"door reaches "+target);}
System.Collections.IEnumerator Flow(){
    UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1600,900,"Release flow QA");Call("ApplyRoom",true);Call("ClearModal");yield return null;yield return null;
    yield return Spot("mentor");yield return Click("FixedQuestionTopic");yield return Message();Check(game.CurrentView=="MentorTopics","fixed-topic callback returns");
    yield return Click("IntroTopic");yield return Lines();yield return Spot("cargo");yield return Message();Check(!fixture.Has("cargo"),"early cargo blocked");
    yield return Door("door","inspection");yield return Spot("weight");yield return Click("WeightChoice1");yield return Message();Check(!fixture.Has("weight"),"wrong weight awards nothing");yield return Click("WeightChoice0");yield return Lines();
    yield return Spot("mentor");yield return Click("ReportWeightTopic");yield return Lines();yield return Door("door","harbor");yield return Spot("cargo");yield return Click("RecordCargoButton");yield return Lines();
    yield return Spot("handover");yield return Lines();yield return Spot("seal");yield return Lines();yield return Spot("mentor");yield return Relation("portMismatch");yield return Relation("portDestination");yield return Blank();
    yield return Travel("departure","warehouse");yield return Spot("worker");yield return Lines();yield return Inspect("parcels");yield return Door("door","dispatch");yield return Inspect("camera");yield return Inspect("dispatch");
    yield return Spot("mentor");yield return Relation("historicalRoute");yield return Blank();yield return Door("door","warehouse");yield return Spot("worker");yield return Click("WorkerRecordTopic");yield return Lines();Check(fixture.Has("letterIdentity"),"letter author identified");yield return Travel("advance","town");
    yield return Spot("denial");yield return Lines();yield return Door("door","records");yield return Spot("shopShare");yield return Lines();yield return Inspect("account");yield return Door("door","town");yield return Inspect("shelf");yield return Spot("courier");yield return Lines();
    yield return Door("door","records");yield return Inspect("wildlife");yield return Spot("shopShare");yield return Click("EcologySourceTopic");yield return Lines();yield return Spot("mentor");yield return Click("EcologyReferTopic");yield return Lines();yield return Door("door","town");
    yield return Spot("mentor");yield return Relation("receiptContradiction");yield return Relation("participantScope");yield return Blank();yield return Travel("advance","interview");
    foreach(var id in new[]{"receiptInquiry","roleInquiry"}){yield return Spot("suspect");yield return Click("InquiryTopic_"+id);foreach(var card in fixture.Action(id).evidence)yield return Click("Evidence_"+card);yield return Click("PresentButton");yield return Lines();}
    yield return Spot("mentor");yield return Click("ScopeTopic");yield return Click("ActionChoice_2");yield return Lines();yield return Door("door","archive");yield return Spot("mentor");yield return Click("FinalReviewTopic");yield return Click("ReviewPerson_6");yield return Message();yield return Click("ReviewPerson_9");
    for(int step=0;step<4;step++){phase="report/"+step;var r=fixture.Book.reports[step];Check(game.CurrentView=="ReportStep_"+step,"sequential report "+step);yield return Click("ReportClaim_"+(step==1?(r.correct+1)%3:r.correct));foreach(var card in r.evidence)yield return Click("Evidence_"+card);yield return Click("PresentButton");if(step<3)yield return Message();}
    Check(game.CurrentView=="EndingReview"&&fixture.State.reviewStep==1,"wrong report requests the exact correction");int count=fixture.State.facts.Count;yield return Click("RetryReportButton");var retry=fixture.Book.reports[1];yield return Click("ReportClaim_"+retry.correct);foreach(var card in retry.evidence)yield return Click("Evidence_"+card);yield return Click("PresentButton");
    Check(game.CurrentView=="EndingAccepted"&&fixture.State.ending=="accepted","corrected report reaches final poster");Check(fixture.State.facts.Count>=count&&fixture.Has("referred"),"correction retains evidence and ecology branch");
    var restored=new GuanshanYouyan.Story.FogStory(fixture.Book);restored.Restore(UnityEngine.JsonUtility.ToJson(fixture.State));Check(restored.State.ending=="accepted"&&restored.State.answers.Count==4,"full UI journey restores from journal");
}
System.Collections.IEnumerator Runner(){
    string error=null;var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(Flow());
    while(stack.Count>0){var current=stack.Peek();bool next=false;try{next=current.MoveNext();}catch(System.Exception ex){error=ex.ToString();}if(error!=null)break;if(!next){stack.Pop();continue;}if(current.Current is System.Collections.IEnumerator nested)stack.Push(nested);else yield return current.Current;}
    var ending=fixture.State.ending;type.GetProperty("Story").SetValue(game,original);type.GetField("home",flags).SetValue(game,oldHome);Call("ClearModal");Call("ApplyRoom",true);
    System.IO.File.WriteAllText("ImportReports/release-full-ui-flow.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null,error,phase,ending,checks=checks.Count,passed=checks,scope="Four-act live UI callbacks and EventSystem raycasts, all optional observations, wrong-answer correction; scene interactions invoked through game controller; not a physical-device walkthrough"},Newtonsoft.Json.Formatting.Indented));
}
game.StartCoroutine(Runner());return "Full release UI flow started with isolated progress";
