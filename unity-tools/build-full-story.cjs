const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'..');
const b=JSON.parse(fs.readFileSync(path.join(root,'story/port-act1-v2.json'),'utf8'));
b.id='mist-story-v2-full';b.subtitle='四幕调查故事';
b.chapters=[{title:'多出的460公斤',time:'第一日 · 16:40',question:'眼前这票货，究竟哪里不一致？'},
{title:'对不上的品名',time:'第一日 · 18:20',question:'同一次作业的入库品名，为什么与出库复核记录不同？'},
{title:'他说自己没来过',time:'第二日 · 17:10',question:'陈渡的说法能解释实际交接吗？'},
{title:'把话放回记录里',time:'第三日 · 09:00',question:'哪些说法站不住，哪些事情仍待核实？'}];
const h=(id,title,kind,x,target='',actor=-1)=>({id,title,kind,x,target,actor});
const room=(id,act,title,floor,mentorX,spots,actors=[],strip=.12)=>({id,act,title,floor,mentorX,spawn:3.4,spots,actors,strip});
const npc=(actor,x)=>({actor,x});
b.rooms=[
room('harbor',0,'雾港 · 查验区',-1.35,5.4,[h('departure','接驳车','travel',2.4,'warehouse'),h('mentor','许岚','mentor',5.4),h('handover','值班员','action',7.5),h('seal','交接标签','action',11.7),h('cargo','查验货箱','action',15.1),h('door','查验办公室','door',21.5,'inspection')],[npc(4,5.4),npc(5,7.5)],.25),
room('inspection',0,'查验办公室',-2.5,12.7,[h('door','返回堆场','door',2.4,'harbor'),h('weight','重量记录','action',8.1),h('mentor','许岚','mentor',12.7)],[npc(4,12.7)]),
room('warehouse',1,'顺达仓库 · 分拣区',-2.5,17.0,[h('back','港口接驳车','travel',1.5,'harbor'),h('parcels','原始入库联与子运单','action',6.3),h('worker','老赵','worker',10.8),h('mentor','许岚','mentor',17),h('door','楼梯 · 上调度室','door',21.8,'dispatch'),h('advance','去青禾镇','travel',23.1,'town')],[npc(6,10.8),npc(4,17)]),
room('dispatch',1,'顺达仓库 · 二层调度室',-2.5,17.0,[h('door','楼梯 · 回分拣区','door',2.4,'warehouse'),h('camera','出库复核与装车影像','action',7.2),h('dispatch','目的地修改登记','action',12.9),h('mentor','许岚','mentor',17)],[npc(4,17)]),
room('town',2,'青禾镇 · 禾记美妆街巷',-2.5,18.5,[h('back','返回仓库','travel',1.0,'warehouse'),h('courier','周叔','action',3.6),h('door','禾记美妆','door',10.1,'records'),h('shelf','橱窗与到货清单','action',13.7),h('denial','联系陈渡','action',17.0),h('mentor','许岚','mentor',18.5),h('advance','前往调查中心','travel',22.6,'interview')],[npc(8,3.6),npc(4,18.5)]),
room('records',2,'禾记美妆 · 店内与后室',-2.5,20.4,[h('door','返回街巷','door',2.4,'town'),h('shopShare','苏禾','shop',6.5),h('account','历史结算账簿','action',11.8),h('wildlife','待核物件','ecology',16),h('mentor','许岚','mentor',20.4)],[npc(7,6.5),npc(4,20.4)]),
room('interview',3,'调查中心 · 询问室',-2.5,5.3,[h('back','返回青禾镇','travel',1.0,'town'),h('mentor','许岚','mentor',5.3),h('suspect','陈渡','inquiries',12.8),h('door','电梯 · 案卷室','door',21.8,'archive')],[npc(4,5.3),npc(9,12.8)]),
room('archive',3,'调查中心 · 案卷室',-2.5,19.1,[h('door','电梯 · 询问室','door',2.4,'interview'),h('board','已核对关系','board',7.8),h('notes','调查手记','notes',13.2),h('mentor','许岚 · 开始复盘','mentor',19.1)],[npc(4,19.1)])];
// Match actual generated walking planes rather than assuming every picture has the same baseline.
for(const r of b.rooms){if(r.act>0){r.floor=-2.1;r.strip=.20;}if(r.id==='town'){r.floor=-1.25;r.strip=.30;}if(r.id==='archive')r.floor=-1.95;}
const wh=b.rooms.find(r=>r.id==='warehouse');wh.mentorX=14.1;wh.actors.find(a=>a.actor===4).x=14.1;wh.spots.find(s=>s.id==='mentor').x=14.1;wh.spots.find(s=>s.id==='door').x=16.5;
b.rooms.find(r=>r.id==='records').spots.push(h('backdoor','后门 · 返回街巷','door',22.0,'town'));
for(const a of b.actions){a.act=0;a.actor=a.id==='handover'?5:a.id==='intro'||a.id==='reportWeight'||a.id==='departure'?4:3;a.feedback='请先比较同票、同口径记录，再计算净重差。';}
for(const r of b.relations){r.act=0;r.room='*';r.target='mentor';r.actor=4;}
const depart=b.actions.find(a=>a.id==='departure');depart.destination='warehouse';depart.lines[2].text='你们带着记录前往顺达。这里要核对的是两天前的 SD-0615，当前 QJ-017 并没有随车离港。';
const L=(speaker,text)=>({speaker,text});
function A(id,title,act,room,x,required,gives,lines,opts={}) {b.actions.push({id,title,act,room,x,required,gives,lines,choices:[],correct:0,actor:3,feedback:'请回看记录的来源与字段。',...opts});}
function M(id,title,kind,text,act){b.materials.push({id,title,kind,text,act});}
for(const m of b.materials)m.act=0;
M('parcels','SD-0615 · 原始入库联与子运单','记录','调查开始前两日，原始入库联的作业单号为SD-0615，登记品名为日用收纳盒。A/B/C子运单均关联该主单，只用于核对作业归属和去向；姓名不同或集中送达本身不构成异常。境内作业单不是进口手续证明。',1);
M('camera','SD-0615 · 出库复核与装车影像','记录','调查开始前两日，出库复核影像归档在SD-0615下，开箱照片显示成箱乳霜，并记录随后分为A/B/C三个包裹组装车。它与同作业单原始入库联的收纳盒品名不符，原因待查；不能仅凭影像确定谁填写品名或谁了解进口手续。',1);
M('dispatch','SD-0615 · 目的地修改登记','记录','调查开始前两日，原分散交接地点被改为青禾镇17号集中交接；登记有陈渡签字。调整目的地本身不能直接判为违法。',1);
M('worker','老赵 · 经手说明与来信回收','经手陈述','老赵经手的是调查开始前两日SD-0615的收发记录，未代其他人确认全部行为。他确认自己向线索受理渠道提交了开场来信。',1);
M('shelf','禾记美妆 · 商品与到货匹配','记录','店内商品批次K17，到货清单列SD-0615及A/B/C子单范围，日期为调查开始前一日。主单和到货清单相符，才支持与历史作业的对应。',2);
M('account','SD-0615 · 店铺结算记录','记录','店主提供的账簿列SD-0615供货，结算联系人陈渡。仅记名不足以认定资金最终归属，仍需核对付款与结算关系。',2);
M('courier','SD-0615 · 签收及交接附件','记录','周叔提供调查开始前一日的交接记录和附件，地点青禾镇17号，陈渡参与现场交接。只说明该次交接，不说明进口环节或全部责任。',2);
M('S1','S1 · 陈渡首次电话陈述','陈述','第二日17:10，在许岚在场的电话中，陈渡说：“我负责联系运输，没到青禾镇现场，也不清楚店里具体怎么收货。”陈述确被记录，内容仍待核对。',2);
M('wildlife','待核物件 · 独立生态线索','旁支观察','店内一块所谓护身片。没有鉴定结果，材质、物种与来源均无法确认，不能并入K17主线或用作人员组织关系的证据。',2);
A('workerIntro','先问老赵经手范围',1,'warehouse',10.8,['act1Complete'],['workerMet'],[L('老赵','今天没有拆你们在港口核查的 QJ-017。你们要找的是前两天的 SD-0615。'),L('老赵','分拣台留着原始入库联和子运单，楼上有出库复核影像、调度登记。品名为什么不一样，我也想弄清。'),L('林见','先核对日期和作业单，确认比较的是同一次作业。')],{actor:6});
A('parcels','核对原始入库联与子运单',1,'warehouse',6.3,['act1Complete'],['parcels'],[L('记录','日期：调查开始前两日\n原始入库联：SD-0615；登记品名：日用收纳盒。\n子运单A/B/C均关联SD-0615。'),L('林见','先确定作业归属，再与出库复核记录比较。收件姓名和集中送达本身不能说明货有问题。')],{choices:['收件姓名不同，所以货物可疑','原始入库联登记为收纳盒，三个子单同属SD-0615','仅凭K17认定已经证实同一票进口货'],correct:1,feedback:'这里核对的是原始登记和作业归属。是否存在品名差异，要与同次出库复核记录比较，不能靠收件姓名判断。',inspect:'parcels'});
A('camera','读取出库复核与装车影像',1,'dispatch',7.2,['act1Complete'],['camera'],[L('记录','日期：调查开始前两日\n归档作业单：SD-0615。\n出库复核的开箱照片显示成箱乳霜；后续影像记录A/B/C包裹组装车。'),L('许岚','先确认影像对应的日期和作业单。若同一作业的入库联记收纳盒，出库复核却是乳霜，就要查清是登记差错、记录对应错误，还是另有原因。')],{inspect:'camera'});
A('dispatch','比较目的地修改登记',1,'dispatch',12.9,['act1Complete'],['dispatch'],[L('记录','SD-0615 · 调查开始前两日\n原分散交接地点 → 青禾镇17号集中交接\n修改签字：陈渡。'),L('林见','这解释了计划去向。实际是否在那里交接，还得找签收记录。')],{inspect:'dispatch'});
A('worker','拿具体记录回访老赵',1,'warehouse',10.8,['parcels','camera','dispatch'],['worker','letterIdentity'],[L('林见','SD-0615原始入库联写收纳盒，出库复核却记乳霜。你经手的是哪一步？'),L('老赵','我按送来的单据做收货登记，没有拆箱。出库复核是后来做的，我不能替复核人员解释照片之外的事。'),L('老赵','那封信是我递的。我翻到两份记录时发现品名对不上，所以把原始入库联留下来，请你们核对。'),L('许岚','我们会把原始记录、形成时间和你的经手范围分别核实。谢谢你保留资料。')],{actor:6});
A('warehouseDeparture','去核对实际交接',1,'warehouse',23.1,['historicalRoute','worker','letterIdentity'],['act2Complete'],[L('许岚','同次作业的品名记录对不上，原因还没查清。调度登记指向青禾镇17号，我们去核对那里实际收到的货和交接资料。'),L('旁白','你们带着原始入库联、出库复核和调度记录的副本前往青禾镇。集中交接只说明去向；陈渡的具体参与，还需要下一站的材料。')],{actor:4,destination:'town'});
A('shopShare','向苏禾说明来意',2,'records',6.5,['act2Complete'],['shopRecordsShared'],[L('苏禾','供货人说这批商品没问题，我也担心会影响顾客。你们要核对哪一次？'),L('林见','我们想看 SD-0615 的到货与结算记录，只核对这次具体往来。'),L('苏禾','账簿和到货清单都在这里，可以查。'),L('许岚','先把资料提供的范围记清。配合提供资料，不等于具体责任已经查明。')],{actor:7});
A('shelf','核对商品与到货清单',2,'town',13.7,['act2Complete','shopRecordsShared','parcels'],['shelf'],[L('林见','橱窗商品也是 K17。但批次相同，还不能直接认成那次出货。'),L('记录','店主提供的到货清单：调查开始前一日；SD-0615；子单范围A/B/C。'),L('林见','主作业单和到货清单互相对应，才支持这次历史流转联系。')],{choices:['只看K17便认定同一票货','把到货清单SD-0615与仓库主子单核对','进口商品出现在商店就能证明违法'],correct:1,feedback:'批次相同只提供方向，请把到货清单的作业单号和子单范围与仓库记录核对。',inspect:'shelf'});
A('account','查看历史结算账簿',2,'records',11.8,['act2Complete','shopRecordsShared'],['account'],[L('记录','SD-0615 · 调查开始前一日的结算记录\n涉及本次供货；结算联系人：陈渡。'),L('许岚','记名支持核查联系，不能直接说明全部资金的最终归属。')],{inspect:'account'});
A('denial','在许岚在场时联系陈渡',2,'town',17.0,['act2Complete'],['S1','denialRecorded'],[L('许岚','陈先生，我们想核对 SD-0615 在青禾镇的交接经过。'),L('陈渡','我负责联系运输，没到青禾镇现场，也不清楚店里具体怎么收货。'),L('林见','这段陈述先记为 S1，之后再与实际记录核对。')],{actor:9});
A('courier','向周叔核对交接',2,'town',3.6,['act2Complete'],['courier'],[L('周叔','我记得那次送货，但记忆说不清全部细节。这里有交接记录和附件。'),L('记录','SD-0615 · 调查开始前一日\n地点：青禾镇17号。陈渡参与现场交接。'),L('周叔','我能说明的是这次送货和交接，其他环节得看其他记录。')],{actor:8});
A('wildlife','观察待核物件',2,'records',16,['act2Complete','shopRecordsShared'],['wildlife','observed'],[L('林见','这块所谓护身片是什么材质？目前看不出来。'),L('许岚','单独记下观察。没有鉴定，不写具体物种，也不把它当作化妆品主线的材料。')],{inspect:'wildlife'});
A('wildlifeSource','问苏禾物件来源',2,'records',6.5,['observed'],['sourceAsked'],[L('苏禾','这块东西是别人以前留下的，我拿不出确切的来源记录。'),L('许岚','来源仍无法确认。把她的回答和不能确认的部分分别记下。')],{actor:7});
A('wildlifeRefer','将观察另行提交核验',2,'records',20.4,['observed','sourceAsked'],['referred'],[L('林见','这条观察与来源问题单独整理，不参与K17材料关联。'),L('许岚','已交工作人员按流程另行核验。这里没有鉴定结果，材质和物种仍待确认。')],{actor:4});
A('townDeparture','安排进一步询问',2,'town',22.6,['shelf','account','receiptContradiction','participantScope'],['act3Complete'],[L('林见','签收与“没到现场”的说法对不上，调度和结算也显示额外联系。'),L('许岚','我来联系后续询问安排。明早在调查中心，让陈渡逐项解释。'),L('旁白','第三日09:00。经工作人员安排，陈渡到调查中心接受进一步询问，林见协助记录。')],{actor:4,destination:'interview'});
A('receiptInquiry','核对“没有到现场”',3,'interview',12.8,['act3Complete','S1','courier','denialRecorded'],['receiptQuestioned'],[L('林见','您在 S1 中说没到现场。SD-0615 的签收记录和附件却记载您参与交接，请核对。'),L('陈渡','这份签收我需要再核对，可能是我帮忙接了一次。'),L('许岚','把补充解释记下来。现有陈述与交接记录不一致，不能跳成全部行为的认定。')],{actor:9,mode:'inquiry',evidence:['S1','courier']});
A('roleInquiry','核对“只联系运输”',3,'interview',12.8,['act3Complete','dispatch','courier','account'],['roleQuestioned'],[L('林见','调度登记有您修改地址的签字，交接与结算也留下联系。您说的“只联系运输”，包括哪些具体环节？'),L('陈渡','地址是我改的，账目也经过我，但不代表你说的所有事情都是我安排。'),L('许岚','继续核查实际分工、资金和知情范围。回应并不自动等于认罪，也不能省略其他环节。')],{actor:9,mode:'inquiry',evidence:['dispatch','courier','account']});
A('scopeReview','复盘其他人员的责任边界',3,'interview',5.3,['receiptQuestioned','roleQuestioned','worker','shopRecordsShared'],['scopeReviewed'],[L('许岚','老赵、苏禾和周叔的记录，都只说明各自经手的范围。你准备怎么写？'),L('林见','现有材料不足以直接认定或排除其他人员责任，要按各自记录分别核查。'),L('许岚','对。去案卷室，把已知事实、推测和待核事项分开。')],{actor:4,choices:['提供了资料的人都已经免责','签收人承担所有环节责任','按各自材料分别核查，不直接认定或排除'],correct:2,feedback:'配合、签收和经手记录各自只支持有限范围。不能据此直接认定或排除所有人的责任。'});
function R(id,title,act,required,evidence,claims,correct,success,feedback){b.relations.push({id,title,act,room:'*',target:'mentor',actor:4,required,evidence,claims,correct,success,feedback});}
R('historicalRoute','品名记录对不上，下一站查什么？',1,['parcels','camera','dispatch'],['parcels','dispatch'],['不同姓名集中收货就说明走私','沿作业单与调度记录，核对青禾镇实际收到的货与交接','境内品名差异已经证明此前进口手续有问题'],1,'同次作业的入库与出库品名不一致，原因待查。原始入库联及其子运单、调度登记提供追查实际收货的方向，青禾镇17号是核对地点，不是仅凭集中收货认定的可疑地点。','先分清异常与路径：要核对的是品名记录为何不一致；作业单和调度记录用来找对应货物与实际交接，不能直接证明此前进口手续。');
R('receiptContradiction','签收与哪句陈述冲突？',2,['denialRecorded','S1','courier'],['S1','courier'],['记录与“没到青禾镇现场”的说法存在冲突','一份签收已经证明全部责任','电话否认足以推翻所有记录'],0,'S1与SD-0615交接记录存在矛盾，需要向陈渡出示材料并听取解释。','对照他实际说过的话和具体签收记录。矛盾成立也不能替代对其他环节的核查。');
R('participantScope','陈渡的参与，是否只是运输？',2,['dispatch','courier','account','shelf','receiptContradiction'],['dispatch','account'],['店主提供账簿就能排除店主责任','签收人承担所有进口责任','调度、交接、结算提示多环节参与，需进一步核查'],2,'现有材料支持继续核查陈渡多环节参与，不直接确定全部资金归属或其他人的责任。','把调度和结算放回已核对的交接链。参与联系与全部责任之间，还缺少进一步调查。');
b.reports=[
{id:'current',title:'当前异常',question:'先说眼前这票QJ-017。你已经核对出什么？',required:['weight','cargo'],evidence:['weight','cargo'],claims:['申报品名与查验实物不一致，成因仍待核查','净重差足以确定全部参与人员','封条有解释，其他异常都不再需要查'],correct:0,feedback:'当前票的核心是申报与实物不一致；净重差或标签说明都不能代替其他问题的核查。'},
{id:'history',title:'历史流转',question:'再说SD-0615。哪一部分有记录，哪一部分尚不能推定？',required:['historicalRoute','shelf','courier','account'],evidence:['parcels','shelf'],claims:['同为K17即可认定同一票货的违法进口','境内流转记录已经证明此前进口手续有问题','主单、到货与交接形成历史路径，此前进口手续另查'],correct:2,feedback:'K17是生产批次，SD-0615是仓库作业号。历史流转路径不能自动证明此前进口环节。'},
{id:'people',title:'人员参与',question:'结合陈渡的回应，哪些参与关系需要重点核查？',required:['receiptQuestioned','roleQuestioned','scopeReviewed'],evidence:['dispatch','account'],claims:['签收人承担所有责任，其他人都已排除','陈渡涉及多环节联系，其他人员按各自材料分别核查','只要回应不认同，现有记录就没有价值'],correct:1,feedback:'签收与配合不等于全部责任或免责。结合调度、交接、结算与询问回应，分别记录参与范围和待核事项。'},
{id:'next',title:'下一步',question:'最后，你要提交什么，又准备留下什么问题？',required:['act3Complete','scopeReviewed'],evidence:['worker','courier'],claims:['把有依据的记录与待核事项提交，生态观察按实际状态单列','把推测写成定论，立即宣告所有人的责任','只提交人物猜测，不保留记录来源'],correct:0,feedback:'调查建议应留下记录来源、询问回应和未知事项；生态旁支只写实际完成的观察与核验步骤。'}];
const H=(required,question,type,specific,act)=>({required,question,type,specific,act});
for(const hint of b.hints)hint.act=0;
b.hints=b.hints.filter(x=>x.required!=='act1Complete');
b.hints.push(H('act1Complete','下一站的问题已经明确。','当前票留港，只带记录副本去查旧往来。','回堆场左侧接驳车，出发去顺达仓库。',0),
 H('parcels','原始入库联登记的是什么？','先核对日期、主单号和子单范围。','到仓库分拣台查看原始入库联与子运单。',1),H('camera','同次出库复核记录的又是什么？','将归档影像与入库联比较，区分记录差异与成因。','从右侧楼梯进入二层调度室，看出库复核与装车影像。',1),H('dispatch','登记中的货物准备送到哪里？','核对修改前后地址和签字，追查去向。','查看二层中间的目的地修改登记。',1),H('worker','两处品名对不上，老赵经手了哪一步？','回访老赵，核对原始记录和经手范围。','回楼下和老赵谈SD-0615的入库联、复核影像与来信。',1),H('historicalRoute','下一站要去核实什么？','向许岚出示原始入库联与子运单、目的地修改登记。','向许岚完成“品名记录对不上，下一站查什么？”。',1),H('act2Complete','记录差异还需要实际收货资料核对。','沿已记录的去向前往青禾镇。','走到仓库右侧出口，前往青禾镇。',1),
 H('shopRecordsShared','先取得对方愿意提供的资料。','向店主说明本次核对的具体往来。','进入禾记美妆，走近苏禾说明来意。',2),H('shelf','同批次还不足以对应一次出货。','比较到货清单与仓库主子单。','到街面橱窗核对SD-0615到货清单。',2),H('account','款项结算留下了哪种联系？','只按账簿能支持的范围记录。','在店内查看结算账簿。',2),H('denialRecorded','现在还不知道陈渡如何解释交接。','在许岚在场时联系陈渡。','到街巷右侧电话点，记录首次陈述S1。',2),H('courier','实际交接不能只依靠记忆。','向承运人核对记录与附件。','向街巷左侧周叔询问交接。',2),H('receiptContradiction','哪一句话与具体记录对不上？','对照S1和签收记录。','向许岚出示S1与SD-0615交接记录。',2),H('participantScope','“只运输”是否完整解释了材料？','把调度和结算放回交接链。','向许岚核对多环节参与，出示调度与账簿。',2),H('act3Complete','矛盾要给对方逐项解释的机会。','许岚联系工作人员安排后续询问。','走到街巷右侧出口，前往调查中心。',2),
 H('receiptQuestioned','在S1与签收之间，还缺一次实际核对。','向陈渡出示记录，听完他的补充说明。','在询问室选择“没有到现场”，出示S1与签收。',3),H('roleQuestioned','另一组问题是“只联系运输”。','同时核对调度、交接和结算。','向陈渡出示调度、签收与账簿，听完回应。',3),H('scopeReviewed','其他人的责任也不能被一句话替代。','向许岚区分已经知道和仍待核查的部分。','回询问室许岚处，复盘其他人员的责任边界。',3),H('reportSubmitted','把事实和未知分别写进建议。','在案卷室向许岚开始四段复盘。','乘电梯去案卷室，走到右侧许岚身边开始复盘。',3));
b.knowledge=[
{title:'品名要回到实物核对',unlock:'act1Complete',text:'官方历史通报介绍过将化妆品申报为化纤布的案件。本作“收纳盒”和QJ-017是虚构设定；玩家核对的是当前记录，不能把现实案件的结论直接套给游戏人物。',source:'济宁市打击走私办公室转引海关总署 · 2018-12-17',url:'https://www.jining.gov.cn/art/2018/12/17/art_77109_2765580.html'},
{title:'拆包不等于已经查明跨境行为',unlock:'act2Complete',text:'澳门海关2020年的通报涉及店铺和分批携带货物出境。本作SD-0615是境内历史仓库作业；主单、运输与签收支持流转路径，不能仅按包裹大小判断此前进口环节。',source:'澳门特别行政区政府 · 澳门海关 · 2020-08-05',url:'https://www.gov.mo/zh-hans/news/302909/'},
{title:'不明物件需要独立核验',unlock:'act3Complete',text:'香港海关2024年通报过一宗穿山甲鳞片案件的判刑情况，适用当地法律。这不证明游戏中物件的材质、来源或物种。观察、询问来源和提交核验分别记录，不能相互替代。',source:'香港海关 · 2024-02-28',url:'https://www.customs.gov.hk/sc/customs-announcement/press-release/index_id_4088.html?m=02&p=1&y=2024'},
{title:'真实办事信息到官方平台查询',unlock:'reportSubmitted',text:'海关政务服务平台提供办事指南与咨询信息。游戏里的受理与回信属于剧情，不会向现实机构发送调查资料。需要真实办理信息时，可以打开官方来源自行查询。',source:'海关政务服务一体化平台 · 办事指南',url:'https://online.customs.gov.cn/static/pages/guides/000629002001/000629002001.html'}];
const text=JSON.stringify(b,null,2)+'\n';
fs.writeFileSync(path.join(root,'story/story-v2.json'),text);
fs.writeFileSync(path.join(root,'unity/GuanshanYouyan2D/Assets/GuanshanYouyan/Data/StoryV2.json'),text);
console.log('Generated full story: '+b.rooms.length+' rooms, '+b.actions.length+' actions, '+b.relations.length+' relations, '+b.reports.length+' review dialogues.');
