(function(n){n.fn.getCheckboxVal=function(){var t=[],i=0;return this.each(function(){t[i++]=n(this).val()}),t},n.fieldValue2=function(n){var o=n.name,i=n.type,u=n.tagName.toLowerCase(),e,r,t,f;if(o&&!n.disabled&&i!="reset"&&i!="button"&&i!="submit"&&i!="image"&&(i!="checkbox"&&i!="radio"||n.checked)&&(u!="select"||n.selectedIndex!=-1)){if(u=="select"){if(e=n.selectedIndex,e<0)return null;for(r=0;r<n.options.length;r++)if(t=n.options[r],t.selected)return(f=t.value)||(f=t.attributes&&t.attributes.value&&!t.attributes.value.specified?t.text:t.value),f}else if(u=="input"&&i=="checkbox"&&!n.value)return n.checked}else return null;return n.value},n.fn.formToArray2=function(){var i={},o,r,u,e,f,t;if(this.length==0||(o=this[0],r=o.elements,!r))return i;for(u=0;u<r.length;u++)(e=r[u],f=e.name,f)&&(t=n.fieldValue2(e),t!==null&&typeof t!="undefined"&&t!=""&&(i[f]||(i[f]=t)));return i},n.fn.formSerialize2=function(){return n.param(this.formToArray2())},n.QueryString=function(t,i){var r={};return n.each(t.split("&"),function(){var n=this.split("=");r[n[0]]=n[1]}),r[i]},n.block=function(){n.blockUI({message:'working on it...<img src="/images/loading.gif"/>'})},n.unblock=function(){n.unblockUI({fadeOut:150})},n.navigate=function(n,t){n+=(n.match(/\?/)?"&":"?")+t,window.location=n}})(jQuery)