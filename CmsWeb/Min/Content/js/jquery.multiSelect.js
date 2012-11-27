(function(n){var t=0;n.widget("ech.multiselect",{options:{header:!0,height:175,minWidth:225,classes:"",checkAllText:"Check all",uncheckAllText:"Uncheck all",noneSelectedText:"Select options",selectedText:"# selected",selectedList:0,show:null,hide:null,autoOpen:!1,multiple:!0,position:{}},_create:function(){var i=this.element.hide(),t=this.options;this.speed=n.fx.speeds._default,this._isOpen=!1;var u=(this.button=n('<button type="button"><span class="ui-icon ui-icon-triangle-2-n-s"><\/span><\/button>')).addClass("ui-multiselect ui-widget ui-state-default ui-corner-all").addClass(t.classes).attr({title:i.attr("title"),"aria-haspopup":!0,tabIndex:i.attr("tabIndex")}).insertAfter(i),e=(this.buttonlabel=n("<span />")).html(t.noneSelectedText).appendTo(u),r=(this.menu=n("<div />")).addClass("ui-multiselect-menu ui-widget ui-widget-content ui-corner-all").addClass(t.classes).appendTo(document.body),f=(this.header=n("<div />")).addClass("ui-widget-header ui-corner-all ui-multiselect-header ui-helper-clearfix").appendTo(r),o=(this.headerLinkContainer=n("<ul />")).addClass("ui-helper-reset").html(function(){return t.header===!0?'<li><a class="ui-multiselect-all" href="#"><span class="ui-icon ui-icon-check"><\/span><span>'+t.checkAllText+'<\/span><\/a><\/li><li><a class="ui-multiselect-none" href="#"><span class="ui-icon ui-icon-closethick"><\/span><span>'+t.uncheckAllText+"<\/span><\/a><\/li>":typeof t.header=="string"?"<li>"+t.header+"<\/li>":""}).append('<li class="ui-multiselect-close"><a href="#" class="ui-multiselect-close"><span class="ui-icon ui-icon-circle-close"><\/span><\/a><\/li>').appendTo(f),s=(this.checkboxContainer=n("<ul />")).addClass("ui-multiselect-checkboxes ui-helper-reset").appendTo(r);this._bindEvents(),this.refresh(!0),t.multiple||r.addClass("ui-multiselect-single")},_init:function(){this.options.header===!1&&this.header.hide(),this.options.multiple||this.headerLinkContainer.find(".ui-multiselect-all, .ui-multiselect-none").hide(),this.options.autoOpen&&this.open(),this.element.is(":disabled")&&this.disable()},refresh:function(i){var u=this.element,f=this.options,s=this.menu,h=this.checkboxContainer,e=[],r="",o=u.attr("id")||t++;u.find("option").each(function(t){var w=n(this),u=this.parentNode,c=this.innerHTML,v=this.title,y=this.value,l="ui-multiselect-"+(this.id||o+"-option-"+t),s=this.disabled,a=this.selected,h=["ui-corner-all"],p=(s?"ui-multiselect-disabled ":" ")+this.className,i;u.tagName==="OPTGROUP"&&(i=u.getAttribute("label"),n.inArray(i,e)===-1&&(r+='<li class="ui-multiselect-optgroup-label '+u.className+'"><a href="#">'+i+"<\/a><\/li>",e.push(i))),s&&h.push("ui-state-disabled"),a&&!f.multiple&&h.push("ui-state-active"),r+='<li class="'+p+'">',r+='<label for="'+l+'" title="'+v+'" class="'+h.join(" ")+'">',r+='<input id="'+l+'" name="multiselect_'+o+'" type="'+(f.multiple?"checkbox":"radio")+'" value="'+y+'" title="'+c+'"',a&&(r+=' checked="checked"',r+=' aria-selected="true"'),s&&(r+=' disabled="disabled"',r+=' aria-disabled="true"'),r+=" /><span>"+c+"<\/span><\/label><\/li>"}),h.html(r),this.labels=s.find("label"),this.inputs=this.labels.children("input"),this._setButtonWidth(),this._setMenuWidth(),this.button[0].defaultValue=this.update(),i||this._trigger("refresh")},update:function(){var t=this.options,r=this.inputs,u=r.filter(":checked"),i=u.length,f;return f=i===0?t.noneSelectedText:n.isFunction(t.selectedText)?t.selectedText.call(this,i,r.length,u.get()):/\d/.test(t.selectedList)&&t.selectedList>0&&i<=t.selectedList?u.map(function(){return n(this).next().html()}).get().join(", "):t.selectedText.replace("#",i).replace("#",r.length),this.buttonlabel.html(f),f},_bindEvents:function(){function r(){return t[t._isOpen?"close":"open"](),!1}var t=this,i=this.button;i.find("span").bind("click.multiselect",r),i.bind({click:r,keypress:function(n){switch(n.which){case 27:case 38:case 37:t.close();break;case 39:case 40:t.open()}},mouseenter:function(){i.hasClass("ui-state-disabled")||n(this).addClass("ui-state-hover")},mouseleave:function(){n(this).removeClass("ui-state-hover")},focus:function(){i.hasClass("ui-state-disabled")||n(this).addClass("ui-state-focus")},blur:function(){n(this).removeClass("ui-state-focus")}}),this.header.delegate("a","click.multiselect",function(i){n(this).hasClass("ui-multiselect-close")?t.close():t[n(this).hasClass("ui-multiselect-all")?"checkAll":"uncheckAll"](),i.preventDefault()}),this.menu.delegate("li.ui-multiselect-optgroup-label a","click.multiselect",function(i){i.preventDefault();var f=n(this),r=f.parent().nextUntil("li.ui-multiselect-optgroup-label").find("input:visible:not(:disabled)"),u=r.get(),e=f.parent().text();t._trigger("beforeoptgrouptoggle",i,{inputs:u,label:e})!==!1&&(t._toggleChecked(r.filter(":checked").length!==r.length,r),t._trigger("optgrouptoggle",i,{inputs:u,label:e,checked:u[0].checked}))}).delegate("label","mouseenter.multiselect",function(){n(this).hasClass("ui-state-disabled")||(t.labels.removeClass("ui-state-hover"),n(this).addClass("ui-state-hover").find("input").focus())}).delegate("label","keydown.multiselect",function(i){i.preventDefault();switch(i.which){case 9:case 27:t.close();break;case 38:case 40:case 37:case 39:t._traverse(i.which,this);break;case 13:n(this).find("input")[0].click()}}).delegate('input[type="checkbox"], input[type="radio"]',"click.multiselect",function(i){var u=n(this),f=this.value,r=this.checked,e=t.element.find("option");if(this.disabled||t._trigger("click",i,{value:f,text:this.title,checked:r})===!1){i.preventDefault();return}u.focus(),u.attr("aria-selected",r),e.each(function(){this.value===f?this.selected=r:t.options.multiple||(this.selected=!1)}),t.options.multiple||(t.labels.removeClass("ui-state-active"),u.closest("label").toggleClass("ui-state-active",r),t.close()),t.element.trigger("change"),setTimeout(n.proxy(t.update,t),10)}),n(document).bind("mousedown.multiselect",function(i){!t._isOpen||n.contains(t.menu[0],i.target)||n.contains(t.button[0],i.target)||i.target===t.button[0]||t.close()}),n(this.element[0].form).bind("reset.multiselect",function(){setTimeout(n.proxy(t.refresh,t),10)})},_setButtonWidth:function(){var n=this.element.outerWidth(),t=this.options;/\d/.test(t.minWidth)&&n<t.minWidth&&(n=t.minWidth),this.button.width(n)},_setMenuWidth:function(){var n=this.menu,t=this.button.outerWidth()-parseInt(n.css("padding-left"),10)-parseInt(n.css("padding-right"),10)-parseInt(n.css("border-right-width"),10)-parseInt(n.css("border-left-width"),10);n.width(t||this.button.outerWidth())},_traverse:function(t,i){var e=n(i),r=t===38||t===37,f=e.parent()[r?"prevAll":"nextAll"]("li:not(.ui-multiselect-disabled, .ui-multiselect-optgroup-label)")[r?"last":"first"](),u;f.length?f.find("label").trigger("mouseover"):(u=this.menu.find("ul").last(),this.menu.find("label")[r?"last":"first"]().trigger("mouseover"),u.scrollTop(r?u.height():0))},_toggleState:function(n,t){return function(){this.disabled||(this[n]=t),t?this.setAttribute("aria-selected",!0):this.removeAttribute("aria-selected")}},_toggleChecked:function(t,i){var r=i&&i.length?i:this.inputs,f=this,u;r.each(this._toggleState("checked",t)),r.eq(0).focus(),this.update(),u=r.map(function(){return this.value}).get(),this.element.find("option").each(function(){!this.disabled&&n.inArray(this.value,u)>-1&&f._toggleState("selected",t).call(this)}),r.length&&this.element.trigger("change")},_toggleDisabled:function(t){this.button.attr({disabled:t,"aria-disabled":t})[t?"addClass":"removeClass"]("ui-state-disabled");var i=this.menu.find("input"),r="ech-multiselect-disabled";i=t?i.filter(":enabled").data(r,!0):i.filter(function(){return n.data(this,r)===!0}).removeData(r),i.attr({disabled:t,"arial-disabled":t}).parent()[t?"addClass":"removeClass"]("ui-state-disabled"),this.element.attr({disabled:t,"aria-disabled":t})},open:function(){var s=this,i=this.button,r=this.menu,f=this.speed,t=this.options,e=[];if(this._trigger("beforeopen")!==!1&&!i.hasClass("ui-state-disabled")&&!this._isOpen){var h=r.find("ul").last(),u=t.show,o=i.offset();n.isArray(t.show)&&(u=t.show[0],f=t.show[1]||s.speed),u&&(e=[u,f]),h.scrollTop(0).height(t.height),n.ui.position&&!n.isEmptyObject(t.position)?(t.position.of=t.position.of||i,r.show().position(t.position).hide()):r.css({top:o.top+i.outerHeight(),left:o.left}),n.fn.show.apply(r,e),this.labels.eq(0).trigger("mouseover").trigger("mouseenter").find("input").trigger("focus"),i.addClass("ui-state-active"),this._isOpen=!0,this._trigger("open")}},close:function(){if(this._trigger("beforeclose")!==!1){var t=this.options,i=t.hide,r=this.speed,u=[];n.isArray(t.hide)&&(i=t.hide[0],r=t.hide[1]||this.speed),i&&(u=[i,r]),n.fn.hide.apply(this.menu,u),this.button.removeClass("ui-state-active").trigger("blur").trigger("mouseleave"),this._isOpen=!1,this._trigger("close")}},enable:function(){this._toggleDisabled(!1)},disable:function(){this._toggleDisabled(!0)},checkAll:function(){this._toggleChecked(!0),this._trigger("checkAll")},uncheckAll:function(){this._toggleChecked(!1),this._trigger("uncheckAll")},getChecked:function(){return this.menu.find("input").filter(":checked")},destroy:function(){return n.Widget.prototype.destroy.call(this),this.button.remove(),this.menu.remove(),this.element.show(),this},isOpen:function(){return this._isOpen},widget:function(){return this.menu},getButton:function(){return this.button},_setOption:function(t,i){var r=this.menu;switch(t){case"header":r.find("div.ui-multiselect-header")[i?"show":"hide"]();break;case"checkAllText":r.find("a.ui-multiselect-all span").eq(-1).text(i);break;case"uncheckAllText":r.find("a.ui-multiselect-none span").eq(-1).text(i);break;case"height":r.find("ul").last().height(parseInt(i,10));break;case"minWidth":this.options[t]=parseInt(i,10),this._setButtonWidth(),this._setMenuWidth();break;case"selectedText":case"selectedList":case"noneSelectedText":this.options[t]=i,this.update();break;case"classes":r.add(this.button).removeClass(this.options.classes).addClass(i);break;case"multiple":r.toggleClass("ui-multiselect-single",!i),this.options.multiple=i,this.element[0].multiple=i,this.refresh()}n.Widget.prototype._setOption.apply(this,arguments)}})})(jQuery)