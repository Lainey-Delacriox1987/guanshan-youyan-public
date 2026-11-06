// Bound rendering work independently of CSS size, including high-DPI screens.
// Unity's screen/input coordinates follow this backing store automatically.
(function(){
 'use strict';
 window.FogWebPerformance={configure:function(canvas,config){
  config.matchWebGLToCanvasSize=false;
  config.devicePixelRatio=1;
  const resize=function(){
   const rect=canvas.getBoundingClientRect(),w=Math.max(1,rect.width),h=Math.max(1,rect.height);
   const scale=Math.min(window.devicePixelRatio||1,1.25,Math.sqrt(1920*1080/(w*h)));
   const width=Math.max(1,Math.round(w*scale)),height=Math.max(1,Math.round(h*scale));
   if(canvas.width===width&&canvas.height===height)return;
   const module=window.fogGameInstance&&window.fogGameInstance.Module;
   // Use the exported API so the engine's resize listeners are notified too.
   if(module&&typeof module.setCanvasSize==='function')module.setCanvasSize(width,height);
   else{canvas.width=width;canvas.height=height;}
  };
  resize();window.addEventListener('resize',resize,{passive:true});
  document.addEventListener('fullscreenchange',resize);
 }};
})();
