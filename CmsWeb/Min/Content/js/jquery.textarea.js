(function(n){function t(n,t,u){var f=n.scrollTop;n.setSelectionRange?i(n,t,u):document.selection&&r(n,t,u),n.scrollTop=f}function i(n,t,i){var r=n.selectionStart,s=n.selectionEnd,u,h,f,a,v;if(r==s)t?"\t"==n.value.substring(r-i.tabString.length,r)?(n.value=n.value.substring(0,r-i.tabString.length)+n.value.substring(r),n.focus(),n.setSelectionRange(r-i.tabString.length,r-i.tabString.length)):"\t"==n.value.substring(r,r+i.tabString.length)&&(n.value=n.value.substring(0,r)+n.value.substring(r+i.tabString.length),n.focus(),n.setSelectionRange(r,r)):(n.value=n.value.substring(0,r)+i.tabString+n.value.substring(r),n.focus(),n.setSelectionRange(r+i.tabString.length,r+i.tabString.length));else{var l=n.value.split("\n"),c=[],e=0,o=0;for(h in l)o=e+l[h].length,c.push({start:e,end:o,selected:e<=r&&o>r||o>=s&&e<s||e>r&&o<s}),e=o+1;u=0;for(h in c)c[h].selected&&(f=c[h].start+u,t&&i.tabString==n.value.substring(f,f+i.tabString.length)?(n.value=n.value.substring(0,f)+n.value.substring(f+i.tabString.length),u-=i.tabString.length):t||(n.value=n.value.substring(0,f)+i.tabString+n.value.substring(f),u+=i.tabString.length));n.focus(),a=r+(u>0?i.tabString.length:u<0?-i.tabString.length:0),v=s+u,n.setSelectionRange(a,v)}}function r(t,i,r){var u=document.selection.createRange(),w,p,l,d,a,o,c;if(t==u.parentElement())if(""==u.text)i?(w=u.getBookmark(),u.moveStart("character",-r.tabString.length),r.tabString==u.text?u.text="":(u.moveToBookmark(w),u.moveEnd("character",r.tabString.length),r.tabString==u.text&&(u.text="")),u.collapse(!0),u.select()):(u.text=r.tabString,u.collapse(!1),u.select());else{var b=u.text,v=b.length,f=b.split("\r\n"),s=document.body.createTextRange();s.moveToElementText(t),s.setEndPoint("EndToStart",u);var k=s.text,e=k.split("\r\n"),h=k.length,y=document.body.createTextRange();for(y.moveToElementText(t),y.setEndPoint("StartToEnd",u),p=y.text,l=document.body.createTextRange(),l.moveToElementText(t),l.setEndPoint("StartToEnd",s),d=l.text,a=n(t).html(),n("#r3").text(h+" + "+v+" + "+p.length+" = "+a.length),h+d.length<a.length?(e.push(""),h+=2,i&&r.tabString==f[0].substring(0,r.tabString.length)?f[0]=f[0].substring(r.tabString.length):i||(f[0]=r.tabString+f[0])):i&&r.tabString==e[e.length-1].substring(0,r.tabString.length)?e[e.length-1]=e[e.length-1].substring(r.tabString.length):i||(e[e.length-1]=r.tabString+e[e.length-1]),o=1;o<f.length;o++)i&&r.tabString==f[o].substring(0,r.tabString.length)?f[o]=f[o].substring(r.tabString.length):i||(f[o]=r.tabString+f[o]);1==e.length&&0==h&&(i&&r.tabString==f[0].substring(0,r.tabString.length)?f[0]=f[0].substring(r.tabString.length):i||(f[0]=r.tabString+f[0])),h+v+p.length<a.length&&(f.push(""),v+=2),s.text=e.join("\r\n"),u.text=f.join("\r\n"),c=document.body.createTextRange(),c.moveToElementText(t),0<h?c.setEndPoint("StartToEnd",s):c.setEndPoint("StartToStart",s),c.setEndPoint("EndToEnd",u),c.select()}}n.fn.tabby=function(i){var u=n.extend({},n.fn.tabby.defaults,i),r=n.fn.tabby.pressed;return this.each(function(){$this=n(this);var i=n.meta?n.extend({},u,$this.data()):u;$this.bind("keydown",function(u){var f=n.fn.tabby.catch_kc(u);return 16==f&&(r.shft=!0),17==f&&(r.ctrl=!0,setTimeout("$.fn.tabby.pressed.ctrl = false;",1e3)),18==f&&(r.alt=!0,setTimeout("$.fn.tabby.pressed.alt = false;",1e3)),9==f&&!r.ctrl&&!r.alt?(u.preventDefault,r.last=f,setTimeout("$.fn.tabby.pressed.last = null;",0),t(n(u.target).get(0),r.shft,i),!1):void 0}).bind("keyup",function(t){16==n.fn.tabby.catch_kc(t)&&(r.shft=!1)}).bind("blur",function(t){9==r.last&&n(t.target).one("focus",function(){r.last=null}).get(0).focus()})})},n.fn.tabby.catch_kc=function(n){return n.keyCode?n.keyCode:n.charCode?n.charCode:n.which},n.fn.tabby.pressed={shft:!1,ctrl:!1,alt:!1,last:null},n.fn.tabby.defaults={tabString:String.fromCharCode(9)}})(jQuery)