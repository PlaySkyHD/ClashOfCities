const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
let pads = [], result;
const context = {
  window:{},
  LibraryManager: {library:{}}, mergeInto:(target,values)=>Object.assign(target,values),
  navigator:{getGamepads:()=>pads}, _malloc:()=>1,
  lengthBytesUTF8:s=>Buffer.byteLength(s), stringToUTF8:s=>{result=JSON.parse(s);}
};
vm.runInNewContext(fs.readFileSync('Assets/Plugins/WebGL/ClashGamepads.jslib','utf8'),context);
function read(){context.LibraryManager.library.ClashGamepads();return result.devices;}
function pad(id,index=0){return {id,index,connected:true,mapping:'standard',buttons:Array.from({length:17},()=>({pressed:false})),axes:[.5,-.75,0,.3]};}
assert.equal(read().length,0);
for(const id of ['Xbox Controller','Nintendo Switch Pro Controller']){
  pads=[pad(id)];
  const nintendo=id.startsWith('Nintendo');
  const pairs=[[nintendo?1:0,0],[nintendo?0:1,1],[nintendo?3:2,2],[nintendo?2:3,3],[5,10],[9,6],[8,4],[12,11],[13,12],[14,13],[15,14]];
  for(const [raw,game] of pairs){pads[0].buttons.forEach(b=>b.pressed=false);pads[0].buttons[raw].pressed=true;assert.equal(read()[0].buttons,1<<game);}
  assert.equal(read()[0].x,.5);assert.equal(read()[0].y,-.75);
}
pads=[pad('Nintendo Switch',0),null,pad('Xbox',2)];assert.deepEqual(read().map(p=>p.id),[0,2]);
pads[0].connected=false;assert.equal(read().length,1);
pads[2].mapping='';assert.equal(read().length,0);
console.log('Browser mapping: empty/disconnected pads, separate devices, axes, 22 Nintendo/standard buttons passed.');

context.window.clashTouch={enabled:true,buttons:1<<10,taps:1<<2,x:.5,y:-.3};
assert.equal(read()[0].buttons,(1<<10)|(1<<2));assert.equal(read()[0].buttons,1<<10);
assert.equal(read()[0].id,1000);context.window.clashTouch.enabled=false;assert.equal(read().length,0);
console.log('Touch bridge: held ultimate, buffered tap, stick and disabled device passed.');
