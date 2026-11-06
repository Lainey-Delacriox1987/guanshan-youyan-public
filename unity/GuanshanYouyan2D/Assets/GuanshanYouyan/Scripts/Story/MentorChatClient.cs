using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GuanshanYouyan.Story
{
    public sealed class MentorChatClient : MonoBehaviour
    {
        [Serializable] class Health {public bool ready;}
        [Serializable] class Payload {public string question;public int chapter;public string[] materials,events;}
        [Serializable] public class Reply {public string answer,error,provider,model;public bool guarded;public int latencyMs;}
        public string endpoint="http://127.0.0.1:39571";public int timeoutSeconds=20;
        public bool Ready{get;private set;}public bool Sending{get;private set;}public Action<bool> AvailabilityChanged;
        UnityWebRequest active;Coroutine pending;
        readonly string[] sharedEvents={"letterReceived","inspectionReady","workerMet","letterIdentity","shopRecordsShared","denialRecorded","observed","sourceAsked","referred","receiptQuestioned","roleQuestioned","scopeReviewed"};
        IEnumerator Start()
        {
            while(true)
            {
                bool ready=false;using(var request=UnityWebRequest.Get(endpoint+"/health"))
                {request.timeout=3;yield return request.SendWebRequest();if(request.result==UnityWebRequest.Result.Success){try{ready=JsonUtility.FromJson<Health>(request.downloadHandler.text).ready;}catch(Exception){ready=false;}}}
                if(Ready!=ready){Ready=ready;AvailabilityChanged?.Invoke(Ready);}yield return new WaitForSecondsRealtime(12);
            }
        }
        public void Ask(FogStory story,string question,Action<Reply> complete)
        {Cancel();pending=StartCoroutine(Send(story,question,complete));}
        public string BuildRequestJson(FogStory story,string question)
        {
            // A visited room can belong to an earlier act; it does not undo the investigation.
            var payload=new Payload{question=question,chapter=story.UnlockedAct,materials=story.Book.materials.Where(m=>story.Has(m.id)).Select(m=>m.id).ToArray(),events=sharedEvents.Where(story.Has).ToArray()};
            return JsonUtility.ToJson(payload);
        }
        IEnumerator Send(FogStory story,string question,Action<Reply> complete)
        {
            Sending=true;
            active=new UnityWebRequest(endpoint+"/mentor","POST");
            var body=Encoding.UTF8.GetBytes(BuildRequestJson(story,question));
            active.uploadHandler=new UploadHandlerRaw(body);active.downloadHandler=new DownloadHandlerBuffer();active.SetRequestHeader("Content-Type","application/json");active.timeout=Mathf.Max(1,timeoutSeconds);
            yield return active.SendWebRequest();Reply reply=null;
            try{reply=JsonUtility.FromJson<Reply>(active.downloadHandler.text);}catch(Exception){reply=null;}
            if(reply==null)reply=new Reply{error="暂时联系不上，可以重试或继续核对现场。"};
            if(active.result!=UnityWebRequest.Result.Success&&string.IsNullOrEmpty(reply.error))reply.error="回答没有完成，可以重试或收起后继续主线。";
            active.Dispose();active=null;Sending=false;pending=null;complete(reply);
        }
        public void Cancel()
        {if(pending!=null){StopCoroutine(pending);pending=null;}if(active!=null){active.Abort();active.Dispose();active=null;}Sending=false;}
        void OnDestroy()=>Cancel();
    }
}
