// A viewport gate, independent of native orientation-lock support.
(() => {
  if (window.FogLandscape) return;
  const root = document.documentElement;
  const pending = new Set(), heldKeys = new Map(), audio = new Set(), pausedAudio = new Set();
  let blocked = window.innerWidth <= window.innerHeight, instance, paused = false;
  const style = document.createElement('style');
  style.textContent = `#fog-rotate{position:fixed;inset:0;z-index:2147483647;display:flex;align-items:center;justify-content:center;background:#13212b;color:#eee5d2;text-align:center;padding:24px;box-sizing:border-box;font-family:system-ui,sans-serif;touch-action:none;overscroll-behavior:none}#fog-rotate[hidden]{display:none!important}#fog-rotate .phone{width:40px;height:72px;border:3px solid #d8b66b;border-radius:9px;margin:0 auto 32px;transform:rotate(90deg)}#fog-rotate h1{font-size:26px;font-weight:600;margin:0 0 16px}#fog-rotate p{font-size:16px;line-height:1.8;margin:0;max-width:28em}html[data-fog-orientation=portrait] #unity-container{visibility:hidden;pointer-events:none}html[data-fog-orientation=portrait] #fog-fullscreen{visibility:hidden}`;
  document.head.appendChild(style);
  function suspend(context) {
    if (context.state !== 'running') return;
    pausedAudio.add(context);
    context.suspend().catch(() => {});
  }
  // Unity owns its WebAudio context privately. Track creation without changing
  // gain/preferences, and suspend only while the portrait gate is active.
  for (const name of ['AudioContext', 'webkitAudioContext']) {
    const Native = window[name];
    if (!Native) continue;
    window[name] = new Proxy(Native, {construct(Target, args) {
      const context = Reflect.construct(Target, args);
      audio.add(context);
      context.addEventListener('statechange', () => { if (blocked) suspend(context); });
      if (blocked) suspend(context);
      return context;
    }});
  }
  function releaseKeys() {
    for (const key of heldKeys.values()) {
      const event = new KeyboardEvent('keyup', {key:key.key,code:key.code,keyCode:key.keyCode,which:key.keyCode,bubbles:true});
      (document.getElementById('unity-canvas') || document).dispatchEvent(event);
    }
    heldKeys.clear();
    document.activeElement?.blur();
  }
  function apply() {
    const next = window.innerWidth <= window.innerHeight;
    if (next && !blocked) releaseKeys();
    blocked = next;
    root.dataset.fogOrientation = blocked ? 'portrait' : 'landscape';
    const overlay = document.getElementById('fog-rotate');
    if (overlay) overlay.hidden = !blocked;
    const container = document.getElementById('unity-container');
    if (container) container.inert = blocked;
    if (blocked) {
      if (instance && !paused) {instance.Module.pauseMainLoop();paused = true;}
      audio.forEach(suspend);
    } else {
      if (paused) {instance.Module.resumeMainLoop();paused = false;}
      pausedAudio.forEach(context => {context.resume().catch(() => {});});
      pausedAudio.clear();
      pending.forEach(resolve => resolve());pending.clear();
    }
    root.dataset.fogGamePaused = String(paused);
  }
  function mount() {
    const overlay = document.createElement('section');
    overlay.id = 'fog-rotate';overlay.setAttribute('role','status');overlay.setAttribute('aria-live','polite');
    overlay.innerHTML = '<div><div class="phone" aria-hidden="true"></div><h1>请横屏游玩</h1><p>请将设备旋转为横屏。<br>横屏后游戏将自动继续。</p></div>';
    document.body.appendChild(overlay);apply();
  }
  for (const type of ['keydown','keyup','pointerdown','pointerup','pointermove','pointercancel','mousedown','mouseup','mousemove','click','dblclick','touchstart','touchmove','touchend','wheel']) {
    window.addEventListener(type, event => {
      if (blocked) {if (event.cancelable) event.preventDefault();event.stopImmediatePropagation();return;}
      if (type === 'keydown') heldKeys.set(event.code,{key:event.key,code:event.code,keyCode:event.keyCode});
      if (type === 'keyup') heldKeys.delete(event.code);
    }, {capture:true,passive:false});
  }
  window.addEventListener('resize',apply);
  window.addEventListener('orientationchange',apply);
  window.visualViewport?.addEventListener('resize',apply);
  document.addEventListener('fullscreenchange', () => {
    if (document.fullscreenElement && screen.orientation?.lock) screen.orientation.lock('landscape').catch(() => {});
    apply();
  });
  window.FogLandscape = {
    start(factory, ...args) {
      const ready = blocked ? new Promise(resolve => pending.add(resolve)) : Promise.resolve();
      return ready.then(() => factory(...args));
    },
    attach(game) {instance = game;apply();}
  };
  apply();
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded',mount,{once:true});else mount();
})();
