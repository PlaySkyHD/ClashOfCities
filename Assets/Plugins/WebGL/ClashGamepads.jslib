mergeInto(LibraryManager.library, {
  ClashTouchMode: function (active) { if(window.clashTouch)window.clashTouch.setActive(!!active); },
  ClashGamepads: function () {
    var devices = [];
    var pads = navigator.getGamepads ? navigator.getGamepads() : [];
    for (var i = 0; i < pads.length; i++) {
      var p = pads[i];
      if (!p || !p.connected || p.mapping !== 'standard') continue;
      var nintendo = /nintendo|switch|057e/i.test(p.id);
      // W3C positions -> SDL label-oriented buttons used by the game.
      var map = nintendo ? [1,0,3,2,8,16,9,10,11,4,5,12,13,14,15] : [0,1,2,3,8,16,9,10,11,4,5,12,13,14,15];
      var buttons = 0;
      for (var b = 0; b < map.length; b++) if (p.buttons[map[b]] && p.buttons[map[b]].pressed) buttons |= (1 << b);
      devices.push({id:p.index,name:p.id,buttons:buttons,x:p.axes[0]||0,y:p.axes[1]||0,scroll:p.axes[3]||0});
    }
    var touch=window.clashTouch;
    if(touch && touch.enabled){devices.push({id:1000,name:'Touch',buttons:touch.buttons|touch.taps,x:touch.x,y:touch.y,scroll:0});touch.taps=0;}
    var json = JSON.stringify({devices:devices});
    var memory = _malloc(lengthBytesUTF8(json)+1);
    stringToUTF8(json,memory,lengthBytesUTF8(json)+1);
    return memory;
  }
});
