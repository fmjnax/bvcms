(function(n){n.fn.showPassword=function(t,i){var r=n(this);return n.fn.showPassword.checker=function(t,i){n('input[id="'+t+'"]').click(function(){n(this).attr("checked")?(n("input."+i).val(r.val()).attr("id",r.attr("id")).attr("name",r.attr("name")),n("input."+i).css("display","inline"),r.css("display","none").removeAttr("id").removeAttr("name")):(r.val(n("input."+i).val()).attr("id",n("input."+i).attr("id")).attr("name",n("input."+i).attr("name")),r.css("display","inline"),n("input."+i).css("display","none").removeAttr("id").removeAttr("name"))})},this.each(function(){var u={classname:"class",name:"password-input",text:"Show Password"},f="spcb_"+parseInt(Math.random()*1e3),e=f.replace("spcb_","spin_"),o,h,s;return o=r.attr("class")!==""?e+" "+r.attr("class"):e,typeof t=="object"&&n.extend(u,t),typeof i=="object"&&n.extend(u,i),h=u.name,theclass=u.classname==""?"":' class="'+u.clasname+'"',n(this).before('<input type="text" value="" class="'+o+'" style="display: none;" />'),s="<label><input"+theclass+' type="checkbox" id="'+f+'" name="'+h+'" value="sp" />'+u.text+"<\/label>",t=="object"||typeof t=="undefined"?n(this).after(s):n(t).html(s),n.fn.showPassword.checker(f,e),this})}})(jQuery)