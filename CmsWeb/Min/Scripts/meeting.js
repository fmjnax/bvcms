function AddSelected(n){$("#visitorDialog").dialog("close"),n.error&&alert(n.error),window.location.reload(!0)}$(function(){$(".clickSelectG").editable("/Meeting/EditGroup/",{indicator:'<img src="/images/loading.gif">',loadurl:"/Meeting/MeetingTypes/",loadtype:"POST",type:"select",submit:"OK",style:"display: inline",tooltip:"Click to edit...",callback:function(n){n=="Group (headcount)"?$(".numpresent").editable("enable"):$(".numpresent").editable("disable")}}),$(".clickSelectC").editable("/Meeting/EditAttendCredit/",{indicator:'<img src="/images/loading.gif">',loadurl:"/Meeting/AttendCredits/",loadtype:"POST",type:"select",submit:"OK",tooltip:"Click to edit...",style:"display: inline"}),$(".numpresent").editable("enable"),$(".clickEdit").editable("/Meeting/Edit/",{indicator:"<img src='/images/loading.gif'>",tooltip:"Click to edit...",style:"display: inline",width:"300px",height:25,submit:"OK"}),$(".bt").button(),$("#visitorDialog").dialog({title:"Add Visitors Dialog",bgiframe:!0,autoOpen:!1,width:750,height:700,modal:!0,overlay:{opacity:.5,background:"black"},close:function(){$("iframe",this).attr("src","")}}),$("#addvisitor,#addregistered").click(function(n){if(n.preventDefault(),n.shiftKey)confirm("Are you sure you want to join all visitors to org?")&&$.post("/Meeting/JoinAllVisitors/"+$("#meetingid").val(),{},function(n){alert(n)});else{var t=$("#visitorDialog");$("iframe",t).attr("src",this.href),t.dialog("open")}}),$("#JoinAllVisitors").click(function(n){n.preventDefault()}),$("#showbuttons input[@name=show]:checked").val()=="attends"&&$(".atck:not(:checked)").parent().parent().hide(),$("#editing").is(":checked")&&($(".atck").removeAttr("disabled"),$(".rgck").removeAttr("disabled")),$("#attends > tbody > tr:visible:even").addClass("alt"),$("#showbuttons input:radio").change(function(){$("#attends > tbody > tr").hide().removeClass("alt");switch($(this).val()){case"attends":$(".atck:checked").parent().parent().show();break;case"absents":$(".atck:not(:checked)").parent().parent().show();break;case"reg":$(".rgck:checked").parent().parent().show(),$(".atck:checked").parent().parent().show();break;case"all":$("#attends > tbody > tr").show()}$("#attends > tbody > tr:visible:even").addClass("alt")}),$("#editing").change(function(){$(this).is(":checked")?($("#showregistered").val()||$("#showbuttons input:radio[value=all]").click(),$(".atck,.rgck").removeAttr("disabled")):$(".atck,.rgck").attr("disabled","disabled")}),$("#sortbyname").click(function(){$("#sort").val()=="false"?($("#sort").val("true"),$("#attends > tbody > tr").sortElements(function(n,t){return $(n).find("td.name a").text()>$(t).find("td.name a").text()?1:-1})):($("#sort").val("false"),$("#attends > tbody > tr").sortElements(function(n,t){var r=$(n).attr("rowtype"),i=$(t).attr("rowtype");return r>i?-1:r<i?1:$(n).find("td.name a").text()>$(t).find("td.name a").text()?1:-1}))}),$("#registering").change(function(){$(this).is(":checked")?($(".showreg").show(),$("#addregistered").removeClass("hidden")):($(".showreg").hide(),$("#addregistered").addClass("hidden"))}),$(".showreg").hide(),$("#showregistered").val()&&($("#showbuttons input:radio[value=reg]").click(),$("#registering").click()),$(".atck").change(function(){var t=$(this),i=t.parent().parent();$.post("/Meeting/MarkAttendance/",{MeetingId:$("#meetingid").val(),PeopleId:t.attr("pid"),Present:t.is(":checked")},function(n){if(n.error)t.attr("checked",!t.is(":checked")),alert(n.error);else{i.effect("highlight",{},3e3);for(var r in n)$("#"+r+" span").text(n[r])}})}),$(".rgck").change(function(){var t=$(this),i=t.parent().parent();$.post("/Meeting/MarkRegistered/",{MeetingId:$("#meetingid").val(),PeopleId:t.attr("pid"),Registered:t.is(":checked")},function(n){if(n.error)t.attr("checked",!t.is(":checked")),alert(n.error);else for(var i in n)$("#"+i+" span").text(n[i])})}),$("#wandtarget").keypress(function(n){var r,t,i;return n.which!=13?!0:($("#editing").is(":checked")||$("#editing").click(),r=$("#wandtarget"),t=r.val(),r.val(""),t.substring(2,0)=="M.")?($.post("/Meeting/CreateMeeting/",{id:t},function(n){n.substring(5,0)=="error"?alert(n):window.location=n}),!1):(i=$("input[pid="+t+"].atck"),i[0]&&(i[0].scrollIntoView(),i.click()),!1)}),$("#wandtarget").focus(),$.extraEditable=function(){$(".editarea").editable("/Meeting/EditExtra/",{type:"textarea",submit:"OK",rows:5,width:200,indicator:'<img src="/images/loading.gif">',tooltip:"Click to edit..."}),$(".editline").editable("/Meeting/EditExtra/",{indicator:"<img src='/images/loading.gif'>",tooltip:"Click to edit...",style:"display: inline",width:200,height:25,submit:"OK"})},$("#newvalueform").dialog({autoOpen:!1,buttons:{Ok:function(){var i=$("#multiline").is(":checked"),n=$("#fieldname").val(),t=$("#fieldvalue").val();n&&$.post("/Meeting/NewExtraValue/"+$("#meetingid").val(),{field:n,value:t,multiline:i},function(n){n.startsWith("error")?alert(n):($("#extras > tbody").html(n),$.extraEditable()),$("#fieldname").val("")}),$(this).dialog("close")}}}),$("#newextravalue").live("click",function(n){n.preventDefault();var t=$("#newvalueform");t.dialog("open")}),$("a.deleteextra").live("click",function(n){return n.preventDefault(),confirm("are you sure?")&&$.post("/Meeting/DeleteExtra/"+$("#meetingid").val(),{field:$(this).attr("field")},function(n){n.startsWith("error")?alert(n):($("#extras > tbody").html(n),$.extraEditable())}),!1})})