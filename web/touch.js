(function () {
  'use strict';
  const coarse = matchMedia('(pointer: coarse)').matches;
  const state = window.clashTouch = { enabled: coarse, x: 0, y: 0, buttons: 0, taps: 0, active: false };
  const panel = document.getElementById('touch-controls');
  const toggle = document.getElementById('touch-toggle');
  const stick = document.getElementById('touch-stick');
  const knob = document.getElementById('touch-knob');
  let stickPointer = null;
  const held = new Map();
  function reset() { state.x = state.y = state.buttons = state.taps = 0; held.clear(); stickPointer = null; knob.style.transform = 'translate(0,0)'; panel.querySelectorAll('button').forEach(b => b.classList.remove('held')); }
  function visible() {
    document.body.classList.toggle('touch-mode', state.enabled);
    panel.hidden = !state.enabled || !state.active;
    toggle.setAttribute('aria-pressed', String(state.enabled));
    toggle.textContent = state.enabled ? 'Touch an' : 'Touch aus';
  }
  state.setActive = function (active) { if (state.active !== active) { reset(); state.active = active; visible(); } };
  toggle.addEventListener('click', () => { reset(); state.enabled = !state.enabled; visible(); });
  function move(event) {
    const r = stick.getBoundingClientRect();const radius=r.width*.35;
    let x=(event.clientX-r.left-r.width/2)/radius,y=(event.clientY-r.top-r.height/2)/radius;
    const length=Math.hypot(x,y);if(length>1){x/=length;y/=length;}
    state.x=x;state.y=y;knob.style.transform=`translate(${x*radius}px,${y*radius}px)`;
  }
  stick.addEventListener('pointerdown', event => {
    if(stickPointer!==null)return;event.preventDefault();stickPointer=event.pointerId;stick.setPointerCapture(event.pointerId);move(event);
  });
  stick.addEventListener('pointermove',event=>{if(event.pointerId===stickPointer){event.preventDefault();move(event);}});
  function releaseStick(event){if(event.pointerId===stickPointer){stickPointer=null;state.x=state.y=0;knob.style.transform='translate(0,0)';}}
  ['pointerup','pointercancel','lostpointercapture'].forEach(type=>stick.addEventListener(type,releaseStick));
  panel.querySelectorAll('[data-pad]').forEach(button => {
    const bit=1<<Number(button.dataset.pad);
    button.addEventListener('pointerdown', event => {
      event.preventDefault();button.setPointerCapture(event.pointerId);held.set(event.pointerId,bit);state.buttons|=bit;state.taps|=bit;button.classList.add('held');
    });
    function release(event){if(event.type==='pointercancel')state.taps&=~bit;held.delete(event.pointerId);state.buttons=Array.from(held.values()).reduce((a,b)=>a|b,0);if(!(state.buttons&bit))button.classList.remove('held');}
    ['pointerup','pointercancel','lostpointercapture'].forEach(type=>button.addEventListener(type,release));
  });
  window.addEventListener('blur',reset);
  document.addEventListener('visibilitychange',()=>{if(document.hidden)reset();});
  window.addEventListener('orientationchange',reset);
  visible();
})();
