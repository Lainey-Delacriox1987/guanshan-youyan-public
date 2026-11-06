using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GuanshanYouyan.Story;
using UnityEngine;

namespace GuanshanYouyan.Editor
{
    public static class FogPerformanceProbe
    {
        public static string Run()
        {
            var game=UnityEngine.Object.FindFirstObjectByType<FogGame>();if(game?.Story==null)throw new Exception("Enter Play mode first");
            var original=game.Story;var fixture=new FogStory(original.Book);int checks=0;
            void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
            var nearest=(Func<float,StorySpot>)Delegate.CreateDelegate(typeof(Func<float,StorySpot>),game,typeof(FogGame).GetMethod("Nearest",BindingFlags.NonPublic|BindingFlags.Instance,null,new[]{typeof(float)},null));
            long bytes=0;
            try
            {
                typeof(FogGame).GetProperty("Story").SetValue(game,fixture);
                foreach(var room in fixture.Book.rooms)
                {
                    fixture.State.room=room.id;fixture.Position(room.spawn,false);
                    for(int i=0;i<96;i++){float x=i*.25f;Check(nearest(x)==room.spots.OrderBy(s=>Mathf.Abs(s.x-x)).First(),"nearest matches stable reference in "+room.id);}
                }
                for(int i=0;i<100;i++){fixture.Position(4,false);nearest(fixture.X);}
                long before=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<10000;i++){fixture.Position(4+(i%100)*.1f,i%2==0);var spot=nearest(fixture.X);float floor=fixture.Room.floor;}
                bytes=GC.GetAllocatedBytesForCurrentThread()-before;
                Check(bytes==0,"steady-state position, room and nearest lookup allocate zero bytes");
                Check(game.chineseFont.atlasPopulationMode==TMPro.AtlasPopulationMode.Static,"main font is pre-baked");
                string text=JsonUtility.ToJson(fixture.Book);
                Check(text.Where(c=>!char.IsControl(c)).All(c=>game.chineseFont.HasCharacter(c)),"all story characters are baked");
                foreach(var path in UnityEditor.AssetDatabase.GetDependencies(FogStoryBuilder.ScenePath,true))
                {
                    if(!path.StartsWith("Assets/GuanshanYouyan/"))continue;
                    var importer=UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                    if(importer!=null)Check(!importer.isReadable&&!importer.mipmapEnabled,"no CPU image copy or mip chain: "+path);
                }
                var dependencies=UnityEditor.AssetDatabase.GetDependencies(FogStoryBuilder.ScenePath,true);
                Check(!dependencies.Contains("Assets/GuanshanYouyan/Fonts/NotoSansCJKsc-Regular.otf"),"large source font is excluded from scene dependencies");
            }
            finally{typeof(FogGame).GetProperty("Story").SetValue(game,original);}
            var result=new{success=true,checks,iterations=10000,steadyStateAllocatedBytes=bytes};
            File.WriteAllText("ImportReports/performance-runtime-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }
    }
}
