mergeInto(LibraryManager.library, {
  FogCoverFrameReady: function () {
    window.__fogCoverPresented=true;
    if(window.FogStartupCover)window.FogStartupCover.presented();
  },
  FogSetPrologueVisible: function (visible) {
    document.documentElement.dataset.fogIntro=visible?"1":"0";
  },
  FogReadSave__deps: ["$lengthBytesUTF8", "$stringToUTF8"],
  FogReadSave: function (name) {
    var value="";try{value=localStorage.getItem("guanshan-mist-save-v1:"+UTF8ToString(name))||"";}catch(error){}
    var length=lengthBytesUTF8(value)+1, pointer=_malloc(length);stringToUTF8(value,pointer,length);return pointer;
  },
  FogWriteSave: function (name,json,expected,force) {
    try{
      var key="guanshan-mist-save-v1:"+UTF8ToString(name),value=UTF8ToString(json),previous=localStorage.getItem(key);
      if(!force&&previous!==null&&previous!==UTF8ToString(expected))return -1;
      // Each setItem commits before Unity reports success, including on page hide.
      if(previous!==null&&previous!==value)localStorage.setItem(key+".bak",previous);
      localStorage.setItem(key,value);return 1;
    }catch(error){return -2;}
  },
  FogInstallSaveLifecycle: function () {
    if(Module.__fogSaveLifecycle)return;
    var save=function(){if(window.fogGameInstance&&window.fogGameInstance.Module===Module)window.fogGameInstance.SendMessage("雾港来信 · 完整故事","SaveForPageHide");};
    var hide=function(){if(document.hidden)save();};
    Module.__fogSaveLifecycle=true;window.addEventListener("pagehide",save);document.addEventListener("visibilitychange",hide);
    Module.deinitializers.push(function(){window.removeEventListener("pagehide",save);document.removeEventListener("visibilitychange",hide);});
  },
  FogReadMusicPreference: function () {
    try {var value=localStorage.getItem("guanshan-mist-music-gain-v1");if(value===null)return -1;var gain=Number(value);return Number.isFinite(gain)?Math.max(0,Math.min(1,gain)):-1;}catch(error){return -1;}
  },
  FogWriteMusicPreference: function (gain) {
    try {localStorage.setItem("guanshan-mist-music-gain-v1",String(gain));}catch(error){}
    // Unity can replay sounds queued before AudioContext.resume(), even after
    // AudioSource.Pause(). Keep the browser channels silent across that event.
    var guard=Module.__fogMusicGuard;
    if(!guard){
      guard=Module.__fogMusicGuard={gain:gain,context:null};
      guard.enforce=function(){
        if(typeof WEBAudio==="undefined")return;
        if(WEBAudio.audioContext&&guard.context!==WEBAudio.audioContext){
          if(guard.context)guard.context.removeEventListener("statechange",guard.enforce);
          guard.context=WEBAudio.audioContext;guard.context.addEventListener("statechange",guard.enforce);
        }
        if(guard.gain>0)return;
        Object.keys(WEBAudio.audioInstances).forEach(function(id){
          var channel=WEBAudio.audioInstances[id];
          if(channel.gain&&channel.pause){channel.setVolume(0);channel.pause();}
        });
      };
      guard.storage=function(event){
        if(event.key!=="guanshan-mist-music-gain-v1"||event.newValue===null)return;
        var value=Number(event.newValue);if(!Number.isFinite(value))return;
        guard.gain=value;guard.enforce();
      };
      window.addEventListener("storage",guard.storage);
      guard.timer=setInterval(guard.enforce,100);
      Module.deinitializers.push(function(){
        clearInterval(guard.timer);window.removeEventListener("storage",guard.storage);
        if(guard.context)guard.context.removeEventListener("statechange",guard.enforce);
      });
    }
    guard.gain=gain;guard.enforce();
  },
  FogVerificationReport: function (json) {
    window.__fogVerification = JSON.parse(UTF8ToString(json));
  },
  FogSyncSave: function () {
    if (Module.__fogSyncing) { Module.__fogSyncAgain = true; return; }
    var sync = function () {
      Module.__fogSyncing = true;
      FS.syncfs(false, function (error) {
        Module.__fogSyncing = false;
        window.__fogSaveStatus = error ? "error" : "synced";
        if (error) console.error("Fog save synchronization failed", error);
        if (Module.__fogSyncAgain) { Module.__fogSyncAgain = false; sync(); }
      });
    };
    sync();
  }
});
