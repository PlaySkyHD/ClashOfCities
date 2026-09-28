const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
class Element {
 constructor(bit){this.dataset={pad:String(bit)};this.listeners={};this.style={};this.classList={add(){},remove(){},toggle(){}};}
 addEventListener(type,fn){this.listeners[type]=fn;}
 setAttribute(){} setPointerCapture(){}
 getBoundingClientRect(){return {left:0,top:0,width:100,height:100};}
 emit(type,id=1,x=50,y=50){this.listeners[type]?.({type,pointerId:id,clientX:x,clientY:y,preventDefault(){}});}
}
const buttons=[0,1,2,3,10,4,6].map(x=>new Element(x));
const ids=Object.fromEntries(['touch-controls','touch-toggle','touch-stick','touch-knob'].map(x=>[x,new Element()]));
ids['touch-controls'].querySelectorAll=()=>buttons;
const window=new Element(),document=new Element();document.body=new Element();document.getElementById=x=>ids[x];
vm.runInNewContext(fs.readFileSync('Assets/WebGLTemplates/Clash/touch.js','utf8'),{window,document,matchMedia:()=>({matches:true})});
const s=window.clashTouch;s.setActive(true);
ids['touch-stick'].emit('pointerdown',11,85,50);buttons[4].emit('pointerdown',12);
assert.equal(s.x,1);assert.equal(s.buttons,1<<10);
buttons[0].emit('pointerdown',13);buttons[4].emit('pointerup',12);
assert.equal(s.buttons,1);assert.equal(s.x,1);
ids['touch-stick'].emit('pointerup',11);assert.equal(s.x,0);
buttons[0].emit('pointercancel',13);assert.equal(s.buttons,0);assert.equal(s.taps&1,0);
buttons[2].emit('pointerdown',14);buttons[2].emit('pointerup',14);assert.ok(s.taps&(1<<2));
window.emit('blur');assert.equal(s.buttons|s.taps,0);
buttons[4].emit('pointerdown',15);s.setActive(false);assert.equal(s.buttons,0);assert.equal(ids['touch-controls'].hidden,true);
console.log('Touch: simultaneous move/ultimate/attack, independent release, cancellation, quick tap, blur and pause reset passed.');
