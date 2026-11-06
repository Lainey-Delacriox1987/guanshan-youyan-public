using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GuanshanYouyan.Story
{
    // Browser commits are synchronous; the existing IDBFS file remains a migration fallback.
    public static class FogSaveStorage
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern string FogReadSave(string name);
        [DllImport("__Internal")] static extern int FogWriteSave(string name,string json,string expected,int force);
#endif
        public static string Read(string path)
        {
            if(path==null)return null;
#if UNITY_WEBGL && !UNITY_EDITOR
            string web=FogReadSave(Path.GetFileName(path));if(!string.IsNullOrEmpty(web))return web;
#endif
            return File.Exists(path)?File.ReadAllText(path):null;
        }
        public static bool Exists(string path)=>Read(path)!=null;
        public static void Write(string path,string json,string expected=null,bool force=false)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            int result=FogWriteSave(Path.GetFileName(path),json,expected??"",force?1:0);
            if(result==-1)throw new IOException("另一个游戏窗口已更新此存档。请先读取最新进度，或另存到手动存档位。");
            if(result!=1)throw new IOException("浏览器未能写入存档，请检查网站存储空间和浏览器设置。");
            // Local storage has committed already. Keep a best-effort file mirror for compatibility.
            try{WriteFile(path,json);FogStory.FlushStorage();}catch(Exception e){Debug.LogWarning("存档已保存，文件镜像暂不可用："+e.Message);}
#else
            if(!force&&Read(path)!=expected)throw new IOException("另一个游戏窗口已更新此存档。请先读取最新进度，或另存到手动存档位。");
            WriteFile(path,json);
#endif
        }
        static void WriteFile(string path,string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string tmp=path+".tmp";File.WriteAllText(tmp,json);
            if(File.Exists(path))
            {
                try{File.Replace(tmp,path,path+".bak");}
                catch(NotSupportedException){File.Copy(path,path+".bak",true);File.Delete(path);File.Move(tmp,path);}
            }
            else File.Move(tmp,path);
        }
    }
    public sealed class FogSaveRecord
    {
        public string Json,Error="",Summary="空存档位";public bool Backup;
        public bool Valid=>Json!=null;
    }
}
