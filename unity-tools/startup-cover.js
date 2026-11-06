// Share one title-screen budget across web loading and Unity startup.
// Keep the web cover until the next game screen has rendered behind it.
(function(){
 'use strict';
 const startedAt=performance.now(),coverDurationMs=3000;
 const state={runtime:false,presented:!!window.__fogCoverPresented,pending:false,released:false};
 function release(){
  if(!state.runtime||!state.presented||state.pending||state.released)return;
  state.pending=true;
  const readyAt=performance.now();
  window.__fogLoadTimings=Object.assign(window.__fogLoadTimings||{},{coverStartedMs:startedAt,coverDurationMs,nativeCoverReadyMs:readyAt});
  setTimeout(function(){
   // The already-loaded title must not restart its own three-second hold.
   window.fogGameInstance.SendMessage('雾港来信 · 完整故事','EnterFromCover');
   window.__fogLoadTimings.coverExitRequestedMs=performance.now();
   requestAnimationFrame(function(){requestAnimationFrame(function(){
    state.released=true;state.pending=false;
    document.documentElement.dataset.fogCoverReady='1';
    window.__fogLoadTimings=Object.assign(window.__fogLoadTimings,{coverReadyMs:performance.now(),handoff:'shared-cover-time-budget'});
   });});
  },Math.max(0,coverDurationMs-(readyAt-startedAt)));
 }
 window.FogStartupCover={
  runtimeReady:function(){
   state.runtime=true;
   window.__fogLoadTimings=Object.assign(window.__fogLoadTimings||{},{runtimeReadyMs:performance.now()});release();
  },
  presented:function(){state.presented=true;release();},
  state:function(){return Object.assign({},state);}
 };
})();
