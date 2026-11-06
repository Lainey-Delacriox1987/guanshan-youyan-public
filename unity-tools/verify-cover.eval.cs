var game=UnityEngine.Object.FindFirstObjectByType<GuanshanYouyan.Story.FogGame>();
if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode required");
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var storyField=game.GetType().GetField("<Story>k__BackingField",flags);
var remaining=game.GetType().GetField("coverRemaining",flags);
var records=new System.Collections.Generic.List<object>();
var book=game.Story.Book;
storyField.SetValue(game,new GuanshanYouyan.Story.FogStory(book));
game.ShowHome();UnityEngine.Time.timeScale=1;
UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
double start=UnityEngine.Time.timeAsDouble;int stage=0;int ticks=game.TickCount;
string saved="",path=System.IO.Path.GetFullPath("ImportReports/cover-probe-"+System.Guid.NewGuid()+".json");
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=>{
    if(game==null||!UnityEditor.EditorApplication.isPlaying){UnityEditor.EditorApplication.update-=tick;return;}
    double elapsed=UnityEngine.Time.timeAsDouble-start;
    if(stage==0&&elapsed>=1){
        var image=UnityEngine.GameObject.Find("CoverArtwork").GetComponent<UnityEngine.UI.Image>();
        var rt=(UnityEngine.RectTransform)image.transform;
        records.Add(new{check="cover-visible",pass=game.CurrentView=="Cover"&&image.sprite==game.coverArt&&UnityEngine.GameObject.Find("NewGameButton")==null,view=game.CurrentView,width=rt.rect.width,height=rt.rect.height});
        UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("ImportReports/cover-startup.png"));
        float before=(float)remaining.GetValue(game);game.ShowHome();
        records.Add(new{check="relayout-keeps-timer",pass=System.Math.Abs(before-(float)remaining.GetValue(game))<.01});stage=1;
    }
    if(stage==1&&elapsed>=3.3){
        bool legacy=System.IO.File.Exists(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath,"guanshan-mist-story-v2-port.json"));
        records.Add(new{check="auto-entry-without-menu",pass=game.CurrentView==(legacy?"Dialogue":"Prologue")&&game.TickCount>ticks,view=game.CurrentView,elapsed,legacy});
        var story=new GuanshanYouyan.Story.FogStory(book,path);story.NewGame();story.Next();saved=System.IO.File.ReadAllText(path);
        storyField.SetValue(game,story);game.ShowHome();start=UnityEngine.Time.timeAsDouble;stage=2;return;
    }
    if(stage==2&&elapsed>=3.3){
        records.Add(new{check="saved-player-auto-resumes",pass=game.CurrentView=="Letter"&&game.Story.State.line==1&&System.IO.File.ReadAllText(path)==saved,view=game.CurrentView,line=game.Story.State.line});
        UnityEditor.EditorApplication.update-=tick;
        System.IO.File.WriteAllText("ImportReports/cover-startup-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));
    }
};
UnityEditor.EditorApplication.update+=tick;
return "Cover verification running against isolated progress";
