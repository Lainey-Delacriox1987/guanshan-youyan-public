using System;
using UnityEngine;

namespace GuanshanYouyan.Story
{
    public sealed partial class FogGame
    {
        void ResumeSavedGame()
        {
            ClearModal();if(home){ShowHome();return;}
            if(Story.State.openingDrivePending)ShowOpeningDrive();else if(Story.Current!=null)ShowDialogue();else if(!string.IsNullOrEmpty(Story.State.ending))ShowEnding();
        }
        void Confirm(string title,string text,Action yes,Action no)
        {
            Open("SaveConfirmation");body=Scroll(modal,new Vector2(.12f,.16f),new Vector2(.88f,.84f));
            Paragraph(body,title,38,gold);Paragraph(body,text);
            Button(body,"确认",yes,"ConfirmSaveButton");Button(body,"取消",no,"CancelSaveButton");AllowBlankDismiss(no);
        }
        void ConfirmNewGame()
        {
            Action start=()=>{try{ResetChat();Story.NewGame();home=false;ApplyRoom(true);ApplyMusic();ShowDialogue();if(Story.SaveError!="")Message("未能保存",Story.SaveError,ShowDialogue);}catch(Exception e){Message("未能开始新故事",e.Message,ShowHome);}};
            if(Story.ReadSlot(0).Valid)Confirm("开始新故事？","将自动保存新故事，当前调查进度会保留为备份。",start,ShowHome);else start();
        }
        void SaveAndHome()
        {
            if(Story.Save())ShowHome();else Message("保存失败",Story.SaveError,ShowNotebook);
        }
        public void ShowSaves()
        {
            if(!home)Story.Save();
            Open("SaveSlots");body=Scroll(modal,new Vector2(.08f,.12f),new Vector2(.92f,.88f));
            Paragraph(body,"存档与读档",40,gold);
            Paragraph(body,"进度保存在当前设备和浏览器。自动存档随调查更新；手动存档位由你决定何时覆盖。",24);
            if(Story.SaveError!="")Paragraph(body,"上次保存未完成："+Story.SaveError,25,gold);
            if(!home)Button(body,"立即保存当前进度",()=>{bool ok=Story.Save();Message(ok?"保存成功":"保存失败",ok?Story.ProgressSummary():Story.SaveError,ShowSaves);},"SaveNowButton");
            SaveSlotCard(0,"自动存档");
            for(int i=1;i<=3;i++)SaveSlotCard(i,"手动存档 "+i);
            if(Story.ReadSlot(-1).Valid)SaveSlotCard(-1,"重新开始前的进度");
            Button(body,home?"返回标题":"返回游戏",ResumeSavedGame,"BackFromSavesButton");AllowBlankDismiss(ResumeSavedGame);
        }
        void SaveSlotCard(int slot,string title)
        {
            var record=Story.ReadSlot(slot);Paragraph(body,title,31,gold);Paragraph(body,record.Summary,24);
            if(slot>0&&!home)
            {
                Action write=()=>{bool ok=Story.SaveSlot(slot);Message(ok?"保存成功":"保存失败",ok?title+"\n"+Story.ReadSlot(slot).Summary:Story.SaveError,ShowSaves);};
                Button(body,record.Valid?"覆盖此存档":"保存到此处",()=>{if(record.Valid||record.Error!="")Confirm("覆盖"+title+"？",record.Summary+"\n\n将用当前调查进度替换。",write,ShowSaves);else write();},"WriteSlot_"+slot);
            }
            if(!record.Valid)return;
            Button(body,"读取"+title,()=>Confirm("读取"+title+"？",record.Summary+"\n\n当前游戏进度将被替换，自动存档随之更新。如需保留当前分支，请先取消并另存到手动存档位。",()=>{
                if(!Story.LoadSlot(slot)){Message("读取失败",Story.LoadNotice,ShowSaves);return;}
                ResetChat();home=false;ApplyRoom(true);ApplyMusic();ResumeSavedGame();
                if(Story.LoadNotice!="")Message("读取存档",Story.LoadNotice,ResumeSavedGame);
            },ShowSaves),"ReadSlot_"+slot);
        }
    }
}
