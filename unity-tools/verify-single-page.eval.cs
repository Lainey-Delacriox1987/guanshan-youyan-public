var game=UnityEngine.Object.FindFirstObjectByType<GuanshanYouyan.Story.FogGame>();
if(!UnityEditor.EditorApplication.isPlaying||game?.Story==null)throw new System.Exception("Play mode required");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var type=game.GetType();var original=game.Story;var oldHome=type.GetField("home",flags).GetValue(game);
var fixture=new GuanshanYouyan.Story.FogStory(original.Book);type.GetProperty("Story").SetValue(game,fixture);type.GetField("home",flags).SetValue(game,false);
var records=new System.Collections.Generic.List<object>();var failures=new System.Collections.Generic.List<string>();int checks=0;
void Call(string name,params object[] args)=>type.GetMethod(name,flags).Invoke(game,args);
void Check(bool pass,string name){checks++;if(!pass)failures.Add(name);}
var screens=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string,System.Action>>();
void Add(string name,System.Action action)=>screens.Add(new System.Collections.Generic.KeyValuePair<string,System.Action>(name,action));
Add("cargo",()=>Call("ShowCargo"));Add("weight",()=>Call("ShowWeight"));
foreach(var entry in fixture.Book.actions){var action=entry;if(!string.IsNullOrEmpty(action.inspect)||action.choices.Length>0&&action.id!="weight")Add("inspect:"+action.id,()=>Call("ShowInspection",action.id));}
foreach(var entry in fixture.Book.actions){var action=entry;Add("review:"+action.id,()=>Call("ReviewAction",action.id));}
foreach(var entry in fixture.Book.knowledge){var card=entry;Add("knowledge:"+card.title,()=>Call("ShowKnowledge",card));}
Add("map",()=>Call("ShowMap"));
System.Collections.IEnumerator Verify(){
    foreach(var size in new[]{new UnityEngine.Vector2Int(1600,900),new UnityEngine.Vector2Int(1280,800),new UnityEngine.Vector2Int(1000,700)}){
        UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)size.x,(uint)size.y,"Single-page QA");yield return null;yield return null;
        foreach(var screen in screens){
            screen.Value();yield return null;yield return null;UnityEngine.Canvas.ForceUpdateCanvases();
            var page=UnityEngine.GameObject.Find("SinglePagePanel");Check(page!=null,screen.Key+" panel");if(page==null)continue;
            Check(page.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().Length==0,screen.Key+" no scrolling");
            foreach(var text in page.GetComponentsInChildren<TMPro.TMP_Text>()){
                text.ForceMeshUpdate();Check(!text.isTextOverflowing&&!text.isTextTruncated,size+" "+screen.Key+" text "+text.name);
            }
            foreach(var button in page.GetComponentsInChildren<UnityEngine.UI.Button>()){
                var corners=new UnityEngine.Vector3[4];((UnityEngine.RectTransform)button.transform).GetWorldCorners(corners);
                Check(corners.All(c=>c.x>=-1&&c.y>=-1&&c.x<=UnityEngine.Screen.width+1&&c.y<=UnityEngine.Screen.height+1),size+" "+screen.Key+" visible "+button.name);
                Check(((UnityEngine.RectTransform)button.transform).rect.height>=50,screen.Key+" usable height "+button.name);
            }
            records.Add(new{size=size.ToString(),screen=screen.Key,texts=page.GetComponentsInChildren<TMPro.TMP_Text>().Length,buttons=page.GetComponentsInChildren<UnityEngine.UI.Button>().Length});
            if(screen.Key=="cargo"){UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("ImportReports/single-page-cargo-"+size.x+"x"+size.y+".png"));yield return null;}
        }
    }
}
System.Collections.IEnumerator Run(){
    var iterator=Verify();string error=null;
    while(true){bool next=false;try{next=iterator.MoveNext();}catch(System.Exception ex){error=ex.ToString();}if(error!=null||!next)break;yield return iterator.Current;}
    type.GetProperty("Story").SetValue(game,original);type.GetField("home",flags).SetValue(game,oldHome);Call("ClearModal");
    System.IO.File.WriteAllText("ImportReports/single-page-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{success=error==null&&failures.Count==0,checks,failures,error,screens=records},Newtonsoft.Json.Formatting.Indented));
}
game.StartCoroutine(Run());return "Isolated inspection/document screen layout audit started";
