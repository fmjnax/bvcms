$(function(){$.RefreshPage=function(){var n=$("#form"),t=n.serialize();$.blockUI(),$.post("/OrgMembers/List",t,function(t){$(n).html(t).ready(function(){$("table.grid > tbody > tr:even",n).addClass("alt"),$.unblockUI(),$(".bt").button(),$("#manage select").css("width","100%")})})},$(".bt").button(),$("select").css("width","100%"),$("#form").delegate("#ProgId","change",$.RefreshPage),$("#form").delegate("#DivId","change",$.RefreshPage),$("#form").delegate("#SourceId","change",$.RefreshPage),$("#form").delegate("#TargetId","change",$.RefreshPage),$("#form").delegate("#MembersOnly","change",$.RefreshPage),$("#form").delegate("#Grades","change",$.RefreshPage),$("#form").delegate("#move","click",function(n){n.preventDefault();var t=$("#form"),i=t.serialize();$.blockUI(),$.post("/OrgMembers/Move",i,function(n){$(t).html(n).ready(function(){$("table.grid > tbody > tr:even",t).addClass("alt"),$.unblockUI(),$.growlUI("Move","Completed"),$(".bt").button(),$("#manage select").css("width","100%")})})}),$("#SelectAll").live("click",function(){$(this).attr("checked")?$("table.grid input[name='List']").attr("checked",!0):$("table.grid input[name='List']").removeAttr("checked")}),$.blockUI.defaults.growlCSS={width:"350px",top:"40%",left:"35%",right:"10px",border:"none",padding:"5px",opacity:"0.7",cursor:null,color:"#fff",backgroundColor:"#000","-webkit-border-radius":"10px","-moz-border-radius":"10px"},$.growlUI=function(n,t,i){var r=$('<div class="growlUI"><\/div>');n&&r.append("<h1>"+n+"<\/h1>"),t&&r.append("<h2>"+t+"<\/h2>"),i==undefined&&(i=3e3),$.blockUI({message:r,fadeIn:400,fadeOut:700,centerY:!1,timeout:i,showOverlay:!1,css:$.blockUI.defaults.growlCSS})},$("#EmailNotices").live("click",function(n){n.preventDefault();var t=$("#form"),i=t.serialize();return $.post("/OrgMembers/EmailNotices",i,function(n){$(t).html(n).ready(function(){$("table.grid > tbody > tr:even",t).addClass("alt"),$.growlUI("Email Notices","emails sent"),$(".bt").button(),$("#manage select").css("width","100%")})}),!1}),$("#form").submit(function(){return!1}),$("form table.grid a.sortable").live("click",function(){var n=$(this).text(),i=$("#Sort").val(),t;$("#Sort").val(n),t=$("#Dir").val(),i==n&&t=="asc"?$("#Dir").val("desc"):$("#Dir").val("asc"),$.RefreshPage()})})