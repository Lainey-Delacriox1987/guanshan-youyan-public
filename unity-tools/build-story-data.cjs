// Neutral story source. Generate the Unity snapshot without modifying the original H5.
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const line = (speaker, text) => ({speaker, text});
const actions = [];
function action(id,title,room,x,required,gives,lines,choices=[],correct=0) {
  actions.push({id,title,room,x,required,gives,lines,choices,correct});
}
action('prologue','一封未署名的信','',0,[],['letterReceived'],[
 line('旁白','雾港。你是林见，第一次跟随许岚参与现场核查。'),
 line('来信摘要','“我是顺达仓库的收发员。前两天，SD-0615的原始入库联写着‘日用收纳盒’，出库复核记录里却是成箱乳霜。我经手过收货，但不知道两处品名为什么对不上。原始入库联和复核影像还在，请求帮忙核对。”'),
 line('旁白','一封信只能提供方向。你们今天要核对的一票货，也计划送往顺达仓库。')]);
action('intro','先从哪里查起？','harbor',5.4,['letterReceived'],['introDone'],[
 line('许岚','信里说了什么，先记着。我们先核对眼前这份货单。'),
 line('林见','从哪里看？'),
 line('许岚','先进右边的查验办公室，确认申报和复核记录说的是同一票货，再比较计量口径。')]);
action('weight','核对重量记录','inspection',8.1,['letterReceived'],['weight'],[
 line('记录','第一日 16:40｜票号 QJ-017\n申报品名：收纳盒；申报净重 800 kg。\n同票复核净重 1260 kg；计量口径一致。'),
 line('林见','同一票、同一计量口径，多了460公斤。重量差值得报告，但它还解释不了原因。')],['净重多出 460 kg','净重多出 2060 kg','仅凭重量差就能确定组织者'],0);
action('seal','观察交接标签','harbor',11.7,['letterReceived'],['seal'],[
 line('林见','标签边缘有两层胶痕，编号可以辨认。先记录为待解释的外观观察。'),
 line('许岚','得看交接记录怎么解释。胶痕本身不能证明有人擅自启封。')]);
action('reportWeight','报告重量差异','*',0,['weight'],['inspectionReady'],[
 line('林见','QJ-017 的两份记录口径一致，净重差460公斤。'),
 line('许岚','重量还解释不了品名。后续查验已经安排，我们去堆场查验点记录结果。'),
 line('许岚','另外，交接记录说明标签覆盖了旧胶痕。这份核验说明不能证明擅自启封；也不代表其他异常已解释。')]);
action('cargo','查看查验实物','harbor',15.1,['weight','inspectionReady'],['cargo'],[
 line('查验记录','第一日｜当前票 QJ-017\n申报：收纳盒。\n实物：乳霜等化妆品；生产批次 K17。'),
 line('林见','申报的是收纳盒，里面却是乳霜。'),
 line('许岚','票号、品名、批次分别记清。K17 是生产批次，不是一票货物的唯一身份。')]);
action('handover','询问计划去向','harbor',7.5,['letterReceived'],['handover'],[
 line('值班员','这票还没出去。计划送顺达仓库，联系人是陈渡。'),
 line('值班员','这是计划交接的副本。附件里还有他前两天的仓储往来，作业单 SD-0615。'),
 line('林见','“计划送达”不等于已经出港。附件里的旧出货，要另查当时的记录。')]);
action('departure','带着记录出发','harbor',2.4,['portMismatch','portDestination'],['act1Complete'],[
 line('许岚','走吧，去顺达仓库核对附件指向的旧出货记录。'),
 line('旁白','QJ-017 仍留在港区，由负责人员继续处理。你们带走的是记录副本。'),
 line('旁白','信里提到的那次出货，发生在两天前。\n第一幕《多出的460公斤》完成。')]);
const book={version:2,chapter:1,title:'关山有眼 · 雾港来信',subtitle:'第一幕　多出的460公斤',
 rooms:[{id:'harbor',title:'雾港 · 查验区',artKey:'walk-harbor',floor:-1.35,spawn:4.1},
 {id:'inspection',title:'查验办公室',artKey:'walk-inspection',floor:-2.5,spawn:3.4}],
 materials:[
 {id:'weight',title:'QJ-017 · 重量比对',kind:'记录',text:'第一日16:40，同票、同一净重口径：申报收纳盒800kg，复核1260kg，差460kg。重量差不能单独解释成因。'},
 {id:'seal',title:'标签边缘 · 胶痕观察',kind:'待核观察',text:'你亲自观察到两层胶痕。仅凭外观不能证明擅自启封。'},
 {id:'cargo',title:'QJ-017 · 查验实物',kind:'记录',text:'当前票实物为乳霜等化妆品，生产批次K17，与申报收纳盒不一致。K17不能单独证明同一票货物。'},
 {id:'handover',title:'计划交接 · 历史往来附件',kind:'记录',text:'当前QJ-017计划送顺达仓库，联系人陈渡。附件提到两天前的历史仓储作业SD-0615。计划不等于已出港；历史作业另行核对。'}],
 actions,
 relations:[
 {id:'portMismatch',title:'眼前这票货，有什么异常？',required:['weight','cargo'],evidence:['weight','cargo'],correct:1,
 claims:['封条编号一致，已经解释全部异常','同票申报品名与查验实物不一致','重量差已说明是谁组织的'],
 success:'申报与实物不一致值得继续核查；差异的原因和责任仍待调查。',feedback:'把同一票的申报字段与实物记录放在一起；外观或重量差不能替代对组织者的调查。'},
 {id:'portDestination',title:'准备去仓库确认什么？',required:['portMismatch','handover'],evidence:['handover'],correct:0,
 claims:['核对附件指向的旧出货记录','跟着QJ-017去看它正在拆包','同为K17就能证明历史货物手续有问题'],
 success:'计划交接与历史往来附件说明了下一站的核查方向。眼前这票货仍留在港口。',feedback:'分清当前计划交接和两天前的仓储往来。下一步是核对旧记录，不是追随当前票出港。'}],
 hints:[
 {required:'weight',question:'先确认两张记录说的是同一票货。',type:'比一比票号、净重口径和数值。',specific:'进入右侧办公室，查看台面上的重量记录。'},
 {required:'inspectionReady',question:'发现异常以后，该请谁组织后续核查？',type:'带着重量比对向许岚报告。',specific:'走近许岚，选择“报告重量差异”。'},
 {required:'cargo',question:'重量还不能回答里面究竟是什么。',type:'查验已安排，去看当前票的实物记录。',specific:'到堆场中部偏右的查验货箱。'},
 {required:'handover',question:'下一站的依据在哪里？',type:'问清计划交接与历史往来。',specific:'在堆场走近值班员，听完他提供资料的对话。'},
 {required:'portMismatch',question:'哪两份记录解释不了彼此？',type:'向许岚复盘当前异常，出示申报与实物。',specific:'走近许岚，选“核对当前异常”，点选重量比对与查验实物。'},
 {required:'portDestination',question:'去仓库需要一个具体的调查问题。',type:'向许岚出示有下一站信息的资料。',specific:'选择“确认下一站”，核对历史往来附件。'},
 {required:'act1Complete',question:'记录已整理好，可以出发。',type:'QJ-017仍留港，你们只带记录副本。',specific:'走到堆场左侧接驳车。'}]};
const json=JSON.stringify(book,null,2)+'\n';
fs.mkdirSync(path.join(root,'story'),{recursive:true});
fs.writeFileSync(path.join(root,'story/port-act1-v2.json'),json);
fs.writeFileSync(path.join(root,'unity/GuanshanYouyan2D/Assets/GuanshanYouyan/Data/PortAct1V2.json'),json);
console.log('Generated identical neutral and Unity story snapshots.');
