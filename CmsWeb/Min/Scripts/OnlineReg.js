$(function(){$.dpoptions={yearRange:"c-99:c+0",onSelect:function(n){var t=$(this).closest("form");$("#age",t).text($.dodate(n))}},$.dodate=function(n){var t=/^(0[1-9]|1[012])(0[1-9]|[12][0-9]|3[01])((19|20)?[0-9]{2})$/i.exec(n),i,r,u;if(t==null&&(t=/^(0?[1-9]|1[012])[\/-](0?[1-9]|[12][0-9]|3[01])[\/-]((19|20)?[0-9]{2})$/i.exec(n)),t!=null){i=parseInt(t[3]),i<1e3&&(i=i<50?i+2e3:i+1900),r=new Date(i,t[1]-1,t[2]),u=new Date,r>u&&(r=new Date(i-100,t[1]-1,t[2]));for(var e=r.getFullYear(),o=r.getMonth(),n=r.getDate(),f=0;r<=u;)r=new Date(e+f,o,n),f++;return f-2}},$("form.DisplayEdit input.dob").live("blur",function(){var n=$(this).closest("form");$("#age",n).text($.dodate($(this).val()))}),$("a.submitbutton, a.submitlink, input.submitbutton.ajax","form.DisplayEdit").live("click",function(n){n.preventDefault();var t=$(this).closest("form"),i=t.serialize();return $.refreshenabled=!0,$.post($(this).attr("href"),i,function(n){if(n.charAt(0)==="/"){$("body").html("<p>Please wait...<\/p>"),window.location=n;return}$(t).html(n).ready(function(){$("#submitit").attr("onlyoneallowed")=="true"?($.InstructionsShow(),$("#submitit").click()):($.InstructionsShow(),$("#dob").datepicker($.dpoptions))})}),!1}),$.setButtons=function(){$(".submitbutton").button()},$.ShowPaymentInfo=function(){var n=$("input[name=Type]:checked").val();$("div.Card").hide(),$("div.Bank").hide(),n==="C"?$("div.Card").show():n==="B"&&$("div.Bank").show()},$.InstructionsShow=function(){$("div.instructions").hide(),$("#selectfamily").attr("id")?$("div.instructions.select").show():$("#personedit").attr("id")?($("#fillout").hide(),$("div.instructions.find").show()):$("#otheredit").attr("id")?$("div.instructions.options").show():$("#specialedit").attr("id")?$("div.instructions.special").show():$("#username").attr("id")?($("#username").focus(),$("div.instructions.login").show()):$("#submitit").attr("id")?$("div.instructions.submit").show():$("#sorry").attr("id")&&$("div.instructions.sorry").show(),$("#allowcc").val()&&$.ShowPaymentInfo()},$("form.DisplayEdit").submit(function(){return $("#submitit").val()?($("#submitit").attr("disabled","true"),!0):!1}),$("form.DisplayEdit a.cancel").live("click",function(n){n.preventDefault();var t=$(this).closest("form"),i=t.serialize();return $.post($(this).attr("href"),i,function(n){n=="refresh"&&location.reload(),$(t).html(n).ready(function(){setTimeout($.setButtons,15)})}),!1}),$("#copy").live("click",function(){return $("input[name$='.emcontact']:last").val($("input[name$='.emcontact']:hidden:last").val()),$("input[name$='.emphone']:last").val($("input[name$='.emphone']:hidden:last").val()),$("input[name$='.insurance']:last").val($("input[name$='.insurance']:hidden:last").val()),$("input[name$='.policy']:last").val($("input[name$='.policy']:hidden:last").val()),$("input[name$='.doctor']:last").val($("input[name$='.doctor']:hidden:last").val()),$("input[name$='.docphone']:last").val($("input[name$='.docphone']:hidden:last").val()),$("input[name$='.mname']:last").val($("input[name$='.mname']:hidden:last").val()),$("input[name$='.fname']:last").val($("input[name$='.fname']:hidden:last").val()),$("input[name$='.paydeposit']:last").val($("input[name$='.paydeposit']:hidden:last").val()),!1}),$.InstructionsShow(),$.validator.setDefaults({highlight:function(n){$(n).addClass("ui-state-highlight")},unhighlight:function(n){$(n).removeClass("ui-state-highlight")}}),$("form.DisplayEdit").validate({rules:{"m.donation":{number:!0}}}),$(".personheader a").live("click",function(n){return n.preventDefault(),$(this).closest("div").nextAll("table").slideToggle(),!1}),$("input.sum").live("change",function(){var n=0;$("input.sum").each(function(){isNaN(this.value)||this.value.length==0||(n+=parseFloat(this.value))}),$("#total").html(n.toFixed(2))}),$("input[name=Type]").live("change",$.ShowPaymentInfo),$("#password").live("keypress",function(n){return n.which&&n.which==13||n.keyCode&&n.keyCode==13?($("#loginbt").click(),!1):!0})})