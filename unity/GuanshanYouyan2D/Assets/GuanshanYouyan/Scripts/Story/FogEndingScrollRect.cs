using UnityEngine;
using UnityEngine.EventSystems;

namespace GuanshanYouyan.Story
{
    // Run the automatic movement after ScrollRect's layout/bounds pass every frame.
    public sealed class FogEndingScrollRect : UnityEngine.UI.ScrollRect
    {
        public FogEndingRoll Roll{get;set;}
        protected override void LateUpdate(){base.LateUpdate();Roll?.Advance(Time.unscaledDeltaTime);}
        public override void OnBeginDrag(PointerEventData data){base.OnBeginDrag(data);Roll?.BeginManualDrag();}
        public override void OnEndDrag(PointerEventData data){base.OnEndDrag(data);Roll?.EndManualDrag();}
        public override void OnScroll(PointerEventData data){base.OnScroll(data);Roll?.ManualScroll();}
        protected override void OnDisable(){Roll?.CancelManualDrag();base.OnDisable();}
    }
}
