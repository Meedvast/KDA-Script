using System;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Script;
using KodakkuAssist.Module.GameEvent.Struct;
using KodakkuAssist.Module.Draw;
using KodakkuAssist.Module.Draw.Manager;
using KodakkuAssist.Data;
// using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Numerics;
using Newtonsoft.Json;
using System.Linq;
using System.ComponentModel;
using System.Xml.Linq;
using Dalamud.Utility.Numerics;
// using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using KodakkuAssist.Module.GameOperate;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace MyScriptNamespace
{
    
    [ScriptType(name: "绝欧精装豪华版", territorys: [1122],guid: "e0bfb4db-0d38-909f-5088-b23f09b7585e", version:"0.0.0.15", author:"Karlin",note: noteStr,updateInfo: UpdateInfo)]
    public class OmegaProtocolUltimate
    {
        const string noteStr =
        """
        欧米茄验证绝境战(基于K佬原有脚本添加P5二三运，P6指路)
        感谢Usami提供的P5一运指路
        """;
        
        private const string UpdateInfo =
            """
            1. 修复P5二运踩塔指路错误。
            """;

        [UserSetting("P3_开场排队顺序")]
        public P3SortEnum P3_StackSort { get; set; }
        [UserSetting("P3_小电视打法")]
        public P3TVEnum P3_TV_Strategy { get; set; }
        
        [UserSetting("天剑颜色")]
        public ScriptColor ArrorColor { get; set; } = new() { V4 = new(1, 0, 0, 1) };
        [UserSetting("一天剑跟随人群")]
        public bool followCrowd { get; set; } = true;
        [UserSetting("箭头粗细")]
        public int ArrowScale { get; set; } = 1;    

        List<int> HtdhParty = [2, 0, 1, 4, 5, 6, 7, 3];
        double parse = 0;

        uint P1_BossId = 0;
        List<int> P1_点名Buff = [0, 0, 0, 0, 0, 0, 0, 0];
        List<Vector3> P1_TowerPos = [];
        DateTime P1_TowerTime= DateTime.MinValue;
        DateTime P1_FanTime = DateTime.MinValue;
        int P1_LineRound = 0;
        int P1_FireCount = 0;

        bool P2_PTBuffIsFar=false;
        List<int> P2_Sony= [0, 0, 0, 0, 0, 0, 0, 0];
        List<int> P2_Stack = [0, 0, 0, 0, 0, 0, 0, 0];
        Dictionary<uint,uint> P2_刀光剑舞连线 = [];

        int P3_ArmCount = 0;
        List <int> P3_StartBuff= [0, 0, 0, 0, 0, 0, 0, 0];
        bool P3_StartPreDone = false;
        bool P3_StartDone=false;
        List<int> P3_TVBuff = [0, 0, 0, 0, 0, 0, 0, 0];

        List<int> P4Stack = [];

        int P51_Eye = 0;
        List<int> P51_Buff = [0, 0, 0, 0, 0, 0, 0, 0];
        List<int> P51_Fist = [0, 0, 0, 0];
        bool P51_FistDone = false;


        int P52_OmegaMDir = 0;
        bool P52_OmegaFDirDone = false;
        int P52_OmegaFDir = 0;
        bool P5_SigmaBuffIsFar = false;
        Vector3 P52_Self_Pos;
        int P52_Self_Dir = 0;
        int[] P52_Towers = new int[16]{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
        Vector3[] P52_TowerPos = new Vector3[16];
        AutoResetEvent P52_semaphoreTowersWereConfirmed = new (false);
        private byte? P52_F_TransformationID;
        int P52_MarkType;
        int P53_MarkType;
        float P53_4_HW;
        int P5_3_MF = 0;
        private bool P5_TV_Support_enable = false;
        public bool P52_OmegaM_Skill = false;
        
        public int P5_3_MFT = 0; //第几组男女
        public List<int> MFTransformStates = [0,0,0,0]; // 变身状态列表
        public List<int> MFPositions = [0,0,0,0];       // 方位列表
        public int FPos1, FPos2;                    // F的两个方位
        public int Combo1, Combo2;                  // 组合技类型
        
        public enum Pattern { Unknown, InOut, OutIn }
        private Pattern _curPattern = Pattern.Unknown;
        private Vector3 MapCenter = new(100.0f, 0.0f, 100.0f);
        private int ArrowNum = 0;
        private int CannonNum = 0;
        public Vector3[] StepCannon = new Vector3[4];
        public int StepCannonIndex = 0;
        private bool isSet = false;
        System.Threading.AutoResetEvent ArrowModeConfirmed = new System.Threading.AutoResetEvent(false);
        // 0为先十字，1为先外圈
        public int arrowMode = -1;
        const string InOut = "InOut";
        const string OutIn = "OutIn";
        
        private static readonly Dictionary<(int, int, int, int), Vector3> P53SafePos = 
    new Dictionary<(int, int, int, int), Vector3>
    {
        {(0, 3, 1, 1), new Vector3(100, 0, 81)}, //A远
        {(0, 0, 2, 1), new Vector3(100, 0, 81)}, //A远
        {(0, 3, 1, 0), new Vector3(119, 0, 100)}, //B远
        {(0, 2, 0, 0), new Vector3(119, 0, 100)}, //B远
        {(0, 1, 3, 1), new Vector3(100, 0, 119)}, //C远
        {(0, 2, 0, 1), new Vector3(100, 0, 119)}, //C远
        {(0, 1, 3, 0), new Vector3(81, 0, 100)}, //D远
        {(0, 0, 2, 0), new Vector3(81, 0, 100)}, //D远
        {(2, 1, 3, 1), new Vector3(100, 0, 95.5f)}, //A近
        {(2, 2, 0, 1), new Vector3(100, 0, 95.5f)}, //A近
        {(3, 3, 1, 1), new Vector3(100, 0, 95.5f)}, //A近
        {(3, 0, 2, 1), new Vector3(100, 0, 95.5f)}, //A近
        {(2, 1, 3, 0), new Vector3(104.5f, 0, 100)}, //B近
        {(2, 0, 2, 0), new Vector3(104.5f, 0, 100)}, //B近
        {(3, 3, 1, 0), new Vector3(104.5f, 0, 100)}, //B近
        {(3, 2, 0, 0), new Vector3(104.5f, 0, 100)}, //B近
        {(2, 3, 1, 1), new Vector3(100, 0, 104.5f)}, //C近
        {(2, 0, 2, 1), new Vector3(100, 0, 104.5f)}, //C近
        {(3, 1, 3, 1), new Vector3(100, 0, 104.5f)}, //C近
        {(3, 2, 0, 1), new Vector3(100, 0, 104.5f)}, //C近
        {(2, 3, 1, 0), new Vector3(95.5f, 0, 100)}, //D近
        {(2, 2, 0, 0), new Vector3(95.5f, 0, 100)}, //D近
        {(3, 1, 3, 0), new Vector3(95.5f, 0, 100)}, //D近
        {(3, 0, 2, 0), new Vector3(95.5f, 0, 100)}, //D近
        {(1, 3, 1, 1), new Vector3(100, 0, 88)}, //A中
        {(1, 0, 2, 1), new Vector3(100, 0, 88)}, //A中
        {(1, 3, 1, 0), new Vector3(112, 0, 100)}, //B中
        {(1, 2, 0, 0), new Vector3(112, 0, 100)}, //B中
        {(1, 1, 3, 1), new Vector3(100, 0, 112)}, //C中
        {(1, 2, 0, 1), new Vector3(100, 0, 112)}, //C中
        {(1, 1, 3, 0), new Vector3(88, 0, 100)}, //D中
        {(1, 0, 2, 0), new Vector3(88, 0, 100)}, //D中
    };

        public enum P3SortEnum
        {
            HTDH,
            THD
        }
        public enum P3TVEnum
        {
            Normal,
            Static
        }
        
        private enum TopPhase
        {
            Init,                   // 初始
            P5A1_DeltaVersion,        // P5 一运
            P5A2_DeltaWorld,          // P5 二传
            P5B1_SigmaVersion,        // P5 二运
            P5B2_SigmaWorld,          // P5 二传
            P5C1_OmegaVersion,        // P5 三运
            P5C2_OmegaWorldA,         // P5 三传
            P5C3_OmegaWorldB,         // P5 四传
            P5D_BlindFaith,          // P5 盲信
        }
        private static List<string> _role = ["MT", "ST", "H1", "H2", "D1", "D2", "D3", "D4"];
        private const bool Debugging = false;
        private static readonly Vector3 Center = new Vector3(100, 0, 100);
        private static TopPhase _phase = TopPhase.Init;
        private volatile List<bool> _bools = new bool[20].ToList();
        private List<int> _numbers = Enumerable.Repeat(0, 20).ToList();
        private static List<ManualResetEvent> _events = Enumerable
            .Range(0, 20)
            .Select(_ => new ManualResetEvent(false))
            .ToList();
    
        private static PriorityDict _pd = new PriorityDict();       // 灵活多用字典

        public void Init(ScriptAccessory sa)
        {
            parse = 0;
            arrowMode = -1;
            ArrowNum = 0;
            CannonNum = 0;
            StepCannonIndex = 0;
            P5_TV_Support_enable = false;
            P52_OmegaMDir = 0;
            P52_OmegaFDirDone = false;
            P52_OmegaFDir = 0;
            P52_OmegaM_Skill = false;
			MFTransformStates = [0,0,0,0]; 
        	MFPositions = [0,0,0,0];
			P5_3_MFT = 0;
            ArrowModeConfirmed = new System.Threading.AutoResetEvent(false);
            InitParams();
            _phase = TopPhase.Init;
            sa.Method.RemoveDraw(".*");
        }
        
        private void InitParams()
        {
            _bools = new bool[20].ToList();
            _numbers = Enumerable.Repeat(0, 20).ToList();
            _events = Enumerable
                .Range(0, 20)
                .Select(_ => new ManualResetEvent(false))
                .ToList();
        }
        
        #region P1
        [ScriptMethod(name: "P1_循环程序_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31491"],userControl:false)]
        public void P1_循环程序_分P(Event @event, ScriptAccessory accessory)
        {
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            P1_BossId=tid;
            parse = 1.1;
            P1_TowerPos = [];
            P1_LineRound = 0;
        }
        [ScriptMethod(name: "P1_循环程序_Buff记录", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(3004|3005|3006|3451)$"],userControl:false)]
        public void P1_循环程序_Buff记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.1) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var index = accessory.Data.PartyList.IndexOf(tid);
            P1_点名Buff[index] = @event["StatusID"] switch
            {
                "3004" => 1,
                "3005" => 2,
                "3006" => 3,
                "3451" => 4,
                _=>0
            };
        }
        [ScriptMethod(name: "P1_循环程序_塔收集", eventType: EventTypeEnum.ObjectChanged, eventCondition: ["Operate:Add", "DataId:2013245"], userControl: false)]
        public void P1_循环程序_塔收集(Event @event, ScriptAccessory accessory)
        {
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            lock (P1_TowerPos)
            {
                P1_TowerPos.Add(pos);
            }
        }
        [ScriptMethod(name: "P1_循环程序_集合提醒", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31491"])]
        public void P1_循环程序_集合提醒(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.1) return;
            accessory.Method.TextInfo("Boss背后集合", 2000);
            accessory.Method.TTS("Boss背后集合");
        }
        [ScriptMethod(name: "P1_循环程序_开始站位提醒", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(3004|3005|3006|3451)$"])]
        public void P1_循环程序_开始站位提醒(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.1) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if (tid != accessory.Data.Me) return;
            if (@event["StatusID"]=="3006")
            {
                accessory.Method.TextInfo("靠前接线", 3000);
                accessory.Method.TTS("靠前接线");
            }
            else
            {
                accessory.Method.TextInfo("靠后", 3000);
                accessory.Method.TTS("靠后");
            }
           
        }
        [ScriptMethod(name: "P1_循环程序_线塔处理位置", eventType: EventTypeEnum.ObjectChanged, eventCondition: ["Operate:Add", "DataId:2013245"])]
        public async void P1_循环程序_线塔处理位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.1) return;
            lock (this)
            {
                if ((DateTime.Now - P1_TowerTime).TotalSeconds < 2) return;
                P1_TowerTime=DateTime.Now;
            }
            await Task.Delay(50);
            Vector3 centre = new(100, 0, 100);
            var towerCount = P1_TowerPos.Count;
            List<int> HtdhParty = [2, 0, 1, 4, 5, 6, 7, 3];
            var myindex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var myBuff = P1_点名Buff[myindex];
            var index1 = P1_点名Buff.IndexOf(myBuff);
            var index2 = P1_点名Buff.LastIndexOf(myBuff);
            var hIndex = HtdhParty.IndexOf(index1) < HtdhParty.IndexOf(index2) ? index1 : index2;
            var meIsHigh = hIndex == myindex;
            var idle = false;
            //塔
            if (towerCount == myBuff * 2)
            {
                idle=true;
                var hPos=default(Vector3);
                var lPos=default(Vector3);
                if (RoundPositionTo4Dir(P1_TowerPos[towerCount - 2], centre) < RoundPositionTo4Dir(P1_TowerPos[towerCount - 1], centre))
                {
                    hPos = P1_TowerPos[towerCount - 2];
                    lPos = P1_TowerPos[towerCount - 1];
                }
                else
                {
                    hPos = P1_TowerPos[towerCount - 1];
                    lPos = P1_TowerPos[towerCount - 2];
                }
                var dealpos = meIsHigh?hPos:lPos;

                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P1_循环程序_塔站位";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 9000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P1_循环程序_塔范围";
                dp.Scale = new(3);
                dp.Position = dealpos;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 9000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            }
            //线
            if (towerCount % 8 == (myBuff + 2) * 2 % 8)
            {
                
                idle = true;
                List<int> isTower = [0, 0, 0, 0];
                isTower[RoundPositionTo4Dir(P1_TowerPos[towerCount - 2], centre)] = 1;
                isTower[RoundPositionTo4Dir(P1_TowerPos[towerCount - 1], centre)] = 1;
                var my4Dir = meIsHigh ? isTower.IndexOf(0) : isTower.LastIndexOf(0);
                var dealpos = RotatePoint(new(100, 0, 85), centre, float.Pi / 2 * my4Dir);

                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P1_循环程序_线站位";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 9000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            //闲
            if (!idle)
            {
                //北点 100,0,86
                var myPos = accessory.Data.Objects.SearchByEntityId(accessory.Data.Me)?.Position??default;
                var drot = (myPos - P1_TowerPos[towerCount - 2]).Length() < (myPos - P1_TowerPos[towerCount - 1]).Length() ? RoundPositionTo4Dir(P1_TowerPos[towerCount - 2],centre) : RoundPositionTo4Dir(P1_TowerPos[towerCount - 1], centre);
                var dealpos=RotatePoint(new(100,0,86),centre,float.Pi/2*drot);
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P1_循环程序_闲站位";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 9000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
        }
        [ScriptMethod(name: "P1_循环程序_接线标记", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31496", "TargetIndex:1"])]
        public void P1_循环程序_接线标记(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.1) return;
            P1_LineRound++;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var waitBuff = (P1_LineRound + 1) % 4 + 1;
            var catchBuff = (P1_LineRound + 2) % 4 + 1;
            var myindex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            if (catchBuff != P1_点名Buff[myindex]) return;

            
            var myBuff = P1_点名Buff[myindex];
            var index1 = P1_点名Buff.IndexOf(myBuff);
            var index2 = P1_点名Buff.LastIndexOf(myBuff);
            var hIndex = HtdhParty.IndexOf(index1) < HtdhParty.IndexOf(index2) ? index1 : index2;
            var meIsHigh = hIndex == myindex;

            var index3 = P1_点名Buff.IndexOf(waitBuff);
            var index4 = P1_点名Buff.LastIndexOf(waitBuff);
            var hWaitIndex = HtdhParty.IndexOf(index3) < HtdhParty.IndexOf(index4) ? index3 : index4;
            var lWaitIndex = HtdhParty.IndexOf(index3) < HtdhParty.IndexOf(index4) ? index4 : index3;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_循环程序_接线标记";
            dp.Scale = new(10);
            dp.Owner = tid;
            dp.TargetObject = meIsHigh? accessory.Data.PartyList[hWaitIndex]: accessory.Data.PartyList[lWaitIndex];
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = new(1,1,0,1);
            dp.DestoryAt = 9000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Line, dp);
        }
        [ScriptMethod(name: "P1_循环程序_接线标记移除", eventType: EventTypeEnum.Tether, eventCondition: ["Id:0059"])]
        public void P1_循环程序_接线标记移除(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.1) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            if (sid != accessory.Data.Me) return;
            accessory.Method.RemoveDraw("P1_循环程序_接线标记");
        }

        [ScriptMethod(name: "P1_全能之主_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31499"], userControl: false)]
        public void P1_全能之主_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 1.2;
            P1_FireCount = 0;
        }
        [ScriptMethod(name: "P1_全能之主_Buff记录", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(3004|3005|3006|3451)$"], userControl: false)]
        public void P1_全能之主_Buff记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.2) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var index = accessory.Data.PartyList.IndexOf(tid);
            P1_点名Buff[index] = @event["StatusID"] switch
            {
                "3004" => 1,
                "3005" => 2,
                "3006" => 3,
                "3451" => 4,
                _ => 0
            };
        }
        [ScriptMethod(name: "P1_全能之主_高低顺位播报", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31499"])]
        public async void P1_全能之主_高低顺位播报(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.2) return;
            await Task.Delay(100);
            var myindex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var mybuff = P1_点名Buff[myindex];
            var i1 = P1_点名Buff.IndexOf(mybuff);
            var i2 = P1_点名Buff.LastIndexOf(mybuff);
            var hIndex = HtdhParty.IndexOf(i1) < HtdhParty.IndexOf(i2) ? i1 : i2;
            if (hIndex== myindex)
            {
                accessory.Method.TextInfo("高顺位(上右)", 10000);
                accessory.Method.TTS("高顺位(上右)");
            }
            else
            {
                accessory.Method.TextInfo("低顺位(下左)", 10000);
                accessory.Method.TTS("低顺位(下左)");
            }
        }
        [ScriptMethod(name: "P1_全能之主_单点命中播报", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31502"])]
        public async void P1_全能之主_单点命中播报(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.2) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if (tid == accessory.Data.Me) 
            {
                accessory.Method.TextInfo("回头", 2000);
                accessory.Method.TTS("回头");
            }
        }
        [ScriptMethod(name: "P1_全能之主_分摊范围", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(350[789]|3510)$"])]
        public void P1_全能之主_分摊范围(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.2) return;
            
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if (!int.TryParse(@event["DurationMilliseconds"], out var dur)) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_全能之主_分摊范围";
            dp.Scale = new(6,30);
            dp.Owner = P1_BossId;
            dp.TargetObject = tid;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.Delay = dur - 3000;
            dp.DestoryAt = 3000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
        }
        [ScriptMethod(name: "P1_全能之主_最远距离顺劈", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:0017"])]
        public void P1_全能之主_最远距离顺劈(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.2) return;
            lock (this)
            {
                if ((DateTime.Now - P1_FanTime).TotalSeconds < 20) return;
                P1_FanTime = DateTime.Now;
            }
            
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_全能之主_最远距离顺劈1";
            dp.Scale = new(20);
            dp.Radian = float.Pi / 3 * 2;
            dp.Owner = P1_BossId;
            dp.TargetResolvePattern=PositionResolvePatternEnum.PlayerFarestOrder;
            dp.TargetOrderIndex = 1;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 13000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_全能之主_最远距离顺劈2";
            dp.Scale = new(20);
            dp.Radian = float.Pi / 3 * 2;
            dp.Owner = P1_BossId;
            dp.TargetResolvePattern = PositionResolvePatternEnum.PlayerFarestOrder;
            dp.TargetOrderIndex = 2;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 13000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
        }
        [ScriptMethod(name: "P1_全能之主_点名直线", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:0017"])]
        public void P1_全能之主_点名直线(Event @event, ScriptAccessory accessory)
        {
            if (parse != 1.2) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P1_全能之主_点名直线";
            dp.Scale = new(6,50);
            dp.TargetObject = tid;
            dp.Owner = P1_BossId;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
        }
        [ScriptMethod(name: "P1_全能之主_T引导顺劈位置", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:32368"])]
        public void P1_全能之主_T引导顺劈位置(Event @event, ScriptAccessory accessory)
        {
            lock (this)
            {
                P1_FireCount++;
                if (P1_FireCount != 26) return;
                var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                if (myindex != 0 && myindex != 1) return;

                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P1_全能之主_T引导顺劈位置";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = new(100,0,86);
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 11000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
        }
        #endregion

        #region P2
        [ScriptMethod(name: "P2_协同程序PT_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31550"], userControl: false)]
        public void P2_协同程序PT_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 2.1;
            P2_Stack = [];
        }
        [ScriptMethod(name: "P2_协同程序PT_Buff记录", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(3427|3428)$"], userControl: false)]
        public void P2_协同程序PT_Buff记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.1) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            P2_PTBuffIsFar = @event["StatusID"] == "3428";
        }
        [ScriptMethod(name: "P2_协同程序PT_索尼记录", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:regex:^(01A[0123])$"], userControl: false)]
        public void P2_协同程序PT_索尼记录(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.1) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            lock (P2_Sony)
            {
                P2_Sony[accessory.Data.PartyList.IndexOf(tid)] = @event["Id"] switch
                {
                    "01A0" => 1,
                    "01A1" => 3,
                    "01A2" => 4,
                    "01A3" => 2,
                    _ => 0
                };
            }
            
        }
        [ScriptMethod(name: "P2_协同程序PT_男女人AOE", eventType: EventTypeEnum.PlayActionTimeline, eventCondition: ["Id:7747", "SourceDataId:regex:^(15714|15715)$"])]
        public void P2_协同程序PT_男女人AOE(Event @event, ScriptAccessory accessory)
        {
            // 15714 男
            // 15715 女
            //男人剑 0 盾 4
            //女人标杆 0 脚刀 4
            if (parse != 2.1) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            Vector3 centre = new(100, 0, 100);
            if ((pos - centre).Length() > 12) return;
            var transformationID = GetTransformationID(sid, accessory);
            if (transformationID == null) return;
            if (@event["SourceDataId"] == "15714")
            {
                //男
                if (transformationID == 0)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_协同程序PT_男钢铁";
                    dp.Scale = new(10);
                    dp.Owner = sid;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.DestoryAt = 5500;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
                }
                if (transformationID == 4)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_协同程序PT_男月环";
                    dp.Scale = new(40);
                    dp.InnerScale = new(10);
                    dp.Radian = float.Pi * 2;
                    dp.Owner = sid;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.DestoryAt = 5500;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);
                }
            }
            if (@event["SourceDataId"] == "15715")
            {
                if (transformationID == 0)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_协同程序PT_女十字1";
                    dp.Scale = new(10, 60);
                    dp.Owner = sid;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.DestoryAt = 5500;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_协同程序PT_女十字1";
                    dp.Scale = new(10, 60);
                    dp.Rotation = float.Pi / 2;
                    dp.Owner = sid;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.DestoryAt = 5500;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
                }
                if (transformationID == 4)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_协同程序PT_女辣翅1";
                    dp.Scale = new(60, 20);
                    dp.Owner = sid;
                    dp.Rotation = float.Pi / 2;
                    dp.Offset = new(-4, 0, 0);
                    dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                    dp.DestoryAt = 5500;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_协同程序PT_女辣翅2";
                    dp.Scale = new(60, 20);
                    dp.Owner = sid;
                    dp.Rotation = float.Pi / -2;
                    dp.Offset = new(4, 0, 0);
                    dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                    dp.DestoryAt = 5500;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
                }
            }
        }
        [ScriptMethod(name: "P2_协同程序PT_眼睛激光", eventType: EventTypeEnum.EnvControl, eventCondition: ["DirectorId:800375AC", "Id:00020001"])]
        public void P2_协同程序PT_眼睛激光(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.1) return;
            var rot = @event["Index"] switch
            {
                "00000001" => 0,
                "00000002" => 1,
                "00000003" => 2,
                "00000004" => 3,
                "00000005" => 4,
                "00000006" => 5,
                "00000007" => 6,
                "00000008" => 7,
                _ => -1
            };
            if (rot == -1) return;
            var pos = RotatePoint(new(100, 0, 80), new(100, 0, 100), float.Pi / 4 * rot);
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_协同程序PT_眼睛激光";
            dp.Scale = new(16,40);
            dp.Position = pos;
            dp.TargetPosition = new(100, 0, 100);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 7500;
            dp.DestoryAt = 6000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);

          
        }
        [ScriptMethod(name: "P2_协同程序PT_五钢铁", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31521", "TargetIndex:1"])]
        public void P2_协同程序PT_五钢铁(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.1) return;
            foreach (var c in accessory.Data.Objects)
            {
                if(c.DataId== 15714 || c.DataId == 15713)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P2_协同程序PT_五钢铁";
                    dp.Scale = new(10);
                    dp.Owner = c.GameObjectId;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.DestoryAt = 11000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
                }
            }
        }
        [ScriptMethod(name: "P2_协同程序PT_眼睛激光索尼处理位置", eventType: EventTypeEnum.EnvControl, eventCondition: ["DirectorId:800375AC", "Id:00020001"])]
        public async void P2_协同程序PT_眼睛激光索尼处理位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.1) return;
            var dir = @event["Index"] switch
            {
                "00000001" => 0,
                "00000002" => 1,
                "00000003" => 2,
                "00000004" => 3,
                "00000005" => 4,
                "00000006" => 5,
                "00000007" => 6,
                "00000008" => 7,
                _ => -1
            };
            if (dir == -1) return;
            await Task.Delay(3000);
            Vector3 centre = new(100, 0, 100);

            Vector3 middleLeft1Pos =  RotatePoint(new(088.5f, 0, 085.5f), centre, float.Pi / 4 * dir);
            Vector3 middleRight1Pos = RotatePoint(new(111.5f, 0, 085.5f), centre, float.Pi / 4 * dir);
            Vector3 middleLeft2Pos = RotatePoint(new(088.5f, 0, 095.0f), centre, float.Pi / 4 * dir);
            Vector3 middleRight2Pos = RotatePoint(new(111.5f, 0, 095.0f), centre, float.Pi / 4 * dir);
            Vector3 middleLeft3Pos = RotatePoint(new(088.5f, 0, 105.0f), centre, float.Pi / 4 * dir);
            Vector3 middleRight3Pos = RotatePoint(new(111.5f, 0, 105.0f), centre, float.Pi / 4 * dir);
            Vector3 middleLeft4Pos = RotatePoint(new(088.5f, 0, 114.5f), centre, float.Pi / 4 * dir);
            Vector3 middleRight4Pos = RotatePoint(new(111.5f, 0, 114.5f), centre, float.Pi / 4 * dir);

            Vector3 farLeft1Pos = RotatePoint(new(091.5f, 0, 083.0f), centre, float.Pi / 4 * dir);
            Vector3 farRight1Pos = RotatePoint(new(108.5f, 0, 117.0f), centre, float.Pi / 4 * dir);
            Vector3 farLeft2Pos = RotatePoint(new(082.0f, 0, 093.0f), centre, float.Pi / 4 * dir);
            Vector3 farRight2Pos = RotatePoint(new(118.0f, 0, 107.0f), centre, float.Pi / 4 * dir);
            Vector3 farLeft3Pos = RotatePoint(new(082.0f, 0, 107.0f), centre, float.Pi / 4 * dir);
            Vector3 farRight3Pos = RotatePoint(new(118.0f, 0, 093.0f), centre, float.Pi / 4 * dir);
            Vector3 farLeft4Pos = RotatePoint(new(091.5f, 0, 117.0f), centre, float.Pi / 4 * dir);
            Vector3 farRight4Pos = RotatePoint(new(108.5f, 0, 083.0f), centre, float.Pi / 4 * dir);

            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var mySony = P2_Sony[myindex];
            var myPartnerIndex = P2_Sony.IndexOf(mySony) == myindex ? P2_Sony.LastIndexOf(mySony) : P2_Sony.IndexOf(mySony);
            var meIsHigh = HtdhParty.IndexOf(myindex) < HtdhParty.IndexOf(myPartnerIndex);
            Vector3 dealpos = mySony switch
            {
                1 => P2_PTBuffIsFar ? (meIsHigh ? farLeft1Pos : farRight1Pos) : (meIsHigh ? middleLeft1Pos : middleRight1Pos),
                2 => P2_PTBuffIsFar ? (meIsHigh ? farLeft2Pos : farRight2Pos) : (meIsHigh ? middleLeft2Pos : middleRight2Pos),
                3 => P2_PTBuffIsFar ? (meIsHigh ? farLeft3Pos : farRight3Pos) : (meIsHigh ? middleLeft3Pos : middleRight3Pos),
                4 => P2_PTBuffIsFar ? (meIsHigh ? farLeft4Pos : farRight4Pos) : (meIsHigh ? middleLeft4Pos : middleRight4Pos),
                _ => default
            };
            if (dealpos == default) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_协同程序PT_眼睛激光索尼处理位置";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 11000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        [ScriptMethod(name: "P2_协同程序PT_分摊处理位置", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:0064"])]
        public void P2_协同程序PT_分摊处理位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.1) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            lock (P2_Stack)
            {
                P2_Stack.Add(accessory.Data.PartyList.IndexOf(tid));
                if (P2_Stack.Count != 2) return;
            }
            
            var myindex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            List<int> leftGroup = [];
            leftGroup.Add(HtdhParty.IndexOf(P2_Sony.IndexOf(1)) < HtdhParty.IndexOf(P2_Sony.LastIndexOf(1)) ? P2_Sony.IndexOf(1) : P2_Sony.LastIndexOf(1));
            leftGroup.Add(HtdhParty.IndexOf(P2_Sony.IndexOf(2)) < HtdhParty.IndexOf(P2_Sony.LastIndexOf(2)) ? P2_Sony.IndexOf(2) : P2_Sony.LastIndexOf(2));
            leftGroup.Add(HtdhParty.IndexOf(P2_Sony.IndexOf(3)) < HtdhParty.IndexOf(P2_Sony.LastIndexOf(3)) ? P2_Sony.IndexOf(3) : P2_Sony.LastIndexOf(3));
            leftGroup.Add(HtdhParty.IndexOf(P2_Sony.IndexOf(4)) < HtdhParty.IndexOf(P2_Sony.LastIndexOf(4)) ? P2_Sony.IndexOf(4) : P2_Sony.LastIndexOf(4));
            
            //左边两个分摊
            if (leftGroup.Contains(P2_Stack[0]) && leftGroup.Contains(P2_Stack[1]))
            {
                var lowStackIndex = P2_Sony[P2_Stack[0]] < P2_Sony[P2_Stack[1]] ? P2_Stack[1] : P2_Stack[0];
                var lowStackSony = P2_Sony[lowStackIndex];
                var lowStackPartnerIndex = P2_Sony.IndexOf(lowStackSony) == lowStackIndex ? P2_Sony.LastIndexOf(lowStackSony) : P2_Sony.IndexOf(lowStackSony);
                leftGroup.Remove(lowStackIndex);
                leftGroup.Add(lowStackPartnerIndex);
            }
            //右边两个分摊
            if (!leftGroup.Contains(P2_Stack[0]) && !leftGroup.Contains(P2_Stack[1]))
            {
                if (P2_PTBuffIsFar)
                {
                    var lowStackIndex = P2_Sony[P2_Stack[0]] < P2_Sony[P2_Stack[1]] ? P2_Stack[0] : P2_Stack[1];
                    var lowStackSony = P2_Sony[lowStackIndex];
                    var lowStackPartnerIndex = P2_Sony.IndexOf(lowStackSony) == lowStackIndex ? P2_Sony.LastIndexOf(lowStackSony) : P2_Sony.IndexOf(lowStackSony);
                    leftGroup.Remove(lowStackPartnerIndex);
                    leftGroup.Add(lowStackIndex);
                }
                else
                {
                    var lowStackIndex = P2_Sony[P2_Stack[0]] < P2_Sony[P2_Stack[1]] ? P2_Stack[1] : P2_Stack[0];
                    var lowStackSony = P2_Sony[lowStackIndex];
                    var lowStackPartnerIndex = P2_Sony.IndexOf(lowStackSony) == lowStackIndex ? P2_Sony.LastIndexOf(lowStackSony) : P2_Sony.IndexOf(lowStackSony);
                    leftGroup.Remove(lowStackPartnerIndex);
                    leftGroup.Add(lowStackIndex);
                }
                
            }
            
            Vector3 dealpos = default;
            if (P2_PTBuffIsFar)
            {
                dealpos = leftGroup.Contains(myindex) ? new(94, 0, 100) : new(106, 0, 100);
            }
            else
            {
                dealpos = leftGroup.Contains(myindex) ? new(97, 0, 100) : new(100, 0, 103);
            }
            var c = accessory.Data.Objects.Where(o => o.DataId == 15713).FirstOrDefault();
            if (c == null) return;
            var dir8 = RoundPositionTo8Dir(c!.Position, new(100, 0, 100));
            //accessory.Log.Debug($"P2_协同程序PT {dir8} {leftGroup.Contains(myindex)}");
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_协同程序PT_分摊处理位置";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = RotatePoint(dealpos, new(100, 0, 100), float.Pi / 4 * dir8);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 7000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }

        [ScriptMethod(name: "P2_协同程序LB_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31544"], userControl: false)]
        public void P2_协同程序LB_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 2.2;
            P2_刀光剑舞连线 = [];
        }
        [ScriptMethod(name: "P2_协同程序LB_射手天剑", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31539"])]
        public void P2_协同程序LB_射手天剑(Event @event, ScriptAccessory accessory)
        {
            if(parse != 2.2) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_协同程序LB_射手天剑";
            dp.Scale = new(10,42);
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 500;
            dp.DestoryAt = 7500;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
        }
        [ScriptMethod(name: "P2_协同程序LB_刀光剑舞", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(3154[01])$"])]
        public void P2_协同程序LB_刀光剑舞(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.2) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;


            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"P2_协同程序LB_刀光剑舞-{sid}";
            dp.Scale = new(40);
            dp.Owner = sid;
            dp.TargetResolvePattern = PositionResolvePatternEnum.TetherTarget;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 8200;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
        }
        //[ScriptMethod(name: "P2_协同程序LB_刀光剑舞移除", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31539"])]
        //public void P2_协同程序LB_刀光剑舞移除(Event @event, ScriptAccessory accessory)
        //{
        //    if (parse != 2.2) return;
        //    foreach (var item in P2_刀光剑舞连线)
        //    {
        //        accessory.Method.RemoveDraw($"P2_协同程序LB_刀光剑舞-{item.Key}-{item.Value}");
        //    }
        //}
        [ScriptMethod(name: "P2_协同程序LB_盾连击S", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31527"])]
        public void P2_协同程序LB_盾连击S(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.2) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"P2_协同程序LB_盾连击S-1-1";
            dp.Scale = new(5);
            dp.Owner = sid;
            dp.CentreResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.CentreOrderIndex = 1;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 5200;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"P2_协同程序LB_盾连击S-1-2";
            dp.Scale = new(5);
            dp.Owner = sid;
            dp.CentreResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.CentreOrderIndex = 2;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 5200;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"P2_协同程序LB_盾连击S-2";
            dp.Scale = new(6);
            dp.Owner = sid;
            dp.CentreResolvePattern = PositionResolvePatternEnum.PlayerNearestOrder;
            dp.CentreOrderIndex = 1;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.Delay = 5200;
            dp.DestoryAt = 2800;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        }
        [ScriptMethod(name: "P2_协同程序LB_盾连击S命中提示", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31528"])]
        public void P2_协同程序LB_盾连击S命中提示(Event @event, ScriptAccessory accessory)
        {
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if (accessory.Data.Me != tid) return;
            accessory.Method.TextInfo("出去出去",3000);
            accessory.Method.TTS("出去出去");
        }


        [ScriptMethod(name: "P2_协同程序LB_射手天剑引导位置", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31544"])]
        public void P2_协同程序LB_射手天剑引导位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.2) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_协同程序LB_射手天剑引导位置";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = new(100,0,94.5f);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 10000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        [ScriptMethod(name: "P2_协同程序LB_盾连击S_男人位置连线", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:32369"])]
        public void P2_协同程序LB_盾连击S_男人位置连线(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.2) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var myindex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            if (myindex != 0 && myindex != 1) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_协同程序LB_盾连击S_男人位置连线";
            dp.Scale = new(5);
            dp.Owner = accessory.Data.Me;
            dp.TargetObject = sid;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.Delay = 8000;
            dp.DestoryAt = 11000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Line, dp);
        }
        [ScriptMethod(name: "P2_协同程序LB_盾连击S二段处理位置", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31528"])]
        public void P2_协同程序LB_盾连击S二段处理位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 2.2) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            Vector3 dealpos = new(100, 0, 100);
            if (accessory.Data.Me == tid) 
            {
                var pos = JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);
                dealpos = RotatePoint(pos, new(100, 0, 100), float.Pi/2);
            }
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P2_协同程序LB_盾连击S二段处理位置";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 2800;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        #endregion

        #region P3

        [ScriptMethod(name: "P3_开场_分P", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31507"], userControl: false)]
        public void P3_开场_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 3.0;
            P3_ArmCount = 0;
            P3_StartBuff = [0, 0, 0, 0, 0, 0, 0, 0];
            P3_StartDone = false;
            P3_StartPreDone = false;
            P3_TVBuff = [0, 0, 0, 0, 0, 0, 0, 0];
        }
        [ScriptMethod(name: "P3_开场_Buff收集", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(3425|3426)$"], userControl: false)]
        public void P3_开场_Buff收集(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.0) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var index = accessory.Data.PartyList.IndexOf(tid);
            if (index == -1) return;
            lock (P3_StartBuff)
            {
                //1分散 2分摊
                P3_StartBuff[index] = @event["StatusID"] == "3425" ? 1 : 2;
            }
        }
        [ScriptMethod(name: "P3_小电视_Buff收集", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(3452|3453)$"], userControl: false)]
        public void P3_小电视_Buff收集(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.0) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var index = accessory.Data.PartyList.IndexOf(tid);
            if (index == -1) return;
            lock (P3_TVBuff)
            {
                P3_TVBuff[index] = @event["StatusID"] == "3452" ? 1 : 2;
            }
        }
        [ScriptMethod(name: "P3_开场_手臂AOE", eventType: EventTypeEnum.PlayActionTimeline, eventCondition: ["Id:regex:^(774[78])$", "SourceDataId:regex:^(1571[89])$"], userControl: false)]
        public void P3_开场_手臂AOE(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.0) return;
            lock (this)
            {
                P3_ArmCount++;
                if (!ParseObjectId(@event["SourceId"], out var sid)) return;

                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_开场_手臂AOE";
                dp.Scale = new(11);
                dp.Owner = sid;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.Delay= P3_ArmCount > 3 ? 11000 : 0;
                dp.DestoryAt = P3_ArmCount > 3 ? 2500 : 14000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
            }
        }
        [ScriptMethod(name: "P3_开场_地震", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31567"])]
        public void P3_开场_地震(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.0) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_开场_地震_1";
            dp.Scale = new(6);
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 0;
            dp.DestoryAt = 4800;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_开场_地震_2";
            dp.InnerScale = new(6);
            dp.Scale = new(12);
            dp.Radian = float.Pi * 2;
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 4800;
            dp.DestoryAt = 2000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_开场_地震_3";
            dp.InnerScale = new(12);
            dp.Scale = new(18);
            dp.Radian = float.Pi * 2;
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 6800;
            dp.DestoryAt = 2000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_开场_地震_4";
            dp.InnerScale = new(18);
            dp.Scale = new(24);
            dp.Radian = float.Pi * 2;
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 8800;
            dp.DestoryAt = 2000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);
        }
        [ScriptMethod(name: "P3_小电视_自身AOE", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(3159[56])$"])]
        public void P3_小电视_自身AOE(Event @event, ScriptAccessory accessory)
        {
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_小电视_自身AOE";
            dp.Scale = new(7);
            dp.Owner = accessory.Data.Me;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 7000;
            dp.DestoryAt = 3000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        }
        [ScriptMethod(name: "P3_开场_Buff预站位处理位置", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:3426"])]
        public async void P3_开场_Buff预站位处理位置(Event @event, ScriptAccessory accessory)
        {
            lock (this)
            {
                if(P3_StartPreDone) return;
                P3_StartPreDone = true;
            }
            await Task.Delay(100);
            List<int> sortOrder = P3_StackSort switch
            {
                P3SortEnum.HTDH => HtdhParty,
                P3SortEnum.THD => [0, 1, 2, 3, 4, 5, 6, 7],
                _ => [0, 1, 2, 3, 4, 5, 6, 7],
            };
            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            //1分散 2分摊
            var myP3StartBuff = P3_StartBuff[myindex];
            var myP3Index = 0;
            for (int i = 0; i < sortOrder.Count; i++)
            {

                var index = sortOrder[i];
                if (myP3StartBuff == P3_StartBuff[index]) myP3Index++;
                //accessory.Log.Debug($"{myindex} {index} {myP3StartBuff} {P3_StartBuff[index]} {myP3Index}");
                if (index == myindex) break;
            }
            Vector3 dealpos = default;
            if (myP3StartBuff == 2 || myP3StartBuff == 0)
            {
                dealpos = myP3Index switch
                {
                    1 =>  new(092.00f, 0, 086.14f),
                    2 =>  new(108.00f, 0, 086.14f),
                    _ => default,
                };
            }
            if (myP3StartBuff == 1)
            {
                dealpos = myP3Index switch
                {
                    1 => new(084.00f, 0, 100.00f),
                    2 => new(092.00f, 0, 113.86f),
                    3 => new(108.00f, 0, 113.86f),
                    4 => new(116.00f, 0, 100.00f),
                    _ => default,
                };
            }
            if (dealpos == default) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_开场_Buff预站位处理位置";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 3100;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        [ScriptMethod(name: "P3_开场_处理位置", eventType: EventTypeEnum.PlayActionTimeline, eventCondition: ["Id:regex:^(774[78])$", "SourceDataId:regex:^(1571[89])$"])]
        public void P3_开场_处理位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 3.0) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            if (MathF.Abs(pos.X - 100) > 1) return;
            if (P3_StartDone) return;
            P3_StartDone = true;

            var northCirle = pos.Z < 100;

            List<int> sortOrder = P3_StackSort switch
            {
                P3SortEnum.HTDH => HtdhParty,
                P3SortEnum.THD => [0, 1, 2, 3, 4, 5, 6, 7],
                _ => [0, 1, 2, 3, 4, 5, 6, 7],
            };
            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            //1分散 2分摊
            var myP3StartBuff = P3_StartBuff[myindex];
            var myP3Index = 0;
            for (int i = 0; i < sortOrder.Count; i++)
            {
                
                var index = sortOrder[i];
                if (myP3StartBuff == P3_StartBuff[index]) myP3Index++;
                //accessory.Log.Debug($"{myindex} {index} {myP3StartBuff} {P3_StartBuff[index]} {myP3Index}");
                if (index == myindex) break;
            }
            
            Vector3 dealpos1 = default;
            Vector3 dealpos2 = default;
            Vector3 dealpos3 = default;
            Vector3 dealpos4 = default;
            if (myP3StartBuff == 2 || myP3StartBuff == 0)
            {
                dealpos1 = myP3Index switch
                {
                    1 => northCirle ? new(086.7f, 0, 086.7f) : new(094.8f, 0, 082.0f),
                    2 => northCirle ? new(113.3f, 0, 086.7f) : new(105.2f, 0, 082.0f),
                    _ => default,
                };
                dealpos2 = myP3Index switch
                {
                    1 => northCirle ? new(087.8f, 0, 087.8f) : new(095.0f, 0, 083.5f),
                    2 => northCirle ? new(112.2f, 0, 087.8f) : new(105.0f, 0, 083.5f),
                    _ => default,
                };
                dealpos3 = myP3Index switch
                {
                    1 => northCirle ? new(088.4f, 0, 085.5f) : new(093.1f, 0, 082.8f),
                    2 => northCirle ? new(111.6f, 0, 085.5f) : new(106.9f, 0, 082.8f),
                    _ => default,
                };
                dealpos4 = myP3Index switch
                {
                    1 => northCirle ? new(094.7f, 0, 083.7f) : new(088.5f, 0, 087.0f),
                    2 => northCirle ? new(105.3f, 0, 083.7f) : new(111.5f, 0, 087.0f),
                    _ => default,
                };
            }
            if (myP3StartBuff == 1)
            {
                dealpos1 = myP3Index switch
                {
                    1 => northCirle ? new(082.0f, 0, 095.0f) : new(082.0f, 0, 104.7f),
                    2 => northCirle ? new(095.0f, 0, 118.0f) : new(086.5f, 0, 113.0f),
                    3 => northCirle ? new(105.0f, 0, 118.0f) : new(113.5f, 0, 113.0f),
                    4 => northCirle ? new(118.0f, 0, 095.0f) : new(118.0f, 0, 104.7f),
                    _ => default,
                };
                dealpos2 = myP3Index switch
                {
                    1 => northCirle ? new(083.5f, 0, 095.5f) : new(083.5f, 0, 104.5f),
                    2 => northCirle ? new(095.0f, 0, 116.5f) : new(088.0f, 0, 112.0f),
                    3 => northCirle ? new(105.0f, 0, 116.5f) : new(112.0f, 0, 112.0f),
                    4 => northCirle ? new(116.5f, 0, 095.5f) : new(116.5f, 0, 104.5f),
                    _ => default,
                };
                dealpos3 = myP3Index switch
                {
                    1 => northCirle ? new(081.7f, 0, 097.2f) : new(081.6f, 0, 102.8f),
                    2 => northCirle ? new(093.2f, 0, 117.2f) : new(088.5f, 0, 114.5f),
                    3 => northCirle ? new(106.8f, 0, 117.2f) : new(111.5f, 0, 114.5f),
                    4 => northCirle ? new(118.3f, 0, 097.2f) : new(118.4f, 0, 102.8f),
                    _ => default,
                };
                dealpos4 = myP3Index switch
                {
                    1 => northCirle ? new(083.5f, 0, 104.0f) : new(084.0f, 0, 095.0f),
                    2 => northCirle ? new(088.5f, 0, 112.5f) : new(095.0f, 0, 116.3f),
                    3 => northCirle ? new(111.5f, 0, 112.5f) : new(105.0f, 0, 116.3f),
                    4 => northCirle ? new(116.5f, 0, 104.0f) : new(116.0f, 0, 095.0f),
                    _ => default,
                };
            }

            if (dealpos1 != default)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_开场_处理位置_预站位";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos1;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 6000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            if (dealpos1 != default && dealpos2 != default)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_开场_处理位置_1-2";
                dp.Scale = new(2);
                dp.Position = dealpos1;
                dp.TargetPosition = dealpos2;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 6000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_开场_处理位置_2";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos2;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 6000;
                dp.DestoryAt = 2000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            if (dealpos2 != default && dealpos3 != default)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_开场_处理位置_2-3";
                dp.Scale = new(2);
                dp.Position = dealpos2;
                dp.TargetPosition = dealpos3;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 8000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_开场_处理位置_3";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos3;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 8000;
                dp.DestoryAt = 6000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }

            if (dealpos3 != default && dealpos4 != default)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_开场_处理位置_3-4";
                dp.Scale = new(2);
                dp.Position = dealpos3;
                dp.TargetPosition = dealpos4;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 14000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P3_开场_处理位置_4";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos4;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 14000;
                dp.DestoryAt = 2000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
        }
        [ScriptMethod(name: "P3_小电视_处理位置", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(3159[56])$"])]
        public void P3_小电视_处理位置(Event @event, ScriptAccessory accessory)
        {
            //31595 东
            //31595 西
            if (parse != 3.0) return;
            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var meIsIdle = P3_TVBuff[myindex]==0;
            var myBuffIndex = 0;
            var isEast = @event["ActionId"] == "31595";
            Vector3 dealpos = default;

            for (int i = 0; i < HtdhParty.Count; i++)
            {
                var index = HtdhParty[i];
                var isIdle = P3_TVBuff[index] == 0;
                if (meIsIdle == isIdle) myBuffIndex++;
                if (index == myindex) break;
            }
            if (P3_TV_Strategy==P3TVEnum.Normal)
            {
                if (meIsIdle)
                {
                    dealpos = myBuffIndex switch
                    {
                        1 => isEast ? new(099.0f, 0, 091.0f) : new(101.0f, 0, 091.0f),
                        2 => isEast ? new(104.0f, 0, 100.0f) : new(096.0f, 0, 100.0f),
                        3 => isEast ? new(115.5f, 0, 100.0f) : new(084.5f, 0, 100.0f),
                        4 => isEast ? new(099.0f, 0, 109.0f) : new(101.0f, 0, 109.0f),
                        5 => isEast ? new(099.0f, 0, 119.0f) : new(101.0f, 0, 119.0f),
                        _ => default
                    };
                }
                else
                {
                    dealpos = myBuffIndex switch
                    {
                        1 => isEast ? new(093.0f, 0, 082.0f) : new(107.0f, 0, 082.0f),
                        2 => isEast ? new(086.0f, 0, 092.5f) : new(114.0f, 0, 092.5f),
                        3 => isEast ? new(086.0f, 0, 107.5f) : new(114.0f, 0, 107.5f),
                        _ => default
                    } ;
                }
            }
            if (P3_TV_Strategy == P3TVEnum.Static)
            {
                if (meIsIdle)
                {
                    dealpos = myBuffIndex switch
                    {
                        1 => isEast ? new(099.0f, 0, 091.0f) : new(101.0f, 0, 091.0f),
                        2 => new(109.0f, 0, 100.0f),
                        3 => new(119.0f, 0, 100.0f),
                        4 => isEast ? new(099.0f, 0, 109.0f) : new(101.0f, 0, 109.0f),
                        5 => isEast ? new(099.0f, 0, 119.0f) : new(101.0f, 0, 119.0f),
                        _ => default
                    };
                }
                else
                {
                    dealpos = myBuffIndex switch
                    {
                        1 => isEast ? new(095.0f, 0, 082.0f) : new(105.0f, 0, 082.0f),
                        2 => new(086.0f, 0, 092.0f),
                        3 => new(086.0f, 0, 108.0f),
                        _ => default
                    };
                }
            }

            if (dealpos == default) return;
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_小电视_处理位置";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 10000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        [ScriptMethod(name: "P3_小电视_面向辅助", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(3159[56])$"])]
        public void P3_小电视_面向辅助(Event @event, ScriptAccessory accessory)
        {
            //31595 东
            //31595 西
            if (parse != 3.0) return;
            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            var meIsIdle = P3_TVBuff[myindex] == 0;
            if (meIsIdle) return;
            var meLeft = P3_TVBuff[myindex] == 2;
            var myBuffIndex = 0;
            var isEast = @event["ActionId"] == "31595";
            float? seeRot = null;

            for (int i = 0; i < HtdhParty.Count; i++)
            {
                var index = HtdhParty[i];
                var isIdle = P3_TVBuff[index] == 0;
                if (meIsIdle == isIdle) myBuffIndex++;
                if (index == myindex) break;
            }
            //b pi/2

            seeRot = myBuffIndex switch
            {
                1 => isEast ? (meLeft ? float.Pi : 0) : (meLeft ? 0 : float.Pi),
                2 => meLeft ? float.Pi / 2 : float.Pi / -2,
                3 => meLeft ? float.Pi / -2 : float.Pi / 2,
                _ => null
            };
            if (seeRot == null) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_小电视_面向辅助_自身1";
            dp.Scale = new(5, 5);
            dp.Owner = accessory.Data.Me;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 10000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Line, dp);
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_小电视_面向辅助_自身2";
            dp.Scale = new(5, 1.5f);
            dp.Offset = new(0, 0, -5);
            dp.Rotation = float.Pi / 6 * 5;
            dp.Owner = accessory.Data.Me;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 10000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Line, dp);
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_小电视_面向辅助_自身3";
            dp.Scale = new(5, 1.5f);
            dp.Offset = new(0, 0, -5);
            dp.Rotation = float.Pi / 6 * -5;
            dp.Owner = accessory.Data.Me;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 10000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Line, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P3_小电视_面向辅助_指向1";
            dp.Scale = new(10,4);
            dp.FixRotation = true;
            dp.Rotation = seeRot.Value;
            dp.Owner = accessory.Data.Me;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 10000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Line, dp);
            
        }
        #endregion

        #region P4
        [ScriptMethod(name: "P4_分P", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31559"], userControl: false)]
        public void P4_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 4.0;
            P4Stack = [];
        }
        [ScriptMethod(name: "P4_分摊点名记录", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:22393"],userControl:false)]
        public void P4_分摊点名记录(Event @event, ScriptAccessory accessory)
        {
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            var index = accessory.Data.PartyList.IndexOf(tid);
            if (index == -1) return;
            lock (P4Stack)
            {
                P4Stack.Add(index);
            }
        }
        [ScriptMethod(name: "P4_地震", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31567"])]
        public void P4_地震(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.0) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_地震_1";
            dp.Scale = new(6);
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 0;
            dp.DestoryAt = 4800;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_地震_2";
            dp.InnerScale = new(6);
            dp.Scale = new(12);
            dp.Radian = float.Pi * 2;
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 4800;
            dp.DestoryAt = 2000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_地震_3";
            dp.InnerScale = new(12);
            dp.Scale = new(18);
            dp.Radian = float.Pi * 2;
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 6800;
            dp.DestoryAt = 2000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_地震_4";
            dp.InnerScale = new(18);
            dp.Scale = new(24);
            dp.Radian = float.Pi * 2;
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 8800;
            dp.DestoryAt = 2000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);
        }
        [ScriptMethod(name: "P4_第一段波动炮命中提示", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31614", "TargetIndex:1"])]
        public void P4_第一段波动炮命中提示(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.0) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if (tid != accessory.Data.Me) return;
            accessory.Method.TextInfo("走", 2000, true);
            accessory.Method.TTS("走");
        }
        [ScriptMethod(name: "P4_第二段波动炮", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31616"])]
        public void P4_第二段波动炮(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.0) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_第二段八方波动炮";
            dp.Scale = new(6,50);
            dp.Owner = sid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 0;
            dp.DestoryAt = 4800;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
        }

        [ScriptMethod(name: "P4_第一段波动炮引导位置", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(3161[07])$"])]
        public void P4_第一段波动炮引导位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.0) return;
            var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
            Vector3 dealpos = myindex switch
            {
                0 => new(087.5f, 0, 094.5f),
                6 => new(086.5f, 0, 100.0f),
                2 => new(087.5f, 0, 105.0f),
                4 => new(090.5f, 0, 109.5f),
                1 => new(112.5f, 0, 094.5f),
                7 => new(113.5f, 0, 100.0f),
                3 => new(112.5f, 0, 105.0f),
                5 => new(109.5f, 0, 109.5f),
                _ => default
            };
            if (dealpos == default) return;

            if (@event["ActionId"]== "31610")
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_第一段波动炮引导位置";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 14000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            else
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_第一段波动炮引导位置";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 5500;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P4_第一段波动炮引导位置";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 15500;
                dp.DestoryAt = 5000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            
        }
        [ScriptMethod(name: "P4_第二段波动炮分摊位置", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31614", "TargetIndex:1"])]
        public void P4_第二段波动炮分摊位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 4.0) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if (tid != accessory.Data.Me) return;

            var myindex=accessory.Data.PartyList.IndexOf(accessory.Data.Me);

            var stack1 = P4Stack[^1];
            var stack2 = P4Stack[^2];

            List<int> leftGroup = [0, 6, 2, 4];
            List<int> rightGroup = [1, 7, 3, 5];
            if (leftGroup.Contains(stack1) && leftGroup.Contains(stack2))
            {
                var change = leftGroup.IndexOf(stack1) < leftGroup.IndexOf(stack2) ? stack2 : stack1;
                leftGroup.Remove(change);
                leftGroup.Add(5);
                rightGroup.Remove(5);
                rightGroup.Add(change);
            }
            if (rightGroup.Contains(stack1) && rightGroup.Contains(stack2))
            {
                var change = rightGroup.IndexOf(stack1) < rightGroup.IndexOf(stack2) ? stack2 : stack1;
                rightGroup.Remove(change);
                rightGroup.Add(4);
                leftGroup.Remove(4);
                leftGroup.Add(change);
            }

            Vector3 dealpos = leftGroup.Contains(myindex) ? new(96.5f, 0, 113) : new(103.5f, 0, 113);

            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P4_第二段波动炮分摊位置";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 5000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        #endregion

        #region P5
        [ScriptMethod(name: "P5_开场_分P", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31621"], userControl: false)]
        public void P5_开场_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 5.0;
        }
        [ScriptMethod(name: "P5_一运_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31624"], userControl: false)]
        public void P5_一运_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 5.1;
            P51_Eye = 0;
            P51_Buff = [0, 0, 0, 0, 0, 0, 0, 0];
            P51_Fist = [0, 0, 0, 0];
            
            _phase = TopPhase.P5A1_DeltaVersion;
            InitParams();
            _pd.Init(accessory, "P5一运");
            _pd.AddPriorities([0, 1, 2, 3, 4, 5, 6, 7]);    // 依职能顺序添加优先值
            myArmUnitBiasEnable = false;
            accessory.Log.Debug($"当前阶段为：{_phase}");
        }
        
        [ScriptMethod(name: "P5_一运_眼睛激光", eventType: EventTypeEnum.EnvControl, eventCondition: ["DirectorId:800375AC", "Id:00020001"])]
        public void P5_一运_眼睛激光(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.1) return;
            var rot = @event["Index"] switch
            {
                "00000001" => 0,
                "00000002" => 1,
                "00000003" => 2,
                "00000004" => 3,
                "00000005" => 4,
                "00000006" => 5,
                "00000007" => 6,
                "00000008" => 7,
                _ => -1
            };
            if (rot == -1) return;
            var pos = RotatePoint(new(100, 0, 80), new(100, 0, 100), float.Pi / 4 * rot);
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_一运_眼睛激光";
            dp.Scale = new(16,40);
            dp.Position = pos;
            dp.TargetPosition = new(100, 0, 100);
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 7500;
            dp.DestoryAt = 12500;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
        }
        
        [ScriptMethod(name: "一运 远线记录", eventType: EventTypeEnum.Tether, eventCondition: ["Id:00C9"], userControl: Debugging)]
        public void P5_Delta_LocalRemoteTetherRecord(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;

            lock (_pd)
            {
                var targetId = ev.TargetId;
                var targetIdx = sa.GetPlayerIdIndex((uint)targetId);
                var sourceId = ev.SourceId;
                var sourceIdx = sa.GetPlayerIdIndex((uint)sourceId);

                var pdValMax = _pd.SelectSpecificPriorityIndex(0, true).Value;
                _pd.AddPriority(targetIdx, pdValMax >= 1000 ? 2000 : 1000);
                _pd.AddPriority(sourceIdx, pdValMax >= 1000 ? 2000 : 1000);
                _events[2].Set();   // 远线搭档记录完毕
            }
        }
        
        [ScriptMethod(name: "一运 非指挥模式记录头标", eventType: EventTypeEnum.Marker, eventCondition: ["Operate:Add", "Id:regex:^(0[123467])$"],
            userControl: Debugging)]
        public void P5_DeltaVersionReceiveMarker(Event ev, ScriptAccessory sa)
        {
            // 只取攻击1234与锁链12
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
        
            lock (_pd)
            {
                var mark = ev.Id0();
                sa.Log.Debug($"检测到ev.Id {mark}");
                var tid = ev.TargetId;
                var tidx = sa.GetPlayerIdIndex((uint)tid);
            
                _pd.AddActionCount();
                var pdVal = mark switch
                {
                    1 => 10,    // 攻击1
                    2 => 20,    // 攻击2
                    3 => 30,    // 攻击3
                    4 => 40,    // 攻击4
                    6 => 100,   // 锁链1
                    7 => 200,   // 锁链2
                    _ => 0
                };
                _pd.AddPriority(tidx, pdVal);
                if (_pd.ActionCount != 6) return;
                sa.Log.Debug($"ev[0] 一运，非指挥模式头标记录完毕。");
                sa.Log.Debug($"{_pd.ShowPriorities()}");
                _events[0].Set();   // 头标记录
            }
        }
        
        [ScriptMethod(name: "一运 定位光头", eventType: EventTypeEnum.PlayActionTimeline, eventCondition: ["SourceDataId:14669", "Id:7747"],
            userControl: Debugging)]
        public void P5_DeltaVersionFindOmegaBald(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            var spos = ev.SourcePosition;
            var dir = spos.Position2Dirs(Center, 4);
            _numbers[0] = dir;              // 光头
            _numbers[1] = (dir + 2) % 4;    // 蟑螂
            _events[1].Set();   // 光头蟑螂定位
            sa.Log.Debug($"ev[1] 光头位置：{_numbers[0]}，蟑螂位置：{_numbers[1]}记录完毕。");
        } 
        
        [ScriptMethod(name: "一运 初始位置指路 *", eventType: EventTypeEnum.PlayActionTimeline, eventCondition: ["SourceDataId:14669", "Id:7747"],
        userControl: true)]
        public void P5_DeltaVersionFirstGuidance(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            _events[0].WaitOne(5000);   // 触发判断时，若5000ms后仍未捕捉到Set信号，移除
            _events[1].WaitOne(2000);
            _events[2].WaitOne(2000);
            
            var myIndex = sa.GetMyIndex();
            var myPdVal = _pd.Priorities[myIndex];
            if (myPdVal >= 1000 & myPdVal.GetDecimalDigit(3) == 0)
            {
                // 玩家需找到锁链1/锁链2搭档
                // 锁链1的搭档，千位相同，百位为1。锁链2的搭档，千位相同，百位为2。
                // 若myPdVal的千位为1，则自身为降序第4，找降序第3(index 2)为搭档
                // 若myPdVal的千位为2，则自身为降序第2，找降序第1(index 0)为搭档
                var myPartner = _pd.SelectSpecificPriorityIndex(myPdVal >= 2000 ? 0 : 2, true);
                sa.Log.Debug($"玩家远线无世界，找到远线搭档为 {sa.GetPlayerJobByIndex(myPartner.Key)}");
                _pd.AddPriority(myIndex, myPartner.Value.GetDecimalDigit(3) * 100 + 10);  // 增加百位+10，如，对方是锁链2，则玩家+210
                sa.Log.Debug($"{_pd.ShowPriorities()}");
            }
            
            // 修订完毕后，将优先级值统一更改。
            // 注，当且仅当玩家是Stop1/Stop2时才会标注出，否则为0。
            for (int i = 0; i < 8; i++)
            {
                _pd.Priorities[i] = _pd.Priorities[i] % 1000 / 10;
            }
            sa.Log.Debug($"经矫正，{_pd.ShowPriorities()}");
            
            // var markerVal = myPdVal % 1000 / 10;  // 去掉千位与个位
            var markerVal = _pd.Priorities[myIndex];
            var omegaBaldDirection = _numbers[0];
            var beetleDirection = _numbers[1];

            Vector3 tpos1 = new(90f, 0, 94f);
            Vector3 tpos2 = new(110f, 0, 94f);

            tpos1 = markerVal switch
            {
                1 => tpos1.RotatePoint(Center, 90f.DegToRad() * beetleDirection),
                2 => tpos1.RotatePoint(Center, 90f.DegToRad() * beetleDirection),
                3 => (tpos1 - new Vector3(0, 0, 8)).RotatePoint(Center, 90f.DegToRad() *
                    beetleDirection),
                4 => (tpos1 - new Vector3(0, 0, 8)).RotatePoint(Center, 90f.DegToRad() *
                    beetleDirection),
                10 => tpos1.RotatePoint(Center, 90f.DegToRad() * omegaBaldDirection),
                11 => tpos1.RotatePoint(Center, 90f.DegToRad() * omegaBaldDirection),
                20 => (tpos1 - new Vector3(0, 0, 8)).RotatePoint(Center,
                    90f.DegToRad() * omegaBaldDirection),
                21 => (tpos1 - new Vector3(0, 0, 8)).RotatePoint(Center,
                    90f.DegToRad() * omegaBaldDirection),
                _ => new Vector3(100f, 0, 100f),
            };

            tpos2 = markerVal switch
            {
                1 => tpos2.RotatePoint(Center, 90f.DegToRad() * beetleDirection),
                2 => tpos2.RotatePoint(Center, 90f.DegToRad() * beetleDirection),
                3 => (tpos2 - new Vector3(0, 0, 8)).RotatePoint(Center, 90f.DegToRad() *
                    beetleDirection),
                4 => (tpos2 - new Vector3(0, 0, 8)).RotatePoint(Center, 90f.DegToRad() *
                    beetleDirection),
                10 => tpos2.RotatePoint(Center, 90f.DegToRad() * omegaBaldDirection),
                11 => tpos2.RotatePoint(Center, 90f.DegToRad() * omegaBaldDirection),
                20 => (tpos2 - new Vector3(0, 0, 8)).RotatePoint(Center,
                    90f.DegToRad() * omegaBaldDirection),
                21 => (tpos2 - new Vector3(0, 0, 8)).RotatePoint(Center,
                    90f.DegToRad() * omegaBaldDirection),
                _ => new Vector3(100f, 0, 100f),
            };

            var dp = sa.DrawGuidance(tpos1, 0, 5000, $"一运待命地点1");
            sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            dp = sa.DrawGuidance(tpos2, 0, 5000, $"一运待命地点2");
            sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            _events[0].Reset();
            _events[1].Reset();
            _events[2].Reset();
        }
    
        [ScriptMethod(name: "一运 记录拳头", eventType: EventTypeEnum.AddCombatant, eventCondition: ["DataId:regex:^(157(09|10))$"], userControl: Debugging)]
        public void P5_DeltaRocketPunchRecord(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            const uint blue = 15709;
            var dataid = JsonConvert.DeserializeObject<uint>(ev["DataId"]);
            var spos = ev.SourcePosition;
            lock (_pd)
            {
                _pd.AddActionCount();
                var myObj = sa.Data.MyObject;
                if (myObj == null) return;
                var a = myObj.Position;
                // 拳头出现时，记录自己的四分之一半场
                if (_pd.ActionCount == 7)
                    _numbers[2] = myObj.Position.Position2Dirs(Center, 4, false);
                if (spos.Position2Dirs(Center, 4, false) == _numbers[2])
                {
                    _numbers[3]++;      // 拳头数量
                    _numbers[4] += dataid == blue ? 1 : -1; // 拳头颜色
                    sa.Log.Debug($"捕捉到在玩家半场{_numbers[2]}出现了第{_numbers[3]}个拳头，为{(dataid == blue ? "蓝" : "黄")}色，记录值为{_numbers[4]}");
                    // _dv.PunchCountAtMyQuadrant++;
                    // _dv.PunchColorAtMyQuadrant += dataid == blue ? 1 : -1;
                }

                if (_pd.ActionCount == 14)  // 6个头标，8个拳头
                {
                    _events[3].Set();   // 拳头记录完毕
                    sa.Method.RemoveDraw($"一运待命地点.*");
                }
            }
        }
    
        [ScriptMethod(name: "一运 拳头待命指路 *", eventType: EventTypeEnum.AddCombatant, eventCondition: ["DataId:regex:^(157(09|10))$"],
            userControl: true, suppress: 10000)]
        public void P5_DeltaRocketPunchGuidance(Event ev, ScriptAccessory sa)
        {
            // var myIndex = accessory.GetMyIndex();
            // var myMarker = MarkType.Attack3;    // 我的头标
            //
            // _dv.MyQuadrant = 3;                 // 我在待命时的象限
            // _dv.PunchCountAtMyQuadrant = 3;     // 我在待命时，象限内拳头数量
            // _dv.PunchColorAtMyQuadrant = 0;     // 0代表象限内拳头异色，2或-2代表象限内拳头同色
            // _dv.OmegaBaldDirection = 2;         // 大光头的方位
            
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            _events[3].WaitOne(2000);   //  拳头记录完毕
            var myIndex = sa.GetMyIndex();
            var markerVal = _pd.Priorities[myIndex];
            var isOutside = markerVal is 3 or 4 or 20 or 21;    // 攻3，攻4，锁2，禁2
            var isRemoteTetherOutside = markerVal is 20 or 21;  // 锁2，禁2
            
            var omegaBaldDirection = _numbers[0];
            var beetleDirection = _numbers[1];
            var myQuadrant = _numbers[2];
            var punchCountAtMyQuadrant= _numbers[3];
            var punchColorAtMyQuadrant = _numbers[4];
            
            // 找到第0象限内，标点靠内。会随着boss位置的变化而出现偏移。
            var tposOut = new Vector3(108.7f, 0f, 90f).RotatePoint(new Vector3(109.9f, 0f, 90.1f),
                omegaBaldDirection % 2 == 1 ? -90f.DegToRad() : 0);
            var tposIn = new Vector3(102.7f, 0f, 90f).RotatePoint(new Vector3(109.9f, 0f, 90.1f),
                omegaBaldDirection % 2 == 1 ? -90f.DegToRad() : 0);  // 外锁链

            var tposBase = isRemoteTetherOutside ? tposIn : tposOut;
            
            tposBase = myQuadrant switch
            {
                1 => tposBase.FoldPointVertical(Center.Z),
                2 => tposBase.FoldPointVertical(Center.Z).FoldPointHorizon(Center.X),
                3 => tposBase.FoldPointHorizon(Center.X),
                _ => tposBase,
            };
            
            if (!isOutside)
            {
                // 此处的Outside是指标点是否偏大，靠场外。
                // 若玩家靠场内，无脑站象限点。
                
                var dp = sa.DrawGuidance(tposBase, 0, 5000, $"一运拳头");
                sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                sa.Log.Debug($"玩家靠场内，站第{myQuadrant}象限点。");
            }
            else
            {
                // 若玩家为外场
                // 玩家所在象限拳头数量不为2，有人没预占位（可能有武士画家），需要自己观察，不作指路
                if (punchCountAtMyQuadrant != 2)
                {
                    sa.Method.TextInfo($"观察同组拳头颜色是否交换。", 4000, true);
                    sa.Log.Debug($"第{myQuadrant}象限内拳头数量错误，需自己观察。");
                }
                // 玩家所在象限拳头数量为2，且颜色值不为0，则拳头同色，需要换位。
                else if (punchColorAtMyQuadrant != 0)
                {
                    var tpos = omegaBaldDirection % 2 == 1
                        ? tposBase.FoldPointVertical(Center.Z)
                        : tposBase.FoldPointHorizon(Center.X);
                    var dp = sa.DrawGuidance(tpos, 0, 5000, $"一运拳头");
                    sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                    sa.Log.Debug($"玩家{myQuadrant}象限内拳头同色，需要换位。");
                }
                else
                {
                    // 无需换位，直接根据_dv.myQuadrant指向对应位置
                    var dp = sa.DrawGuidance(tposBase, 0, 5000, $"一运拳头");
                    sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                    sa.Log.Debug($"玩家无需换位，站第{myQuadrant}象限点。");
                }
            }
            
            _events[3].Reset();
        }
    
        [ScriptMethod(name: "一运 拳头旋转引导位置", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:regex:^(009[CD])$"], userControl: true)]
        public void P5_DeltaArmUnitRotate(Event ev, ScriptAccessory sa)
        {
            // uint id = 0x009C;
            // uint tid = 0x4000C420;
            // var tpos = IbcHelper.GetById(tid)?.Position ?? Center;
            
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            var id = ev.Id0();
            var tid = ev.TargetId;
            var tpos = sa.GetById(tid)?.Position ?? Center;
            
            tpos = tpos.PointInOutside(Center, 1f);
            // RotateCW = 156
            var baitPos = tpos.RotatePoint(Center, id == 156 ? -5f.DegToRad() : 5f.DegToRad());
            var dp = sa.DrawStaticCircle(baitPos, sa.Data.DefaultSafeColor.WithW(3f), 0, 10000, $"手臂单元转转", 0.5f);
            sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Circle, dp);
        }
    
        private bool myArmUnitBiasEnable = false;
        [ScriptMethod(name: "一运 玩家引导拳头指路 *", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31587"],
            userControl: true)]
        public void P5_DeltaMyArmUnitBiasGuidance(Event ev, ScriptAccessory sa)
        {
            // var myIndex = accessory.GetMyIndex();
            // var myMarker = MarkType.Attack1;
            // var myPos = new Vector3(90f, 0, 90f);
            // _dv.OmegaBaldDirection = 2;
            
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            // 这一条判断会因为suppress被return回去，所以suppress的使用场景需注意，事件确认可被触发。否则就用bool。
            if (ev.TargetId != sa.Data.Me) return;
            if (_bools[0]) return;  // 玩家引导拳头指路是否绘画完成
            _bools[0] = true;
            myArmUnitBiasEnable = true;
            var myIndex = sa.GetMyIndex();
            var markerVal = _pd.Priorities[myIndex];
            var myPos = ev.TargetPosition;
            
            var omegaBaldDirection = _numbers[0];
            
            var omegaBaldDirection12 = omegaBaldDirection * 3;
            var omegaBaldPos = new Vector3(100, 0, 80).RotatePoint(Center, omegaBaldDirection12 * 30f.DegToRad());
            
            var isShieldTarget = markerVal is 10 or 11;     // 锁1，禁1，需往场中被盾击
            var isOutside = markerVal is 3 or 4 or 20 or 21;    // 攻3，攻4，锁2，禁2，偏场外
            var isAtRight = myPos.IsAtRight(omegaBaldPos, Center);
            var isBind = markerVal is 10 or 20 or 11 or 21; // 锁1，锁2，禁1，禁2
            
            var val = 100 * (isOutside ? 1 : 0) + 10 * (isAtRight ? 1 : 0) + 1 * (isBind ? 1 : 0);
            
            // myArmUnit
            _numbers[5] = val switch
            {
                111 => (omegaBaldDirection12 + 1) % 12,
                110 => (omegaBaldDirection12 + 5) % 12,
                101 => (omegaBaldDirection12 + 11) % 12,
                100 => (omegaBaldDirection12 + 7) % 12,
                10 => (omegaBaldDirection12 + 3) % 12,
                0 => (omegaBaldDirection12 + 9) % 12,
                
                11 => (omegaBaldDirection12 + 3) % 12,
                1 => (omegaBaldDirection12 + 9) % 12,
                
                _ => -1
            };
            var myArmUnit = _numbers[5];
            
            sa.Log.Debug(!isShieldTarget ? $"玩家所需引导手臂单元位于方位{myArmUnit}" : $"玩家需前往场中偏方位{myArmUnit}");
            sa.Method.TextInfo($"同组靠内集合，等待黄圈", 2000);

            if (!isShieldTarget)
            {
                sa.Log.Debug($"向引导拳头位置绘图");
                var armUnitPos = new Vector3(100, 0, 84).RotatePoint(Center, myArmUnit * 30f.DegToRad());
                var dp = sa.DrawGuidance(armUnitPos, 0, 4000, $"引导拳头指引", isSafe: false);
                sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            else
            {
                sa.Log.Debug($"向场中引导位置绘图");
                var armUnitPos = new Vector3(100, 0, 95).RotatePoint(Center, myArmUnit * 30f.DegToRad());
                var dp = sa.DrawGuidance(armUnitPos, 0, 4000, $"场中引导指引", isSafe: false);
                sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
        }
        
        [ScriptMethod(name: "一运 玩家引导拳头指路刷新", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31482"],
            userControl: Debugging, suppress: 10000)]
        public void P5_DeltaMyArmUnitBiasRefresh(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            sa.Method.RemoveDraw($"引导拳头指引");
            sa.Method.RemoveDraw($"场中引导指引");
            
            if (!myArmUnitBiasEnable) return;
            
            var myIndex = sa.GetMyIndex();
            var markerVal = _pd.Priorities[myIndex];
            var isShieldTarget = markerVal is 10 or 11;
            if (isShieldTarget) return;
            
            var myArmUnit = _numbers[5];
            
            sa.Log.Debug($"去引导拳头");
            var armUnitPos = new Vector3(100, 0, 84).RotatePoint(Center, myArmUnit * 30f.DegToRad());
            var dp = sa.DrawGuidance(armUnitPos, 0, 4000, $"引导拳头", isSafe: true);
            sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        
        [ScriptMethod(name: "一运 转转手引导指路删除", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31600"],
            userControl: Debugging, suppress: 10000)]
        public void P5_DeltaMyArmUnitBiasRemove(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            sa.Method.RemoveDraw($"引导拳头");
        }
        
        [ScriptMethod(name: "一运 玩家场中盾引导指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31482"],
            userControl: true, suppress: 10000)]
        public void P5_DeltaOmegaCenterShieldBias(Event ev, ScriptAccessory sa)
        {
            // var myIndex = accessory.GetMyIndex();
            // var myMarker = MarkType.Bind1;
            // _dv.MyArmUnit = 3;  // only 0, 3, 6, 9
            
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            var myIndex = sa.GetMyIndex();
            var markerVal = _pd.Priorities[myIndex];
            
            if (markerVal is not 10 and not 11) return;
            var myArmUnit = _numbers[5];
            
            var centerBiasPos = new Vector3(100, 0, 95).RotatePoint(Center, myArmUnit * 30f.DegToRad());
            var dp = sa.DrawGuidance(centerBiasPos, 0, 3000, $"场中盾连击引导");
            sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        
        [ScriptMethod(name: "一运 转转手引导后近线待命指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31600"],
            userControl: true, suppress: 10000)]
        public void P5_DeltaAfterArmUnitBiasGuidance(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            var myIndex = sa.GetMyIndex();
            var markerVal = _pd.Priorities[myIndex];

            if (markerVal is not 1 and not 2 and not 3 and not 4) return;
            var myArmUnit = _numbers[5];
        
            var standByPos = new Vector3(100, 0, 86).
                RotatePoint(Center, MathF.Round((float)myArmUnit * 2 / 3) * 45f.DegToRad());
            var dp = sa.DrawGuidance(standByPos, 0, 6000, $"攻击头标标点待命");
            sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
    
        [ScriptMethod(name: "一运 光头左右扫描记录", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(3163[89])$"],
            userControl: Debugging)]
        public void P5_DeltaOmegaBaldCannonRecord(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            const uint right = 31638;
            _numbers[6] = ev.ActionId == right ? 1 : 2;
            // var omegaBaldCannonType = _numbers[6]
        }
    
        [ScriptMethod(name: "一运 玩家小电视Buff记录", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(345[23])$"],
            userControl: Debugging)]
        public void P5_DeltaPlayerCannonRecord(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            var tidx = sa.GetPlayerIdIndex((uint)ev.TargetId);
            _pd.AddPriority(tidx, 100);    // 小电视点名+100
            // _dv.PlayerCannonSource = tidx;
            const uint right = 3452;
            _numbers[7] = ev.StatusId == right ? 1 : 2; // 右刀1，左刀2
            // var playerCannonType = _numbers[7]
            // _dv.PlayerCannonType = ev.StatusId == right ? 1 : 2;
        }
        
        [ScriptMethod(name: "一运 盾连击目标记录", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31528"],
        userControl: Debugging)]
        public void P5_DeltaShieldTargetRecord(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            if (JsonConvert.DeserializeObject<uint>(ev["TargetIndex"]) != 1) return;
            sa.Method.RemoveDraw($"场中盾连击引导");
            
            var tidx = sa.GetPlayerIdIndex((uint)ev.TargetId);
            _pd.AddPriority(tidx, 1000);    // 盾连击目标+1000
            // _dv.ShieldTarget = tidx;
            // 盾连击是一运流程的最后一环
            _events[4].Set();   // (int)RecordedIdx.ShieldTargetRecorded
        }
        
        [ScriptMethod(name: "一运 分摊与小电视指路", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31528"],
            userControl: true)]
        public void P5_DeltaStackAndCannonGuidance(Event ev, ScriptAccessory sa)
        {
            // var myIndex = accessory.GetMyIndex();
            // var myMarker = MarkType.Bind2;
            // _dv.PlayerCannonSource = myIndex;        // 小电视玩家Idx
            // _dv.ShieldTarget = myIndex;              // 盾连击玩家Idx
            // _dv.OmegaBaldDirection = 0;              // 光头位置
            // _dv.OmegaBaldCannonType = 2;             // 光头电视1右2左
            // _dv.PlayerCannonType = 1;                // 玩家电视1右2左
            
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            _events[4].WaitOne(1000);
            var myIndex = sa.GetMyIndex();
            var markerVal = _pd.Priorities[myIndex] % 100;  // 不需要知道百位数
            var myPriVal = _pd.Priorities[myIndex];  // 不需要知道百位数
            
            if (markerVal is 1 or 2 or 3 or 4)
            {
                // 近线组不参与讨论
                _events[4].Reset();
                return;
            }
            
            // 盾连击目标是否等于小电视目标，玩家是否为小电视目标
            // 以光头在A，光头电视打右为基准，以光头为12点做旋转。
            
            // var isSameTarget = _dv.PlayerCannonSource == _dv.ShieldTarget;
            var isSameTarget = _pd.SelectSpecificPriorityIndex(0, true).Value >= 1100;
            
            // 如果盾连击目标与小电视目标相同，盾连击目标需往外一步，分摊目标需往内一步
            var shieldTargetPos = new Vector3(101, 0, 85) + (isSameTarget ? new Vector3(3.5f, 0, 0) : new Vector3(0, 0, 0));
            var stackPos = new Vector3(101, 0, 100) + (isSameTarget ? new Vector3(0, 0, 0) : new Vector3(3.5f, 0, 0));

            var omegaBaldDirection = _numbers[0];
            var omegaBaldCannonType = _numbers[6];
            var playerCannonType = _numbers[7];
                
            // _dv.OmegaBaldCannonType 1右2左
            if (omegaBaldCannonType == 2)
            {
                // 左刀则折叠后再旋转
                shieldTargetPos = shieldTargetPos.FoldPointHorizon(Center.X);
                stackPos = stackPos.FoldPointHorizon(Center.X);
            }
            
            var rotateRad = omegaBaldDirection * 90f.DegToRad();
            shieldTargetPos = shieldTargetPos.RotatePoint(Center, rotateRad);
            stackPos = stackPos.RotatePoint(Center, rotateRad);

            if (myPriVal / 1000 == 1)   // >1000，盾连击目标
            {
                var dp = sa.DrawGuidance(shieldTargetPos, 0, 5000, $"一运盾连击指路");
                sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            else
            {
                var dp = sa.DrawGuidance(stackPos, 0, 5000, $"一运分摊指路");
                sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }

            if (myPriVal % 1000 >= 100)    // >100，小电视目标
            {
                var faceDir = (omegaBaldDirection + (omegaBaldCannonType != playerCannonType ? 2 : 0)) % 4;
                var dp = sa.DrawStatic((ulong)sa.Data.Me, (ulong)0, 0, faceDir * 90f.DegToRad().Logic2Game(),
                    1f, 4.5f, sa.Data.DefaultSafeColor, 0, 5000, $"小电视面向辅助-正确面向");
                dp.FixRotation = true;
                sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Arrow, dp);
                
                // 由于DrawStatic一般用于静态方案，所以在rotation中加入了Game2Logic。跟随单位的需要额外加上Game2Logic。
                var dp0 = sa.DrawStatic((ulong)sa.Data.Me, (ulong)0, 0, 0f.Logic2Game(),
                    1f, 4.5f, sa.Data.DefaultDangerColor, 0, 5000, $"小电视面向辅助-自身");
                sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Arrow, dp0);
                
                sa.Method.TextInfo($"小电视，站在外侧", 3000, true);
            }
            else
                sa.Method.TextInfo($"躲避小电视，站在内侧", 3000, true);
            
            _events[4].Reset();
        }

        [ScriptMethod(name: "P5_一运_小电视辅助", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(3163[89])$"], userControl: true)]
        public async void P5_一运_小电视辅助(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            var myIndex = sa.GetMyIndex();
            var myPriVal = _pd.Priorities[myIndex];
            if (myPriVal % 1000 < 100) return;
            var me = sa.Data.MyObject;
            if (me == null) return;
            P5_TV_Support_enable = true;
            bool? oldState = null;
            var rot = ev.SourceRotation;
            if (ev.ActionId == 31638 && _numbers[7] == 1 || ev.ActionId == 31639 && _numbers[7] == 2 )
            {
                rot = rot + float.Pi;
            }

            await Task.Delay(6000);
            while (P5_TV_Support_enable)
            {
                await Task.Delay(100);
                bool state = me.Rotation.Equals(rot);
                if (oldState == state || IsMoving)
                {
                    // sa.Log.Debug($"面向为{me.Rotation}，保持");
                }   
                else
                {
                    SetRotation(sa, me, rot);
                    oldState = state;
                }
            }
        }
        
        public static bool IsMoving
        {
            get
            {
                bool isMoving = false;
                unsafe
                {
                    FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMap* ptr = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMap.Instance();
                    if (ptr is not null)
                    {
                        isMoving = ptr->IsPlayerMoving;
                    }
                }
                return isMoving;
            }
        }
        
        [ScriptMethod(name: "P5_一运_小电视辅助关闭", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(3163[89])$"], userControl: false)]
        public async void P5_一运_小电视辅助关闭(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            P5_TV_Support_enable = false;
        }
        
        [ScriptMethod(name: "一运 绘图删除，准备一传", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(31529)$"],
            userControl: Debugging)]
        public void P5_DeltaVersionComplete(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A1_DeltaVersion) return;
            _phase = TopPhase.P5A2_DeltaWorld;
            sa.Method.RemoveDraw("小电视.*");
            sa.Method.RemoveDraw("一运.*");
            
            // 初始化_events
            _events = Enumerable
                .Range(0, 20)
                .Select(_ => new ManualResetEvent(false))
                .ToList();
            
            for (int i = 0; i < 8; i++)
            {
                _pd.Priorities[i] %= 100;    // 保留个位与十位
            }
            sa.Log.Debug($"一传：经矫正，{_pd.ShowPriorities()}");
        }
        
        [ScriptMethod(name: "----《P5 一传》----", eventType: EventTypeEnum.NpcYell, eventCondition: ["HelloMyWorld"],
            userControl: true)]
        public void SplitLine_DeltaWorld(Event ev, ScriptAccessory sa)
        {
        }
    
        [ScriptMethod(name: "一传 蟑螂左右刀记录", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(3163[67])$"],
            userControl: Debugging)]
        public void P5_DeltaBeetleSwipeRecord(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A2_DeltaWorld) return;
            const uint right = 31636;
            _numbers[8] = ev.ActionId == right ? 1 : 2;
            // var beetleSwipe = _numbers[8];
            _events[0].Set();
            // (int)RecordedIdx.BeetleSwipeRecorded
        }
        
        [ScriptMethod(name: "一传 蟑螂左右刀", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(3163[67])$"],
            userControl: true)]
        public void P5_一传_蟑螂左右刀(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A2_DeltaWorld) return;
            var rot = ev.ActionId == 31636 ? -float.Pi / 2 : float.Pi / 2;
            var dp = sa.Data.GetDefaultDrawProperties();
            dp.Name = "P5_一传 蟑螂左右刀";
            dp.Radian = float.Pi + float.Pi / 6;
            dp.Scale = new(90);
            dp.Rotation = rot;
            dp.Owner = ev.SourceId;
            dp.Color = sa.Data.DefaultDangerColor;
            dp.DestoryAt = 10000;
            sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
        } 

        [ScriptMethod(name: "一传 指路 *", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(3163[67])$"],
            userControl: true)]
        public void P5_DeltaWorldGuidance(Event ev, ScriptAccessory sa)
        {
            // var myMarker = MarkType.Bind2;
            // _dv.BeetleSwipe = 2;    // 1右2左
            // _dv.BeetleDirection = 1;
            
            if (_phase != TopPhase.P5A2_DeltaWorld) return;
            _events[0].WaitOne();
            var myIndex = sa.GetMyIndex();
            // var myMarker = _dv.GetMarkers()[myIndex];
            var markerVal = _pd.Priorities[myIndex];
            
            // 以蟑螂右刀，蟑螂在A为基准
            List<Vector3> posList =
            [
                new(102f, 0, 81f),          // Atk1 - FarTarget
                new(110.6f, 0, 116.2f),     // Atk2 - FarTarget
                new(108.9f, 0, 88.9f),      // Atk3 - NearTarget
                new(113.7f, 0, 86.3f),      // Atk4 - NearTarget
                new(119.5f, 0, 100f),       // Bind1 - FarSource
                new(106.5f, 0, 100f),       // Bind2 - NearSource
                new(116.2f, 0, 111f),       // Stop1 - Idle
                new(116.2f, 0, 111f)        // Stop2 - Idle
            ];

            var myPosIdx = markerVal switch
            {
                1 => 0,
                2 => 1,
                3 => 2,
                4 => 3,
                10 => 4,
                20 => 5,
                11 => 6,
                21 => 7,
                _ => -1,
            };
            
            if (myPosIdx == -1)
            {
                sa.Log.Debug($"玩家标点信息{markerVal}读取错误");
                return;
            }

            var beetleSwipe = _numbers[8];
            var beetleDirection = _numbers[1];
            
            // 根据蟑螂左右刀与所在方位旋转折叠
            var myPos = posList[myPosIdx];
            var isRightSwipe = beetleSwipe == 1;
            if (!isRightSwipe)
                myPos = myPos.FoldPointHorizon(Center.X);
            myPos = myPos.RotatePoint(Center, beetleDirection * 90f.DegToRad());

            var dp = sa.DrawGuidance(myPos, 0, 5000, $"一传指路");
            sa.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            _events[0].Reset();
        }
        
        [ScriptMethod(name: "一传 指路移除", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(3163[67])$"],
            userControl: Debugging)]
        public void P5_DeltaWorldGuidanceRemove(Event ev, ScriptAccessory sa)
        {
            if (_phase != TopPhase.P5A2_DeltaWorld) return;
            sa.Method.RemoveDraw("一传指路");
        }
        
        [ScriptMethod(name: "一传 近线拉线提示", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:1672"],
            userControl: true)]
        public void P5_DeltaWorldLocalTetherBreakHint(Event ev, ScriptAccessory sa)
        {
            // TODO 该功能未实现，目前未知原因
            // 在DeltaVersion期间建立了近线
            if (_phase != TopPhase.P5A2_DeltaWorld) return;

            // 在线变为实线前，标点已取好
            var myIndex = sa.GetMyIndex();
            var markerVal = _pd.Priorities[myIndex];
            if (markerVal is not 1 and not 2) return;
            
            // BreakLocalTether = 1
            if (_bools[1]) return;
            _bools[1] = true;

            // 找到攻击1/2
            var myPartner = _pd.SelectSpecificPriorityIndex(markerVal == 1 ? 1 : 0).Key;
            var dur = (int)JsonConvert.DeserializeObject<uint>(ev["DurationMilliseconds"]);
            sa.Log.Debug($"我的标点是Attack1或Attack2，将与{sa.GetPlayerJobByIndex(myPartner)}画拉线提示。{dur}");
            
            // 在还剩10秒时，DeltaWorld正在执行，需在最后2秒拉断。

            var delay1 = dur - 8000;
            var destroy1 = 6000;
            var delay2 = dur - 3000;
            var destroy2 = 3000;

            // 近线实际距离约为10，取11
            var dp1 = sa.DrawCircle(sa.Data.PartyList[myPartner], 11, delay1, destroy1, $"近线别拉断");
            dp1.Color = sa.Data.DefaultDangerColor;
            sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp1);
            
            var dp2 = sa.DrawCircle(sa.Data.PartyList[myPartner], 11, delay2, destroy2, $"近线拉断");
            dp2.Color = sa.Data.DefaultSafeColor;
            sa.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp2);
        }
        
        [ScriptMethod(name: "P5_二运_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:32788"], userControl: false)]
        public void P5_二运_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 5.2;
            accessory.Log.Debug($"--- parse 更改为 {parse}");
            _phase = TopPhase.P5B1_SigmaVersion;
            InitParams();
            _pd.Init(accessory, "P5二运");
            P52_semaphoreTowersWereConfirmed = new AutoResetEvent(false);
        }
        
        [ScriptMethod(name: "P5_二运_男人位置", eventType: EventTypeEnum.AddCombatant, eventCondition: ["DataId:15724"],userControl: false)]
        public void P5_二运_男人位置(Event @event, ScriptAccessory sa)
        {
            if (parse != 5.2) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            P52_OmegaMDir = (RoundPositionTo8Dir(pos, new(100, 0, 100))+4)%8;
            sa.Log.Debug($"P5二运 男人方位：{P52_OmegaMDir}");
            
        }
        
        [ScriptMethod(name: "P5_二运_远近Buff", eventType: EventTypeEnum.StatusAdd, eventCondition: ["StatusID:regex:^(3427|3428)$"], userControl: false)]
        public void P5_二运_远近Buff(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.2) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            P5_SigmaBuffIsFar = @event["StatusID"] == "3428";
        }
        
        [ScriptMethod(name: "P5_二运_前半站位", eventType: EventTypeEnum.Marker, eventCondition: ["Operate:Add", "Id:regex:^(0[1-46-8]|12)$"], userControl: true)]
        public void P5_二运_前半站位(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.2) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if (accessory.Data.PartyList.IndexOf(tid) != accessory.Data.PartyList.IndexOf(accessory.Data.Me)) return;
            accessory.Log.Debug($"P5二运 玩家头标：{@event["Id"]}");
            P52_Self_Pos = @event["Id"] switch
            {
                "01" => P5_SigmaBuffIsFar
                    ? RotatePoint(new Vector3(92.73f, 0, 82.45f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir)
                    : RotatePoint(new Vector3(95.03f, 0, 87.99f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir),
                "02" => P5_SigmaBuffIsFar
                    ? RotatePoint(new Vector3(83.37f, 0, 93.11f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir)
                    : RotatePoint(new Vector3(89.84f, 0, 95.79f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir),
                "03" => P5_SigmaBuffIsFar
                    ? RotatePoint(new Vector3(82.45f, 0, 107.27f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir)
                    : RotatePoint(new Vector3(89.843f, 0, 104.21f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir),
                "04" => P5_SigmaBuffIsFar
                    ? RotatePoint(new Vector3(92.73f, 0, 117.55f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir)
                    : RotatePoint(new Vector3(95.79f, 0, 110.16f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir),
                "06" => P5_SigmaBuffIsFar
                    ? RotatePoint(new Vector3(107.27f, 0, 82.45f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir)
                    : RotatePoint(new Vector3(104.97f, 0, 87.99f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir),
                "07" => P5_SigmaBuffIsFar
                    ? RotatePoint(new Vector3(116.63f, 0, 93.11f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir)
                    : RotatePoint(new Vector3(110.16f, 0, 95.79f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir),
                "08" => P5_SigmaBuffIsFar
                    ? RotatePoint(new Vector3(117.55f, 0, 107.27f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir)
                    : RotatePoint(new Vector3(110.16f, 0, 104.21f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir),
                "12" => P5_SigmaBuffIsFar
                    ? RotatePoint(new Vector3(107.27f, 0, 117.55f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir)
                    : RotatePoint(new Vector3(104.21f, 0, 110.16f), new(100, 0, 100), float.Pi / 4 * P52_OmegaMDir),
            };
            
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_二运_前半站位";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = P52_Self_Pos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 9000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        
        [ScriptMethod(name: "P5_二运_塔位置", eventType: EventTypeEnum.ObjectChanged, eventCondition: ["Operate:Add", "DataId:regex:^(2013245|2013246)$"],userControl: false)]
        public void P5_二运_塔位置(Event @event, ScriptAccessory sa)
        {
            if (parse != 5.2) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            var dir = RoundPositionTo16Dir(pos, new(100, 0, 100));
            P52_TowerPos[dir] = pos;
            if (@event["DataId"] == "2013245")
            {
                P52_Towers[dir] = 1;
            }else if (@event["DataId"] == "2013246")
            {
                P52_Towers[dir] = 2;
            }
            sa.Log.Debug($"P5二运 塔方位 {dir} 为 {P52_Towers[dir]} 人塔");
            int count = P52_Towers.Count(x => x != 0);
            if (count == 6 && !P5_SigmaBuffIsFar)
            {
                sa.Log.Debug($"P5二运 场上找到了 {count} 座塔");
                P52_semaphoreTowersWereConfirmed.Set();
            }
            else if(count == 5 && P5_SigmaBuffIsFar)
            {
                sa.Log.Debug($"P5二运 场上找到了 {count} 座塔");
                P52_semaphoreTowersWereConfirmed.Set();
            }
        }
        
        [ScriptMethod(name: "P5_二运_击退塔指路", eventType: EventTypeEnum.ObjectChanged, eventCondition: ["Operate:Add","DataId:regex:^(2013245|2013246)$"],userControl: true)]
        public void P5_二运_击退塔指路(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.2) return;
            Thread.MemoryBarrier();
            P52_semaphoreTowersWereConfirmed.WaitOne();
            Thread.MemoryBarrier();
            P52_Self_Dir = RoundPositionTo16Dir(P52_Self_Pos, new(100, 0, 100));
            accessory.Log.Debug($"P5二运击退塔前 玩家所在八方方位 {P52_Self_Dir}");
            
            int tempCwDirIndex;
            int tempCcwDirIndex;
            int tempSide = 0;
            int targetIndex = 0;
            

            if (P5_SigmaBuffIsFar)
            {
                accessory.Log.Debug($"是 远Buff");
                tempCwDirIndex = (P52_Self_Dir + 15) % 16;
                tempCcwDirIndex = (P52_Self_Dir + 1) % 16;
            }
            else
            {
                accessory.Log.Debug($"是 近Buff");
                tempCwDirIndex = (P52_Self_Dir + 14) % 16;
                tempCcwDirIndex = (P52_Self_Dir + 2) % 16;
            }

            accessory.Log.Debug($"即将对（{tempCwDirIndex}与{tempCcwDirIndex}），进行检查");
            accessory.Log.Debug($"CWDir：{P52_Towers[tempCwDirIndex]}，CCWDir：{P52_Towers[tempCcwDirIndex]}");
            
            if (P52_Towers[tempCwDirIndex] == 2)
            {
                accessory.Log.Debug($"面向塔，逆时针方向（{tempCwDirIndex}），存在双人塔！");
                tempSide = 1;
            }
            else if (P52_Towers[tempCcwDirIndex] == 2)
            {
                accessory.Log.Debug($"面向塔，顺时针方向（{tempCcwDirIndex}），存在双人塔");
                tempSide = 2;
            }
            else
            {
                bool cwIsSingle = P52_Towers[tempCwDirIndex] == 1;
                bool ccwIsNotDouble = P52_Towers[tempCcwDirIndex] != 2;

                if (cwIsSingle && ccwIsNotDouble)
                {
                    accessory.Log.Debug($"面向塔，逆时针方向（{tempCwDirIndex}），仅存在单人塔");
                    tempSide = 1;
                }
                else
                {
                    bool ccwIsSingle = P52_Towers[tempCcwDirIndex] == 1;
                    bool cwIsNotDouble = P52_Towers[tempCwDirIndex] != 2;

                    if (ccwIsSingle && cwIsNotDouble)
                    {
                        accessory.Log.Debug($"面向塔，顺时针方向（{tempCcwDirIndex}），仅存在单人塔");
                        tempSide = 2;
                    }
                }
            }

            if (tempSide != 0)
            {
                targetIndex = tempSide == 1 ? tempCwDirIndex : tempCcwDirIndex;
            }

            var str = "Towers: [";
            for (int i = 0; i < P52_Towers.Length; i++)
            {
                str += $"{P52_Towers[i]}, ";
            }
            str += "]";
            accessory.Log.Debug($"{str}");
            
            accessory.Log.Debug($"目标塔为：{targetIndex}");
            
            var dp1 = accessory.Data.GetDefaultDrawProperties();
            dp1.Name = "P52_击退指路起点";
            dp1.Scale = new(2);
            dp1.Owner = accessory.Data.Me;
            dp1.TargetPosition = RotatePoint(new Vector3(100,0,97), new Vector3(100,0,100), targetIndex * float.Pi / 8);
            dp1.ScaleMode |= ScaleMode.YByDistance;
            dp1.Color = accessory.Data.DefaultSafeColor;
            dp1.DestoryAt = 4500;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp1);
            
            var dp2 = accessory.Data.GetDefaultDrawProperties();
            dp2.Name = "P52_击退指路";
            dp2.Scale = new(1.5f, 13);
            dp2.ScaleMode |= ScaleMode.YByDistance;
            dp2.Owner = accessory.Data.Me;
            dp2.TargetPosition = P52_TowerPos[targetIndex];
            dp2.Rotation = 0;
            dp2.Color = accessory.Data.DefaultSafeColor;
            dp2.DestoryAt = 6500;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp2);
        }
        
        [ScriptMethod(name: "P5_二运_后半", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:regex:^(31555)$"],userControl: false)]
        public void P5_二运_后半(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.2) return;
            parse = 5.21;
            accessory.Log.Debug($"--- parse 更改为 {parse}");
        }
        
        [ScriptMethod(name: "P5_二三四传头标记录", eventType: EventTypeEnum.Marker, eventCondition: ["Operate:Add", "Id:regex:^(0[1-46-7]|09|10)$"], userControl: false)]
        public void P5_二三四传头标记录(Event @event, ScriptAccessory accessory)
        {
            if (parse < 5.2) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            if (accessory.Data.PartyList.IndexOf(tid) != accessory.Data.PartyList.IndexOf(accessory.Data.Me)) return;
            
            P52_MarkType = @event["Id"] switch
            {
                "01" => 1, //攻击1
                "02" => 2, //攻击2
                "03" => 3, //攻击3
                "04" => 4, //攻击4
                "06" => 5, //锁链1
                "07" => 6, //锁链2
                "09" => 7, //禁止1
                "10" => 8, //禁止2
            };
            accessory.Log.Debug($"--- 玩家头标被更新于 {parse}, 为 {P52_MarkType}");
        }
        
        [ScriptMethod(name: "P5_二运_女人位置", eventType: EventTypeEnum.PlayActionTimeline, eventCondition: ["Id:7747", "SourceDataId:15720"], userControl: false)]
        public void P5_二运_女人位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.21) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            if (!P52_OmegaFDirDone)
            {
                P52_OmegaFDir = RoundPositionTo8Dir(pos, new(100, 0, 100));
                P52_OmegaFDirDone = true;
                accessory.Log.Debug($"P52_OmegaFDir:{P52_OmegaFDir}");
            }
        }
        
        [ScriptMethod(name: "P5_二运_旋转激光", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:regex:^(009C|009D)$"],userControl: true)]
        public void P5_二运_旋转激光(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.21) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
            int P52_OmegaFfixDir = P52_OmegaFDir switch
            {
                0 => 0,
                1 => 3,
                2 => 2,
                3 => 1,
                4 => 0,
                5 => 3,
                6 => 2,
                7 => 1,
            };
            
            int dx = -1;
            if (@event["Id"] == "009D")
            {
                dx = 1;
            }
            for (int i = 0; i < 14; i++)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = $"P5_二运_旋转激光_{i}";
                dp.Scale = new(50,12);
                dp.Owner = tid;
                dp.FixRotation = true;
                dp.Rotation = float.Pi / 2 + float.Pi / 4 * P52_OmegaFfixDir + float.Pi / 20 * i * dx;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 10000 + 580*i;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
            }
        }
        
        [ScriptMethod(name: "P5_二运_后半起跑点指路", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:regex:^(009C|009D)$"],userControl: true)]
        public void P5_二运_后半起跑点指路(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.21) return;
            Vector3 dealpos = Vector3.Zero;
            string eventId = @event["Id"];
            var positionMap = new Dictionary<string, Dictionary<bool, Vector3>>
            {
                ["009C"] = new Dictionary<bool, Vector3>
                {
                    { true, new Vector3(93f, 0f, 82f) },
                    { false, new Vector3(107f, 0f, 118f) }
                },
                ["009D"] = new Dictionary<bool, Vector3>
                {
                    { true, new Vector3(107f, 0f, 82f) },
                    { false, new Vector3(93f, 0f, 118f) }
                }
            };
            Vector3 center = new Vector3(100f, 0f, 100f);
            if (positionMap.TryGetValue(eventId, out var idMap))
            {
                bool isSpecialType = P52_MarkType == 7 || P52_MarkType == 8 || P52_MarkType == 1;
                float angle = float.Pi / 4 * P52_OmegaFDir;
                dealpos = RotatePoint(idMap[isSpecialType], center, angle);
            }
            accessory.Log.Debug($"P5_二运_后半起跑点:{dealpos}");
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_二运_后半起跑点";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 10000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }

        [ScriptMethod(name: "P5_二运_女人技能", eventType: EventTypeEnum.PlayActionTimeline, eventCondition: ["Id:7747", "SourceDataId:15720"], userControl: true)]
        public void P5_二运_女人技能(Event @event, ScriptAccessory accessory)
        {
            if (P52_OmegaM_Skill || parse != 5.21) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            P52_F_TransformationID = GetTransformationID(sid, accessory);
            P52_OmegaM_Skill = true;
            accessory.Log.Debug($"P52_F_TransformationID:{P52_F_TransformationID}");
            if (P52_F_TransformationID == null) return;
            accessory.Log.Debug($"--- 进入了P5_二运_女人技能");
            if (P52_F_TransformationID == 4)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P5_二运_女辣翅1";
                dp.Scale = new(60, 20);
                dp.Owner = sid;
                dp.Rotation = float.Pi / 2;
                dp.Offset = new(-4, 0, 0);
                dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                dp.Delay = 12000;
                dp.DestoryAt = 3000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P5_二运_女辣翅2";
                dp.Scale = new(60, 20);
                dp.Owner = sid;
                dp.Rotation = float.Pi / -2;
                dp.Offset = new(4, 0, 0);
                dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                dp.Delay = 12000;
                dp.DestoryAt = 3000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
            }
            else
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P5_二运_女十字1";
                dp.Scale = new(10, 60);
                dp.Owner = sid;
                dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                dp.Delay = 12000;
                dp.DestoryAt = 3000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P5_二运_女十字2";
                dp.Scale = new(10, 60);
                dp.Rotation = float.Pi / 2;
                dp.Owner = sid;
                dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                dp.Delay = 12000;
                dp.DestoryAt = 3000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
            }
        }
        
        [ScriptMethod(name: "P5_二运_二传起跑提示", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31631"], userControl: true)]
        public void P5_二运_二传起跑提示(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.21) return;
            if (P52_F_TransformationID == 4)
            {
                accessory.Method.TextInfo("等待激光判定后穿入", 3000, true);
            }
            else
            {
                accessory.Method.TextInfo("等待十字判定后穿入", 5000, true);
            }
        }
        
        [ScriptMethod(name: "P5_二运_二传指路", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:regex:^(009C|009D)$"],userControl: true)]
        public void P5_二运_二传指路(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.21) return;
            int dx = 1;
            if (@event["Id"] == "009C")
            {
                dx = -1;
            }
            Vector3 dealpos = Vector3.Zero;
            dealpos = P52_MarkType switch
            {
                1 => RotatePoint(new Vector3(100f+ (-19.5f*dx), 0f, 100f), new Vector3(100, 0, 100),
                    float.Pi / 4 * P52_OmegaFDir),
                2 => RotatePoint(new Vector3(100f+ 19.5f*dx, 0f, 100f), new Vector3(100, 0, 100),
                    float.Pi / 4 * P52_OmegaFDir),
                3 => RotatePoint(new Vector3(102f, 0f, 111f), new Vector3(100, 0, 100),
                    float.Pi / 4 * P52_OmegaFDir),
                4 => RotatePoint(new Vector3(105.26f, 0f, 118.26f), new Vector3(100, 0, 100),
                    float.Pi / 4 * P52_OmegaFDir),
                5 => RotatePoint(new Vector3((100 + dx * 10), 0f, 100f), new Vector3(100, 0, 100),
                    float.Pi / 4 * P52_OmegaFDir),
                6 => RotatePoint(new Vector3(94.74f, 0f, 118.26f), new Vector3(100, 0, 100),
                    float.Pi / 4 * P52_OmegaFDir),
                7 => RotatePoint(new Vector3(86.56f, 0f, 86.56f), new Vector3(100, 0, 100),
                    float.Pi / 4 * P52_OmegaFDir),
                8 => RotatePoint(new Vector3(113.44f, 0f, 86.56f), new Vector3(100, 0, 100),
                    float.Pi / 4 * P52_OmegaFDir),
            };
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_二运_二传";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.Delay = P52_F_TransformationID == 4? 10000 : 13000;
            dp.DestoryAt = P52_F_TransformationID == 4? 9000 : 7000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        
        [ScriptMethod(name: "P5_三运_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:32789"], userControl: false)]
        public void P5_三运_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 5.3;
        }
        
        [ScriptMethod(name: "P5_三运_扩散波动炮", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(31643|31644)$"], userControl: true)]
        public void P5_三运_扩散波动炮(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.3) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            float rot = JsonConvert.DeserializeObject<int>(@event["ActionId"]) == 31644 ? float.Pi / 2 : 0;
            
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_扩散波动炮一段";
            dp.Scale = new(20);
            dp.Radian = float.Pi / 3 * 2;
            dp.Owner = sid;
            dp.Rotation = rot;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 9000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
            
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_扩散波动炮一段";
            dp.Scale = new(20);
            dp.Radian = float.Pi / 3 * 2;
            dp.Owner = sid;
            dp.Rotation = rot + float.Pi;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 9000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
            
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_扩散波动炮二段";
            dp.Scale = new(20);
            dp.Radian = float.Pi / 3 * 2;
            dp.Owner = sid;
            dp.Rotation = rot + float.Pi / 2;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 9000;
            dp.DestoryAt = 4000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
            
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_扩散波动炮二段";
            dp.Scale = new(20);
            dp.Radian = float.Pi / 3 * 2;
            dp.Owner = sid;
            dp.Rotation = rot + float.Pi + float.Pi / 2;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.Delay = 9000;
            dp.DestoryAt = 4000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
            
        }
        
        [ScriptMethod(name: "P5_三运_男女组合技", eventType: EventTypeEnum.PlayActionTimeline, eventCondition: ["Id:7747", "SourceDataId:regex:^(15721|15722)$"],userControl: true)]
        public void P5_三运_男女组合技(Event @event, ScriptAccessory accessory)
        {
            if(parse != 5.3) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            P5_3_MF++;
            var transformationID = GetTransformationID(sid, accessory);
            if (transformationID == null) return;
            if (@event["SourceDataId"] == "15721")
            {
                //男
                if (transformationID == 0)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = $"P5_三运_男钢铁{P5_3_MF}";
                    dp.Scale = new(10);
                    dp.Owner = sid;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.Delay = P5_3_MF < 3 ? 0 : 9000;
                    dp.DestoryAt = P5_3_MF < 3? 13000 : 4000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
                }
                if (transformationID == 4)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = $"P5_三运_男月环{P5_3_MF}";
                    dp.Scale = new(40);
                    dp.InnerScale = new(10);
                    dp.Radian = float.Pi * 2;
                    dp.Owner = sid;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.Delay = P5_3_MF < 3 ? 0 : 9000;
                    dp.DestoryAt = P5_3_MF < 3? 13000 : 4000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);
                }
            }
            if (@event["SourceDataId"] == "15722")
            {
                if (transformationID == 0)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = $"P5_三运_女十字1{P5_3_MF}";
                    dp.Scale = new(10, 60);
                    dp.Owner = sid;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.Delay = P5_3_MF < 3 ? 0 : 9000;
                    dp.DestoryAt = P5_3_MF < 3? 13000 : 4000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = $"P5_三运_女十字1{P5_3_MF}";
                    dp.Scale = new(10, 60);
                    dp.Rotation = float.Pi / 2;
                    dp.Owner = sid;
                    dp.Color = accessory.Data.DefaultDangerColor;
                    dp.Delay = P5_3_MF < 3 ? 0 : 9000;
                    dp.DestoryAt = P5_3_MF < 3? 13000 : 4000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
                }
                if (transformationID == 4)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = $"P5_三运_女辣翅1{P5_3_MF}";
                    dp.Scale = new(60, 20);
                    dp.Owner = sid;
                    dp.Rotation = float.Pi / 2;
                    dp.Offset = new(-4, 0, 0);
                    dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                    dp.Delay = P5_3_MF < 3 ? 0 : 9000;
                    dp.DestoryAt = P5_3_MF < 3? 13000 : 4000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = $"P5_三运_女辣翅2{P5_3_MF}";
                    dp.Scale = new(60, 20);
                    dp.Owner = sid;
                    dp.Rotation = float.Pi / -2;
                    dp.Offset = new(4, 0, 0);
                    dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                    dp.Delay = P5_3_MF < 3 ? 0 : 9000;
                    dp.DestoryAt = P5_3_MF < 3? 13000 : 4000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
                }
            }
        }
        
        [ScriptMethod(name: "P5_三运_男女位置技能处理", eventType: EventTypeEnum.PlayActionTimeline, eventCondition: ["Id:7747", "SourceDataId:regex:^(15721|15722)$"],userControl: false)]
        public void P5_三运_男女位置技能处理(Event @event, ScriptAccessory accessory)
        {
            if(parse != 5.3) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var transformationID = GetTransformationID(sid, accessory);
            if (transformationID == null) return;
            P5_3_MFT++;
			accessory.Log.Debug($"P5_3_MFT:{P5_3_MFT}");
            if (@event["SourceDataId"] == "15721")
            {
                //男
                if (@event.SourcePosition.X < 100 && @event.SourcePosition.Z < 100)
                {
                    MFPositions[P5_3_MFT <= 2 ? 0 : 2] = 0;
                }else if (@event.SourcePosition.X < 100 && @event.SourcePosition.Z > 100)
                {
                    MFPositions[P5_3_MFT <= 2 ? 0 : 2] = 1;
                }else if (@event.SourcePosition.X > 100 && @event.SourcePosition.Z > 100)
                {
                    MFPositions[P5_3_MFT <= 2 ? 0 : 2] = 2;
                }else if (@event.SourcePosition.X > 100 && @event.SourcePosition.Z < 100)
                {
                    MFPositions[P5_3_MFT <= 2 ? 0 : 2] = 3;
                }
                
                if (transformationID == 0)
                {
                    MFTransformStates[P5_3_MFT <= 2 ? 0 : 2] = 0;
                }
                if (transformationID == 4)
                {
                    MFTransformStates[P5_3_MFT <= 2 ? 0 : 2] = 1;
                }
            }
            if (@event["SourceDataId"] == "15722")
            {
                if (@event.SourcePosition.X < 100 && @event.SourcePosition.Z < 100)
                {
                    MFPositions[P5_3_MFT <= 2 ? 1 : 3] = 0;
                }else if (@event.SourcePosition.X < 100 && @event.SourcePosition.Z > 100)
                {
                    MFPositions[P5_3_MFT <= 2 ? 1 : 3] = 1;
                }else if (@event.SourcePosition.X > 100 && @event.SourcePosition.Z > 100)
                {
                    MFPositions[P5_3_MFT <= 2 ? 1 : 3] = 2;
                }else if (@event.SourcePosition.X > 100 && @event.SourcePosition.Z < 100)
                {
                    MFPositions[P5_3_MFT <= 2 ? 1 : 3] = 3;
                }
                
                if (transformationID == 0)
                {
                    MFTransformStates[P5_3_MFT <= 2 ? 1 : 3] = 0;
                }
                if (transformationID == 4)
                {
                    MFTransformStates[P5_3_MFT <= 2 ? 1 : 3] = 1;
                }
            }
        }
        
        [ScriptMethod(name: "P5_三运_前半指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(31643|31644)$"], userControl: true)]
        public void P5_三运_前半指路(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.3) return;
            int type = JsonConvert.DeserializeObject<int>(@event["ActionId"]) == 31643 ? 0 : 1;
            int type2 = JsonConvert.DeserializeObject<int>(@event["ActionId"]) == 31643 ? 1 : 0;
            Combo1 = MFTransformStates[0] + 2 * MFTransformStates[1];
            Combo2 = MFTransformStates[2] + 2 * MFTransformStates[3];
            accessory.Log.Debug($"Combo1: {Combo1}, Combo2: {Combo2}");
            accessory.Log.Debug($"男1:{MFPositions[0]}  女1:{MFPositions[1]}  男2:{MFPositions[2]}  女2:{MFPositions[3]}");
            accessory.Log.Debug($"type:{type},type2:{type2}");
            //前后为0，左右为1
            var P53Tuple1 = (Combo1, MFPositions[0], MFPositions[1], type);
            var P53Tuple2 = (Combo2, MFPositions[2], MFPositions[3], type2);
            Vector3 dealpos1 = P53SafePos.TryGetValue(P53Tuple1, out var safePos) ? safePos : default;
            Vector3 dealpos2 = P53SafePos.TryGetValue(P53Tuple2, out var safePos1) ? safePos1 : default;;
            
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_前半指路_1";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos1;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 9000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_前半指路_1-2";
            dp.Scale = new(2);
            dp.Position = dealpos1;
            dp.TargetPosition = dealpos2;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 9000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_前半指路_2";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos2;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.Delay = 9000;
            dp.DestoryAt = 4000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

        }
        
        [ScriptMethod(name: "P5_三运_探测波动炮", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(31638|31639)$"], userControl: true)]
        public void P5_三运_探测波动炮(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.3) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            int dir = @event["ActionId"] == "31638" ? 1 : -1;
            
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_探测波动炮";
            dp.Scale = new(20);
            dp.Radian = float.Pi;
            dp.Owner = sid;
            dp.Rotation = float.Pi + float.Pi / 2 * dir;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 10000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
        }
        
        [ScriptMethod(name: "P5_三运_三传指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(31638|31639)$"], userControl: true)]
        public void P5_三运_三传(Event @event, ScriptAccessory accessory)
        {
            Task.Delay(1500).Wait();
            if (parse != 5.3) return;
            int dir = @event["ActionId"] == "31638" ? 2 : -2;

            var dealpos = P52_MarkType switch
            {
                1 => RotatePoint(new Vector3(81f, 0f, 101f), new Vector3(100, 0, 100), float.Pi / 4 * dir),
                2 => RotatePoint(new Vector3(119f, 0f, 101f), new Vector3(100, 0, 100), float.Pi / 4 * dir),
                3 => RotatePoint(new Vector3(102f, 0f, 111f), new Vector3(100, 0, 100), float.Pi / 4 * dir),
                4 => RotatePoint(new Vector3(105.26f, 0f, 118.26f), new Vector3(100, 0, 100), float.Pi / 4 * dir),
                5 => RotatePoint(new Vector3(90f, 0f, 101f), new Vector3(100, 0, 100), float.Pi / 4 * dir),
                6 => RotatePoint(new Vector3(94.74f, 0f, 118.26f), new Vector3(100, 0, 100), float.Pi / 4 * dir),
                7 => RotatePoint(new Vector3(90.8f, 0f, 90.8f), new Vector3(100, 0, 100), float.Pi / 4 * dir),
                8 => RotatePoint(new Vector3(109.2f, 0f, 90.8f), new Vector3(100, 0, 100), float.Pi / 4 * dir)
            };
            
            accessory.Log.Debug($"绘制三传指路 向 {dealpos}");
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_三传";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 8500;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            parse = 5.4;
        }
        
        [ScriptMethod(name: "P5_三运_四传指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:32374"], userControl: true)]
        public void P5_三运_四传(Event @event, ScriptAccessory accessory)
        {
            Task.Delay(2500).Wait();
            if (parse != 5.4) return;
            var dealpos = P52_MarkType switch
            {
                1 => RotatePoint(new Vector3(81f, 0f, 101f), new Vector3(100, 0, 100), float.Pi / 4 * P53_4_HW),
                2 => RotatePoint(new Vector3(119f, 0f, 101f), new Vector3(100, 0, 100), float.Pi / 4 * P53_4_HW),
                3 => RotatePoint(new Vector3(102f, 0f, 111f), new Vector3(100, 0, 100), float.Pi / 4 * P53_4_HW),
                4 => RotatePoint(new Vector3(105.26f, 0f, 118.26f), new Vector3(100, 0, 100), float.Pi / 4 * P53_4_HW),
                5 => RotatePoint(new Vector3(90f, 0f, 101f), new Vector3(100, 0, 100), float.Pi / 4 * P53_4_HW),
                6 => RotatePoint(new Vector3(94.74f, 0f, 118.26f), new Vector3(100, 0, 100), float.Pi / 4 * P53_4_HW),
                7 => RotatePoint(new Vector3(89.7f, 0f, 83.5f), new Vector3(100, 0, 100), float.Pi / 4 * P53_4_HW),
                8 => RotatePoint(new Vector3(110.3f, 0f, 83.5f), new Vector3(100, 0, 100), float.Pi / 4 * P53_4_HW)
            };
            accessory.Log.Debug($"绘制四传指路 向 {dealpos}");
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_四传";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 8500;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        
        [ScriptMethod(name: "P5_三运_四传方向获取", eventType: EventTypeEnum.AddCombatant, eventCondition: ["DataId:15724"], userControl: false)]
        public void P5_三运_四传方向获取(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.3) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            P53_4_HW = RoundPositionTo8Dir(pos, new(100, 0, 100));
            accessory.Log.Debug($"四传基准方位为 {P53_4_HW}");
        }
        #endregion

        #region P6
        [ScriptMethod(name: "P6转场记录", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31649"], userControl: false)]
        public void P6转场记录(Event @event, ScriptAccessory accessory)
        {
            parse = 6;
        }
	    
        [ScriptMethod(name: "宇宙天箭计数", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31650"], userControl: false)]
        public void 宇宙天箭计数(Event @event, ScriptAccessory accessory)
        {
            ArrowNum++;
            isSet = false;
		    ArrowModeConfirmed = new System.Threading.AutoResetEvent(false);
        }

        [ScriptMethod(name: "宇宙天箭", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31651"], userControl: false)]
        public void 宇宙天箭(Event @event, ScriptAccessory accessory)
        {
            var casterPos = @event.SourcePosition();
            var center = MapCenter;
            var offset = casterPos - center;
            // 模式判断
            if (Math.Abs(offset.X) < 5) // 中央垂直线
            {
                GenerateLine(accessory, casterPos, new Vector2(1, 0), 4, InOut);
                GenerateLine(accessory, casterPos, new Vector2(-1, 0), 4, InOut);
                if (!isSet)
                {
                    arrowMode = 0;
                    isSet = true;
				    System.Threading.Thread.MemoryBarrier();
				    ArrowModeConfirmed.Set();
                }
            }
            else if (Math.Abs(offset.Z) < 5) // 中央水平线
            {
                GenerateLine(accessory, casterPos, new Vector2(0, 1), 4, InOut);
                GenerateLine(accessory, casterPos, new Vector2(0, -1), 4, InOut);
                if (!isSet)
                {
                    arrowMode = 0;
                    isSet = true;
				    System.Threading.Thread.MemoryBarrier();
				    ArrowModeConfirmed.Set();
                }
            }
            else if (Math.Abs(offset.X) < 18) // 侧边垂直线
            {
                GenerateLine(accessory, casterPos, new Vector2(offset.X < 0 ? 1 : -1, 0), 7, OutIn);
                if (!isSet)
                {
                    arrowMode = 1;
                    isSet = true;
				    System.Threading.Thread.MemoryBarrier();
				    ArrowModeConfirmed.Set();
                }
            }
            else if (Math.Abs(offset.Z) < 18) // 侧边水平线
            {
                GenerateLine(accessory, casterPos, new Vector2(0, offset.Z < 0 ? 1 : -1), 7, OutIn);
                if (!isSet)
                {
                    arrowMode = 1;
                    isSet = true;
				    System.Threading.Thread.MemoryBarrier();
				    ArrowModeConfirmed.Set();
                }
            }
        }

        private void GenerateLine(ScriptAccessory accessory, Vector3 origin, Vector2 direction, int steps, string pos)
        {
            for (int i = 0; i < steps; i++)
            {
                if (i == 0)
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = $"天箭线段_{direction}_{i}_{pos}";
                    dp.Color = ArrorColor.V4;
                    dp.Position = origin;
                    dp.Scale = new Vector2(10, 40); // 宽度10，长度100
                    dp.Rotation = -MathF.Atan2(direction.Y, direction.X);
                    dp.Delay = 0;
                    dp.DestoryAt = 8000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
                }
                else
                {
                    var dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = $"天箭线段_{direction}_{i}_{pos}";
                    dp.Color = ArrorColor.V4;
                    dp.Position = origin + new Vector3(direction.X * 5f * i + direction.X * 2.5f , 0, direction.Y * 5f * i + direction.Y * 2.5f);
                    dp.Scale = new Vector2(5, 100); // 宽度5，长度100
                    dp.Rotation = MathF.Atan2(direction.Y, direction.X);
                    dp.Delay = 6000 + i*2000;
                    dp.DestoryAt = 2000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
                }
            }
        }

        [ScriptMethod(name: "宇宙天箭指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31651"], suppress: 3000)]
        public async void 宇宙天箭指路(Event @event, ScriptAccessory accessory)
        {
            System.Threading.Thread.MemoryBarrier();
		    
		    ArrowModeConfirmed.WaitOne();
		    System.Threading.Thread.MemoryBarrier();

		    var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
		    if (followCrowd)
		    {
			    myindex = ArrowNum < 1 ? 4 : accessory.Data.PartyList.IndexOf(accessory.Data.Me);
		    }
		    else
		    {
			    myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
		    }
            int offset1 = arrowMode < 1 ? 13 : 17;
            int offset2 = arrowMode < 1 ? 7: 23;
            int offset3 = arrowMode < 1 ? 23 : 17;
            var offset4 = 12.5f;
            var offset5 = 7.5f;
            var delayMode = arrowMode < 1 ? 4000 : 2000;
            var delayModeTN = arrowMode < 1 && myindex < 4 ? 2000 : 0;
            // accessory.Method.TextInfo($"{arrowMode}_{delayMode}_{delayModeTN}", duration: 5000, true);
            
            Vector3 dealpos0 = default; // 起跑点
            
            Vector3 dealpos1 = default; // 第一次穿入
            Vector3 dealpos2 = default; // 第二次穿入
            Vector3 dealpos3 = default; // 第三次穿入
            Vector3 dealpos4 = default; // 第四次穿入
            Vector3 coordinate0 = arrowMode < 1 ? new Vector3(93.5f, 0, 93.5f) : new(91.5f, 0, 91.5f);
            Vector3 coordinate1 = arrowMode < 1 ? new Vector3(96.5f, 0, 96.5f) : new(88.5f, 0, 88.5f);
            Vector3 coordinate2 = arrowMode < 1 ? new Vector3(88.5f, 0, 88.5f) : new(91.5f, 0, 91.5f);
            Vector3 coordinate3 = new Vector3(100.0f, 0, 87.5f);
            Vector3 coordinate4 = new Vector3(100.0f, 0, 92.5f);
            dealpos0 = myindex switch
            {
                // MT和D3（基准坐标）
                0 or 6 => coordinate0,
                
                // ST和D4（X轴+offset1）
                1 or 7 => new Vector3(coordinate0.X + offset1, 0, coordinate0.Z),
                
                // H1和D1（Z轴+offset1）
                2 or 4 => new Vector3(coordinate0.X, 0, coordinate0.Z + offset1),
                
                // H2和D2（X/Z轴都+offset1）
                3 or 5 => new Vector3(coordinate0.X + offset1, 0, coordinate0.Z + offset1),
                    
                _ => default
            };
            dealpos1 = myindex switch
            {
                // MT和D3（基准坐标）
                0 or 6 => coordinate1,
                
                // ST和D4
                1 or 7 => new Vector3(coordinate1.X + offset2, 0, coordinate1.Z),
                
                // H1和D1
                2 or 4 => new Vector3(coordinate1.X, 0, coordinate1.Z + offset2),
                
                // H2和D2
                3 or 5 => new Vector3(coordinate1.X + offset2, 0, coordinate1.Z + offset2),
                    
                _ => default
            };
            dealpos2 = myindex switch
            {
                0 => arrowMode < 1 ? new Vector3(94.0f, 0, 90.0f) : coordinate2,
                1 => arrowMode < 1 ? new Vector3(110.0f, 0, 94.0f) : new Vector3(coordinate2.X + offset3, 0, coordinate2.Z),
                2 => arrowMode < 1 ? new Vector3(90.0f, 0, 106.0f) : new Vector3(coordinate2.X, 0, coordinate2.Z + offset3),
                3 => arrowMode < 1 ? new Vector3(106f, 0, 110.0f) : new Vector3(coordinate2.X + offset3, 0, coordinate2.Z + offset3),
                6 => coordinate2,
                7 => new Vector3(coordinate2.X + offset3, 0, coordinate2.Z),
                4 => new Vector3(coordinate2.X, 0, coordinate2.Z + offset3),
                5 => new Vector3(coordinate2.X + offset3, 0, coordinate2.Z + offset3),
                _ => default
            };
            dealpos3 = myindex switch
            {
                0 => coordinate3,
                1 => new Vector3(coordinate3.X + offset4, 0, coordinate3.Z + offset4),
                2 => new Vector3(coordinate3.X - offset4, 0, coordinate3.Z + offset4),
                3 => new Vector3(coordinate3.X, 0, coordinate2.Z + (offset4 * 2)),
                4 => arrowMode < 1 ? new Vector3(88.5f, 0, 111.5f) : new Vector3(coordinate1.X, 0, coordinate1.Z + offset2),
                5 => arrowMode < 1 ? new Vector3(111.5f, 0, 111.5f) : new Vector3(coordinate1.X + offset2, 0, coordinate1.Z + offset2),
                6 => arrowMode < 1 ? new Vector3(88.5f, 0, 88.5f) : coordinate1,
                7 => arrowMode < 1 ? new Vector3(111.5f, 0, 88.5f) : new Vector3(coordinate1.X + offset2, 0, coordinate1.Z),
                _ => default
            };
            dealpos4 = myindex switch
            {
                0 => coordinate4,
                1 => new Vector3(coordinate4.X + offset5, 0, coordinate4.Z + offset5),
                2 => new Vector3(coordinate4.X - offset5, 0, coordinate4.Z + offset5),
                3 => new Vector3(coordinate4.X, 0, coordinate4.Z + (offset5 * 2)),
                4 => new Vector3(91.5f, 0, 108.5f),
                5 => new Vector3(108.5f, 0, 108.5f),
                6 => new Vector3(91.5f, 0, 91.5f),
                7 => new Vector3(108.5f, 0, 91.5f),
                _ => default
            };
            if (dealpos0 != default)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P6_射手天箭_预站位";
                dp.Scale = new(ArrowScale);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos0;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 7500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            if (dealpos0 != default && dealpos1 != default)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P6_射手天箭_1-2";
                dp.Scale = new(ArrowScale);
                dp.Position = dealpos0;
                dp.TargetPosition = dealpos1;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 7500;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P6_射手天箭_2";
                dp.Scale = new(ArrowScale);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos1;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 7500;
                dp.DestoryAt = 4000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            if (dealpos1 != default && dealpos2 != default)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P6_射手天箭_2-3";
                dp.Scale = new(ArrowScale);
                dp.Position = dealpos1;
                dp.TargetPosition = dealpos2;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.Delay = 7500;
                dp.DestoryAt = 4000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P6_射手天箭_3";
                dp.Scale = new(ArrowScale);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos2;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 11500;
                dp.DestoryAt = 2000 + delayModeTN;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }

            if (dealpos2 != default && dealpos3 != default)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P6_射手天箭_3-4";
                dp.Scale = new(ArrowScale);
                dp.Position = dealpos2;
                dp.TargetPosition = dealpos3;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.Delay = 11500;
                dp.DestoryAt = 2000 + delayModeTN;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P6_射手天箭_4";
                dp.Scale = new(ArrowScale);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos3;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 13500 + delayModeTN;
                dp.DestoryAt = delayMode - delayModeTN;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
            if (dealpos2 != default && dealpos3 != default)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P6_射手天箭_4-5";
                dp.Scale = new(ArrowScale);
                dp.Position = dealpos3;
                dp.TargetPosition = dealpos4;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.Delay = 13500;
                dp.DestoryAt = delayMode;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
                

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P6_射手天箭_5";
                dp.Scale = new(ArrowScale);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = dealpos4;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.Delay = 13500 + delayMode;
                dp.DestoryAt = 2000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }

        }
        
        [ScriptMethod(name: "地火8方指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31663"], userControl: true)]
        public void 地火计数(Event @event, ScriptAccessory accessory)
        {
            CannonNum++;
            if (CannonNum == 48)
            {
                var myindex = accessory.Data.PartyList.IndexOf(accessory.Data.Me);
                var pos = myindex switch
                {
                    0 => new Vector3(99.97f, -0.00f, 86.97f),
                    1 => new Vector3(113.15f, -0.00f, 100.07f),
                    2 => new Vector3(86.91f, 0.00f, 100.03f),
                    3 => new Vector3(100.04f, -0.00f, 112.89f),
                    4 => new Vector3(90.66f, -0.00f, 109.15f),
                    5 => new Vector3(109.40f, -0.00f, 109.29f),
                    6 => new Vector3(90.67f, 0.00f, 90.78f),
                    7 => new Vector3(109.47f, 0.00f, 90.83f),
                    _ => default
                };
                
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P6一地火8方";
                dp.Scale = new(2);
                dp.Owner = accessory.Data.Me;
                dp.TargetPosition = pos;
                dp.ScaleMode |= ScaleMode.YByDistance;
                dp.Color = accessory.Data.DefaultSafeColor;
                dp.DestoryAt = 8000;
                accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
            }
        }
        
        [ScriptMethod(name: "步进式地火计数", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31661"], userControl: false)]
        public void 步进式地火计数(Event @event, ScriptAccessory accessory)
        {
            var pos = @event.SourcePosition();
            StepCannon[(StepCannonIndex++)%4] = pos;
        }
        
        [ScriptMethod(name: "步进式地火指路", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31660"], userControl: true)]
        public void 步进式地火指路(Event @event, ScriptAccessory accessory)
        {
            
            var c1 = StepCannon[0];
            var c2 = StepCannon[1];
            float a = (MathF.Atan2(c1.X - 100, c1.Z - 100) - MathF.Atan2(c2.X - 100, c2.Z - 100)) / float.Pi * 180;
            if (a>180) a=a-360;
            if (a<-180) a=a+360;
            var c1e = new Vector3((c1.X - 100) / 24 * 18 + 100, 0, (c1.Z - 100) / 24 * 18 + 100);
            
            var end = RotatePointFromCentre(c1e, MapCenter, a*-1.5f);
            
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P6地火起跑位置";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = end;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 9000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }

        private static Vector3 RotatePointFromCentre(Vector3 point, Vector3 centre, float angleDegrees)
        {
            float dx = point.X - centre.X;
            float dz = point.Z - centre.Z;
            float thetaRad = MathF.Atan2(dx, dz);
            float normalizedAngle = (1f - (thetaRad / MathF.PI)) % 2f;
            if (normalizedAngle < 0) normalizedAngle += 2f;
            float baseRotation = normalizedAngle * 180f;
            float totalRotation = (baseRotation + angleDegrees) * MathF.PI / 180f;
            float distance = MathF.Sqrt(dx * dx + dz * dz);
            return new Vector3(
                centre.X + MathF.Sin(totalRotation) * distance,
                0f, 
                centre.Z - MathF.Cos(totalRotation) * distance 
            );
        }
	    
        [ScriptMethod(name: "陨石核爆点名", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:015A"], userControl: true)]
        public void 陨石核爆点名(Event @event, ScriptAccessory accessory)
        {
	        if (parse != 6 || @event.TargetId() == 0) return;
	        var tid = @event.TargetId();
	        var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P6陨石核爆点名";
            dp.Scale = new(20);
            dp.Owner = tid;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 8000;
            accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        }    
            

        #endregion
        
        #region 类函数

        public class PriorityDict
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed
            public ScriptAccessory sa { get; set; } = null!;

            // ReSharper disable once NullableWarningSuppressionIsUsed
            public Dictionary<int, int> Priorities { get; set; } = null!;
            public string Annotation { get; set; } = "";
            public int ActionCount { get; set; } = 0;

            public void Init(ScriptAccessory accessory, string annotation, int partyNum = 8)
            {
                sa = accessory;
                Priorities = new Dictionary<int, int>();
                for (var i = 0; i < partyNum; i++)
                {
                    Priorities.Add(i, 0);
                }

                Annotation = annotation;
                ActionCount = 0;
            }

            /// <summary>
            /// 为特定Key增加优先级
            /// </summary>
            /// <param name="idx">key</param>
            /// <param name="priority">优先级数值</param>
            public void AddPriority(int idx, int priority)
            {
                Priorities[idx] += priority;
            }

            /// <summary>
            /// 从Priorities中找到前num个数值最小的，得到新的Dict返回
            /// </summary>
            /// <param name="num"></param>
            /// <returns></returns>
            public List<KeyValuePair<int, int>> SelectSmallPriorityIndices(int num)
            {
                return SelectMiddlePriorityIndices(0, num);
            }

            /// <summary>
            /// 从Priorities中找到前num个数值最大的，得到新的Dict返回
            /// </summary>
            /// <param name="num"></param>
            /// <returns></returns>
            public List<KeyValuePair<int, int>> SelectLargePriorityIndices(int num)
            {
                return SelectMiddlePriorityIndices(0, num, true);
            }

            /// <summary>
            /// 从Priorities中找到升序排列中间的数值，得到新的Dict返回
            /// </summary>
            /// <param name="skip">跳过skip个元素。若从第二个开始取，skip=1</param>
            /// <param name="num"></param>
            /// <param name="descending">降序排列，默认为false</param>
            /// <returns></returns>
            public List<KeyValuePair<int, int>> SelectMiddlePriorityIndices(int skip, int num, bool descending = false)
            {
                if (Priorities.Count < skip + num)
                    return new List<KeyValuePair<int, int>>();

                IEnumerable<KeyValuePair<int, int>> sortedPriorities;
                if (descending)
                {
                    // 根据值从大到小降序排序，并取前num个键
                    sortedPriorities = Priorities
                        .OrderByDescending(pair => pair.Value) // 先根据值排列
                        .ThenBy(pair => pair.Key) // 再根据键排列
                        .Skip(skip) // 跳过前skip个元素
                        .Take(num); // 取前num个键值对
                }
                else
                {
                    // 根据值从小到大升序排序，并取前num个键
                    sortedPriorities = Priorities
                        .OrderBy(pair => pair.Value) // 先根据值排列
                        .ThenBy(pair => pair.Key) // 再根据键排列
                        .Skip(skip) // 跳过前skip个元素
                        .Take(num); // 取前num个键值对
                }

                return sortedPriorities.ToList();
            }

            /// <summary>
            /// 从Priorities中找到升序排列第idx位的数据，得到新的Dict返回
            /// </summary>
            /// <param name="idx"></param>
            /// <param name="descending">降序排列，默认为false</param>
            /// <returns></returns>
            public KeyValuePair<int, int> SelectSpecificPriorityIndex(int idx, bool descending = false)
            {
                var sortedPriorities = SelectMiddlePriorityIndices(0, Priorities.Count, descending);
                return sortedPriorities[idx];
            }

            /// <summary>
            /// 从Priorities中找到对应key的数据，得到其Value排序后位置返回
            /// </summary>
            /// <param name="key"></param>
            /// <param name="descending">降序排列，默认为false</param>
            /// <returns></returns>
            public int FindPriorityIndexOfKey(int key, bool descending = false)
            {
                var sortedPriorities = SelectMiddlePriorityIndices(0, Priorities.Count, descending);
                var i = 0;
                foreach (var dict in sortedPriorities)
                {
                    if (dict.Key == key) return i;
                    i++;
                }

                return i;
            }

            /// <summary>
            /// 一次性增加优先级数值
            /// 通常适用于特殊优先级（如H-T-D-H）
            /// </summary>
            /// <param name="priorities"></param>
            public void AddPriorities(List<int> priorities)
            {
                if (Priorities.Count != priorities.Count)
                    throw new ArgumentException("输入的列表与内部设置长度不同");

                for (var i = 0; i < Priorities.Count; i++)
                    AddPriority(i, priorities[i]);
            }

            /// <summary>
            /// 输出优先级字典的Key与优先级
            /// </summary>
            /// <returns></returns>
            public string ShowPriorities(bool showJob = true)
            {
                var str = $"{Annotation} ({ActionCount}-th) 优先级字典：\n";
                if (Priorities.Count == 0)
                {
                    str += $"PriorityDict Empty.\n";
                    return str;
                }

                foreach (var pair in Priorities)
                {
                    str += $"Key {pair.Key} {(showJob ? $"({_role[pair.Key]})" : "")}, Value {pair.Value}\n";
                }

                return str;
            }

            public void AddActionCount(int count = 1)
            {
                ActionCount += count;
            }

        }
        
        #endregion
        private static bool ParseObjectId(string? idStr, out uint id)
        {
            id = 0;
            if (string.IsNullOrEmpty(idStr)) return false;
            try
            {
                var idStr2 = idStr.Replace("0x", "");
                id = uint.Parse(idStr2, System.Globalization.NumberStyles.HexNumber);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private int RoundPositionTo4Dir(Vector3 point, Vector3 centre)
        {
            var r = Math.Round(2 - 2 * Math.Atan2(point.X - centre.X, point.Z - centre.Z) / Math.PI) % 4;
            return (int)r;
        }
        private int RoundPositionTo8Dir(Vector3 point, Vector3 centre)
        {
            // Dirs: N = 0, NE = 1, ..., NW = 7
            var r = Math.Round(4 - 4 * Math.Atan2(point.X - centre.X, point.Z - centre.Z) / Math.PI) % 8;
            return (int)r;

        }
        
        private int RoundPositionTo16Dir(Vector3 point, Vector3 centre)
        {
            var r = Math.Round(8 - 8 * Math.Atan2(point.X - centre.X, point.Z - centre.Z) / Math.PI) % 16;
            return (int)r;
        }
        
        private int FloorPositionTo4Dir(Vector3 point, Vector3 centre)
        {
            var r = Math.Floor(2 - 2 * Math.Atan2(point.X - centre.X, point.Z - centre.Z) / Math.PI) % 4;
            return (int)r;
        }
        private int FloorPositionTo8Dir(Vector3 point, Vector3 centre)
        {
            // Dirs: N = 0, NE = 1, ..., NW = 7
            var r = Math.Floor(4 - 4 * Math.Atan2(point.X - centre.X, point.Z - centre.Z) / Math.PI) % 8;
            return (int)r;

        }
        private Vector3 RotatePoint(Vector3 point, Vector3 centre, float radian)
        {

            Vector2 v2 = new(point.X - centre.X, point.Z - centre.Z);

            var rot = (MathF.PI - MathF.Atan2(v2.X, v2.Y) + radian);
            var lenth = v2.Length();
            return new(centre.X + MathF.Sin(rot) * lenth, centre.Y, centre.Z - MathF.Cos(rot) * lenth);
        }
        
        private byte? GetTransformationID(uint _id, ScriptAccessory accessory)
        {
            var obj = accessory.Data.Objects.SearchById(_id);
            if (obj != null)
            {
                unsafe
                {
                    FFXIVClientStructs.FFXIV.Client.Game.Character.Character* objStruct = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)obj.Address;
                    return objStruct->Timeline.ModelState;
                }
            }
            return null;
        }
        
        public static void SetRotation(ScriptAccessory sa, IGameObject? obj, float rotation)
        {
            if (obj == null || !obj.IsValid())
            {
                sa.Log.Error($"传入的IGameObject不合法。");
                return;
            }
            unsafe
            {
                GameObject* charaStruct = (GameObject*)obj.Address;
                charaStruct->SetRotation(rotation);
            }
            sa.Log.Debug($"SetRotation => {obj.Name.TextValue} | {obj} => {rotation}");
        }
    }
}

public static class EventExtensions
{
    private static bool ParseHexId(string? idStr, out uint id)
    {
        id = 0;
        if (string.IsNullOrEmpty(idStr)) return false;
        try
        {
            var idStr2 = idStr.Replace("0x", "");
            id = uint.Parse(idStr2, System.Globalization.NumberStyles.HexNumber);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static uint Id0(this Event @event)
    {
        return ParseHexId(@event["Id"], out var id) ? id : 0;
    }
    
    public static uint ActionId(this Event @event)
    {
        return JsonConvert.DeserializeObject<uint>(@event["ActionId"]);
    }

    public static uint SourceId(this Event @event)
    {
        return ParseHexId(@event["SourceId"], out var id) ? id : 0;
    }


    public static string DurationMilliseconds(this Event @event)
    {
        return JsonConvert.DeserializeObject<string>(@event["DurationMilliseconds"]) ?? string.Empty;
    }

    public static float SourceRotation(this Event @event)
    {
        return float.TryParse(@event["SourceRotation"], out var rot) ? rot : 0;
    }

    public static byte Index(this Event @event)
    {
        return (byte)(ParseHexId(@event["Index"], out var index) ? index : 0);
    }

    public static uint State(this Event @event)
    {
        return ParseHexId(@event["State"], out var state) ? state : 0;
    }

    public static string SourceName(this Event @event)
    {
        return JsonConvert.DeserializeObject<string>(@event["SourceName"]) ?? string.Empty;
    }

    public static uint TargetId(this Event @event)
    {
        return ParseHexId(@event["TargetId"], out var id) ? id : 0;
    }

    public static Vector3 SourcePosition(this Event @event)
    {
        return JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
    }

    public static Vector3 TargetPosition(this Event @event)
    {
        return JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]);
    }

    public static Vector3 EffectPosition(this Event @event)
    {
        return JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]);
    }
}


#region 计算函数
public static class DirectionCalc
{
    public static float DegToRad(this float deg) => (deg + 360f) % 360f / 180f * float.Pi;
    public static float RadToDeg(this float rad) => (rad + 2 * float.Pi) % (2 * float.Pi) / float.Pi * 180f;
    
    // 以北为0建立list
    // Game         List    Logic
    // 0            - 4     pi
    // 0.25 pi      - 3     0.75pi
    // 0.5 pi       - 2     0.5pi
    // 0.75 pi      - 1     0.25pi
    // pi           - 0     0
    // 1.25 pi      - 7     1.75pi
    // 1.5 pi       - 6     1.5pi
    // 1.75 pi      - 5     1.25pi
    // Logic = Pi - Game (+ 2pi)

    /// <summary>
    /// 将游戏基角度（以南为0，逆时针增加）转为逻辑基角度（以北为0，顺时针增加）
    /// 算法与Logic2Game完全相同，但为了代码可读性，便于区分。
    /// </summary>
    /// <param name="radian">游戏基角度</param>
    /// <returns>逻辑基角度</returns>
    public static float Game2Logic(this float radian)
    {
        // if (r < 0) r = (float)(r + 2 * Math.PI);
        // if (r > 2 * Math.PI) r = (float)(r - 2 * Math.PI);

        var r = float.Pi - radian;
        r = (r + float.Pi * 2) % (float.Pi * 2);
        return r;
    }

    /// <summary>
    /// 将逻辑基角度（以北为0，顺时针增加）转为游戏基角度（以南为0，逆时针增加）
    /// 算法与Game2Logic完全相同，但为了代码可读性，便于区分。
    /// </summary>
    /// <param name="radian">逻辑基角度</param>
    /// <returns>游戏基角度</returns>
    public static float Logic2Game(this float radian)
    {
        // var r = (float)Math.PI - radian;
        // if (r < Math.PI) r = (float)(r + 2 * Math.PI);
        // if (r > Math.PI) r = (float)(r - 2 * Math.PI);

        return radian.Game2Logic();
    }

    /// <summary>
    /// 适用于旋转，FF14游戏基顺时针旋转为负。
    /// </summary>
    /// <param name="radian"></param>
    /// <returns></returns>
    public static float Cw2Ccw(this float radian)
    {
        return -radian;
    }

    /// <summary>
    /// 适用于旋转，FF14游戏基顺时针旋转为负。
    /// 与Cw2CCw完全相同，为了代码可读性便于区分。
    /// </summary>
    /// <param name="radian"></param>
    /// <returns></returns>
    public static float Ccw2Cw(this float radian)
    {
        return -radian;
    }

    /// <summary>
    /// 输入逻辑基角度，获取逻辑方位（斜分割以正上为0，正分割以右上为0，顺时针增加）
    /// </summary>
    /// <param name="radian">逻辑基角度</param>
    /// <param name="dirs">方位总数</param>
    /// <param name="diagDivision">斜分割，默认true</param>
    /// <returns>逻辑基角度对应的逻辑方位</returns>
    public static int Rad2Dirs(this float radian, int dirs, bool diagDivision = true)
    {
        var r = diagDivision
            ? Math.Round(radian / (2f * float.Pi / dirs))
            : Math.Floor(radian / (2f * float.Pi / dirs));
        r = (r + dirs) % dirs;
        return (int)r;
    }

    /// <summary>
    /// 输入坐标，获取逻辑方位（斜分割以正上为0，正分割以右上为0，顺时针增加）
    /// </summary>
    /// <param name="point">坐标点</param>
    /// <param name="center">中心点</param>
    /// <param name="dirs">方位总数</param>
    /// <param name="diagDivision">斜分割，默认true</param>
    /// <returns>该坐标点对应的逻辑方位</returns>
    public static int Position2Dirs(this Vector3 point, Vector3 center, int dirs, bool diagDivision = true)
    {
        double dirsDouble = dirs;
        var r = diagDivision
            ? Math.Round(dirsDouble / 2 - dirsDouble / 2 * Math.Atan2(point.X - center.X, point.Z - center.Z) / Math.PI) % dirsDouble
            : Math.Floor(dirsDouble / 2 - dirsDouble / 2 * Math.Atan2(point.X - center.X, point.Z - center.Z) / Math.PI) % dirsDouble;
        return (int)r;
    }

    /// <summary>
    /// 以逻辑基弧度旋转某点
    /// </summary>
    /// <param name="point">待旋转点坐标</param>
    /// <param name="center">中心</param>
    /// <param name="radian">旋转弧度</param>
    /// <returns>旋转后坐标点</returns>
    public static Vector3 RotatePoint(this Vector3 point, Vector3 center, float radian)
    {
        // 围绕某点顺时针旋转某弧度
        Vector2 v2 = new(point.X - center.X, point.Z - center.Z);
        var rot = MathF.PI - MathF.Atan2(v2.X, v2.Y) + radian;
        var length = v2.Length();
        return new Vector3(center.X + MathF.Sin(rot) * length, center.Y, center.Z - MathF.Cos(rot) * length);
    }

    /// <summary>
    /// 以逻辑基角度从某中心点向外延伸
    /// </summary>
    /// <param name="center">待延伸中心点</param>
    /// <param name="radian">旋转弧度</param>
    /// <param name="length">延伸长度</param>
    /// <returns>延伸后坐标点</returns>
    public static Vector3 ExtendPoint(this Vector3 center, float radian, float length)
    {
        // 令某点以某弧度延伸一定长度
        return new Vector3(center.X + MathF.Sin(radian) * length, center.Y, center.Z - MathF.Cos(radian) * length);
    }

    /// <summary>
    /// 寻找外侧某点到中心的逻辑基弧度
    /// </summary>
    /// <param name="center">中心</param>
    /// <param name="newPoint">外侧点</param>
    /// <returns>外侧点到中心的逻辑基弧度</returns>
    public static float FindRadian(this Vector3 newPoint, Vector3 center)
    {
        var radian = MathF.PI - MathF.Atan2(newPoint.X - center.X, newPoint.Z - center.Z);
        if (radian < 0)
            radian += 2 * MathF.PI;
        return radian;
    }

    /// <summary>
    /// 将输入点左右折叠
    /// </summary>
    /// <param name="point">待折叠点</param>
    /// <param name="centerX">中心折线坐标点</param>
    /// <returns></returns>
    public static Vector3 FoldPointHorizon(this Vector3 point, float centerX)
    {
        return point with { X = 2 * centerX - point.X };
    }

    /// <summary>
    /// 将输入点上下折叠
    /// </summary>
    /// <param name="point">待折叠点</param>
    /// <param name="centerZ">中心折线坐标点</param>
    /// <returns></returns>
    public static Vector3 FoldPointVertical(this Vector3 point, float centerZ)
    {
        return point with { Z = 2 * centerZ - point.Z };
    }

    /// <summary>
    /// 将输入点中心对称
    /// </summary>
    /// <param name="point">输入点</param>
    /// <param name="center">中心点</param>
    /// <returns></returns>
    public static Vector3 PointCenterSymmetry(this Vector3 point, Vector3 center)
    {
        return point.RotatePoint(center, float.Pi);
    }

    /// <summary>
    /// 将输入点朝某中心点往内/外同角度延伸，默认向内
    /// </summary>
    /// <param name="point">待延伸点</param>
    /// <param name="center">中心点</param>
    /// <param name="length">延伸长度</param>
    /// <param name="isOutside">是否向外延伸</param>>
    /// <returns></returns>
    public static Vector3 PointInOutside(this Vector3 point, Vector3 center, float length, bool isOutside = false)
    {
        Vector2 v2 = new(point.X - center.X, point.Z - center.Z);
        var targetPos = (point - center) / v2.Length() * length * (isOutside ? 1 : -1) + point;
        return targetPos;
    }

    /// <summary>
    /// 获得两点之间距离
    /// </summary>
    /// <param name="point"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static float DistanceTo(this Vector3 point, Vector3 target)
    {
        Vector2 v2 = new(point.X - target.X, point.Z - target.Z);
        return v2.Length();
    }

    /// <summary>
    /// 寻找两点之间的角度差，范围0~360deg
    /// </summary>
    /// <param name="basePoint">基准位置</param>
    /// <param name="targetPos">比较目标位置</param>
    /// <param name="center">场地中心</param>
    /// <returns></returns>
    public static float FindRadianDifference(this Vector3 targetPos, Vector3 basePoint, Vector3 center)
    {
        var baseRad = basePoint.FindRadian(center);
        var targetRad = targetPos.FindRadian(center);
        var deltaRad = targetRad - baseRad;
        if (deltaRad < 0)
            deltaRad += float.Pi * 2;
        return deltaRad;
    }

    /// <summary>
    /// 从第三人称视角出发观察某目标是否在另一目标的右侧。
    /// </summary>
    /// <param name="basePoint">基准位置</param>
    /// <param name="targetPos">比较目标位置</param>
    /// <param name="center">场地中心</param>
    /// <returns></returns>
    public static bool IsAtRight(this Vector3 targetPos, Vector3 basePoint, Vector3 center)
    {
        // 从场中看向场外，在右侧
        return targetPos.FindRadianDifference(basePoint, center) < float.Pi;
    }

    /// <summary>
    /// 获取给定数的指定位数
    /// </summary>
    /// <param name="val">给定数值</param>
    /// <param name="x">对应位数，个位为1</param>
    /// <returns></returns>
    public static int GetDecimalDigit(this int val, int x)
    {
        string valStr = val.ToString();
        int length = valStr.Length;

        if (x < 1 || x > length)
        {
            return -1;
        }

        char digitChar = valStr[length - x]; // 从右往左取第x位
        return int.Parse(digitChar.ToString());
    }
}
#endregion 计算函数

#region 位置序列函数
public static class IndexHelper
{
    public static IGameObject? GetById(this ScriptAccessory sa, ulong gameObjectId)
    {
        return sa.Data.Objects.SearchById(gameObjectId);
    }
    
    /// <summary>
    /// 输入玩家dataId，获得对应的位置index
    /// </summary>
    /// <param name="pid">玩家SourceId</param>
    /// <param name="accessory"></param>
    /// <returns>该玩家对应的位置index</returns>
    public static int GetPlayerIdIndex(this ScriptAccessory accessory, uint pid)
    {
        // 获得玩家 IDX
        return accessory.Data.PartyList.IndexOf(pid);
    }

    /// <summary>
    /// 获得主视角玩家对应的位置index
    /// </summary>
    /// <param name="accessory"></param>
    /// <returns>主视角玩家对应的位置index</returns>
    public static int GetMyIndex(this ScriptAccessory accessory)
    {
        return accessory.Data.PartyList.IndexOf(accessory.Data.Me);
    }

    /// <summary>
    /// 输入玩家dataId，获得对应的位置称呼，输出字符仅作文字输出用
    /// </summary>
    /// <param name="pid">玩家SourceId</param>
    /// <param name="accessory"></param>
    /// <returns>该玩家对应的位置称呼</returns>
    public static string GetPlayerJobById(this ScriptAccessory accessory, uint pid)
    {
        // 获得玩家职能简称，无用处，仅作DEBUG输出
        var idx = accessory.Data.PartyList.IndexOf(pid);
        var str = accessory.GetPlayerJobByIndex(idx);
        return str;
    }

    /// 输入位置index，获得对应的位置称呼，输出字符仅作文字输出用
    /// </summary>
    /// <param name="idx">位置index</param>
    /// <param name="fourPeople">是否为四人迷宫</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static string GetPlayerJobByIndex(this ScriptAccessory accessory, int idx, bool fourPeople = false)
    {
        List<string> role8 = ["MT", "ST", "H1", "H2", "D1", "D2", "D3", "D4"];
        List<string> role4 = ["T", "H", "D1", "D2"];
        if (idx < 0 || idx >= 8 || (fourPeople && idx >= 4))
            return "Unknown";
        return fourPeople ? role4[idx] : role8[idx];
    }
}
#endregion 位置序列函数

#region 绘图函数
public static class AssignDp
{
    /// <summary>
    /// 返回箭头指引相关dp
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerObj">箭头起始，可输入uint或Vector3</param>
    /// <param name="targetObj">箭头指向目标，可输入uint或Vector3，为0则无目标</param>
    /// <param name="delay">绘图出现延时</param>
    /// <param name="destroy">绘图消失时间</param>
    /// <param name="name">绘图名称</param>
    /// <param name="rotation">箭头旋转角度</param>
    /// <param name="scale">箭头宽度</param>
    /// <param name="isSafe">使用安全色</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static DrawPropertiesEdit DrawGuidance(this ScriptAccessory accessory,
        object ownerObj, object targetObj, int delay, int destroy, string name, float rotation = 0, float scale = 1f, bool isSafe = true)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.Rotation = rotation;
        dp.ScaleMode |= ScaleMode.YByDistance;
        dp.Color = isSafe ? accessory.Data.DefaultSafeColor : accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;

        if (ownerObj is uint or ulong)
        {
            dp.Owner = (ulong)ownerObj;
        }
        else if (ownerObj is Vector3 spos)
        {
            dp.Position = spos;
        }
        else
        {
            throw new ArgumentException("ownerObj的目标类型输入错误");
        }

        if (targetObj is uint or ulong)
        {
            if ((ulong)targetObj != 0) dp.TargetObject = (ulong)targetObj;
        }
        else if (targetObj is Vector3 tpos)
        {
            dp.TargetPosition = tpos;
        }
        else
        {
            throw new ArgumentException("targetObj的目标类型输入错误");
        }

        return dp;
    }

    public static DrawPropertiesEdit DrawGuidance(this ScriptAccessory accessory,
        object targetObj, int delay, int destroy, string name, float rotation = 0, float scale = 1f, bool isSafe = true)
    => accessory.DrawGuidance((ulong)accessory.Data.Me, targetObj, delay, destroy, name, rotation, scale, isSafe);

    /// <summary>
    /// 返回扇形左右刀
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为boss</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="isLeftCleave">是左刀</param>
    /// <param name="radian">扇形角度</param>
    /// <param name="scale">扇形尺寸</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawLeftRightCleave(this ScriptAccessory accessory, ulong ownerId, bool isLeftCleave, int delay, int destroy, string name, float radian = float.Pi, float scale = 60f, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.Radian = radian;
        dp.Rotation = isLeftCleave ? float.Pi / 2 : -float.Pi / 2;
        dp.Owner = ownerId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回扇形前后刀
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为boss</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="isFrontCleave">是前刀</param>
    /// <param name="radian">扇形角度</param>
    /// <param name="scale">扇形尺寸</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawFrontBackCleave(this ScriptAccessory accessory, ulong ownerId, bool isFrontCleave, int delay, int destroy, string name, float radian = float.Pi, float scale = 60f, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.Radian = radian;
        dp.Rotation = isFrontCleave ? 0 : -float.Pi;
        dp.Owner = ownerId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回距离某对象目标最近/最远的dp
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为boss</param>
    /// <param name="orderIdx">顺序，从1开始</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="isNear">true为最近，false为最远</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawTargetNearFarOrder(this ScriptAccessory accessory, ulong ownerId, uint orderIdx,
        bool isNear, float width, float length, int delay, int destroy, string name, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Owner = ownerId;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.CentreResolvePattern =
            isNear ? PositionResolvePatternEnum.PlayerNearestOrder : PositionResolvePatternEnum.PlayerFarestOrder;
        dp.CentreOrderIndex = orderIdx;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.ScaleMode |= lengthByDistance ? ScaleMode.YByDistance : ScaleMode.None;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回距离某坐标位置最近/最远的dp
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="position">特定坐标点</param>
    /// <param name="orderIdx">顺序，从1开始</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="isNear">true为最近，false为最远</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawPositionNearFarOrder(this ScriptAccessory accessory, Vector3 position, uint orderIdx,
        bool isNear, float width, float length, int delay, int destroy, string name, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Position = position;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.TargetResolvePattern =
            isNear ? PositionResolvePatternEnum.PlayerNearestOrder : PositionResolvePatternEnum.PlayerFarestOrder;
        dp.TargetOrderIndex = orderIdx;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.ScaleMode |= lengthByDistance ? ScaleMode.YByDistance : ScaleMode.None;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回ownerId施法目标的dp
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为boss</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <param name="name">绘图名称</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawOwnersTarget(this ScriptAccessory accessory, ulong ownerId, float width, float length, int delay,
        int destroy, string name, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Owner = ownerId;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.TargetResolvePattern = PositionResolvePatternEnum.OwnerTarget;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.ScaleMode |= lengthByDistance ? ScaleMode.YByDistance : ScaleMode.None;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回ownerId仇恨相关的dp
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为boss</param>
    /// <param name="orderIdx">仇恨顺序，从1开始</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <param name="name">绘图名称</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawOwnersEnmityOrder(this ScriptAccessory accessory, ulong ownerId, uint orderIdx, float width, float length, int delay, int destroy, string name, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Owner = ownerId;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.CentreResolvePattern = PositionResolvePatternEnum.OwnerEnmityOrder;
        dp.CentreOrderIndex = orderIdx;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.ScaleMode |= lengthByDistance ? ScaleMode.YByDistance : ScaleMode.None;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回owner与target的dp，可修改 dp.Owner, dp.TargetObject, dp.Scale
    /// </summary>
    /// <param name="ownerId">起始目标id，通常为自己</param>
    /// <param name="targetId">目标单位id</param>
    /// <param name="rotation">绘图旋转角度</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawTarget2Target(this ScriptAccessory accessory, ulong ownerId, ulong targetId, float width, float length, int delay, int destroy, string name, float rotation = 0, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Rotation = rotation;
        dp.Owner = ownerId;
        dp.TargetObject = targetId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= lengthByDistance ? ScaleMode.YByDistance : ScaleMode.None;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回画向某目标的扇形绘图
    /// </summary>
    /// <param name="sourceId">起始目标id，通常为自己</param>
    /// <param name="targetId">目标单位id</param>
    /// <param name="radian">扇形角度</param>
    /// <param name="scale">扇形尺寸</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="color">绘图颜色</param>
    /// <param name="rotation">旋转角度</param>
    /// <param name="lengthByDistance">长度是否随距离改变</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawFanToTarget(this ScriptAccessory accessory, ulong sourceId, ulong targetId, float radian, float scale, int delay, int destroy, string name, Vector4 color, float rotation = 0, bool lengthByDistance = false, bool byTime = false)
    {
        var dp = accessory.DrawTarget2Target(sourceId, targetId, scale, scale, delay, destroy, name, rotation, lengthByDistance, byTime);
        dp.Radian = radian;
        dp.Color = color;
        return dp;
    }

    /// <summary>
    /// 返回owner与target之间的连线dp，使用Line绘制
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="ownerId">起始目标id，通常为自己</param>
    /// <param name="targetId">目标单位id</param>
    /// <param name="scale">线条宽度</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawConnectionBetweenTargets(this ScriptAccessory accessory, ulong ownerId,
        ulong targetId, int delay, int destroy, string name, float scale = 1f)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.Owner = ownerId;
        dp.TargetObject = targetId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= ScaleMode.YByDistance;
        return dp;
    }

    /// <summary>
    /// 返回圆形dp，跟随owner，可修改 dp.Owner, dp.Scale
    /// </summary>
    /// <param name="ownerId">起始目标id，通常为自己或Boss</param>
    /// <param name="scale">圆圈尺寸</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawCircle(this ScriptAccessory accessory, ulong ownerId, float scale, int delay, int destroy, string name, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.Owner = ownerId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回环形dp，跟随owner，可修改 dp.Owner, dp.Scale
    /// </summary>
    /// <param name="ownerId">起始目标id，通常为自己或Boss</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="scale">外环实心尺寸</param>
    /// <param name="innerScale">内环空心尺寸</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawDonut(this ScriptAccessory accessory, ulong ownerId, float scale, float innerScale, int delay, int destroy, string name, bool byTime = false)
    {
        var dp = accessory.DrawFan(ownerId, float.Pi * 2, 0, scale, innerScale, delay, destroy, name, byTime);
        return dp;
    }

    /// <summary>
    /// 返回静态dp，通常用于指引固定位置。可修改 dp.Position, dp.Rotation, dp.Scale
    /// </summary>
    /// <param name="ownerObj">绘图起始，可输入uint或Vector3</param>
    /// <param name="targetObj">绘图目标，可输入uint或Vector3，为0则无目标</param>
    /// <param name="radian">图形角度</param>
    /// <param name="rotation">旋转角度，以北为0度顺时针</param>
    /// <param name="width">绘图宽度</param>
    /// <param name="length">绘图长度</param>
    /// <param name="color">是Vector4则选用该颜色</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawStatic(this ScriptAccessory accessory, object ownerObj, object targetObj,
        float radian, float rotation, float width, float length, object color, int delay, int destroy, string name)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);

        if (ownerObj is uint or ulong)
        {
            dp.Owner = (ulong)ownerObj;
        }
        else if (ownerObj is Vector3 spos)
        {
            dp.Position = spos;
        }
        else
        {
            throw new ArgumentException("ownerObj的目标类型输入错误");
        }

        if (targetObj is uint or ulong)
        {
            if ((ulong)targetObj != 0) dp.TargetObject = (ulong)targetObj;
        }
        else if (targetObj is Vector3 tpos)
        {
            dp.TargetPosition = tpos;
        }
        else
        {
            throw new ArgumentException("ownerObj的目标类型输入错误");
        }

        dp.Radian = radian;
        dp.Rotation = rotation.Logic2Game();

        switch (color)
        {
            case Vector4 clr:
                dp.Color = clr;
                break;
            default:
                dp.Color = accessory.Data.DefaultDangerColor;
                break;
        }
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        return dp;
    }

    /// <summary>
    /// 返回静态圆圈dp，通常用于指引固定位置。
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="center">圆圈中心位置</param>
    /// <param name="color">圆圈颜色</param>
    /// <param name="scale">圆圈尺寸，默认1.5f</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawStaticCircle(this ScriptAccessory accessory, Vector3 center, Vector4 color,
        int delay, int destroy, string name, float scale = 1.5f)
        => accessory.DrawStatic(center, (ulong)0, 0, 0, scale, scale, color, delay, destroy, name);
    // {
    //     var dp = accessory.DrawStatic(center, (uint)0, 0, 0, scale, scale, color, delay, destroy, name);
    //     return dp;
    // }

    /// <summary>
    /// 返回静态月环dp，通常用于指引固定位置。
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="center">月环中心位置</param>
    /// <param name="color">月环颜色</param>
    /// <param name="scale">月环外径，默认1.5f</param>
    /// <param name="innerscale">月环内径，默认scale-0.05f</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawStaticDonut(this ScriptAccessory accessory, Vector3 center, Vector4 color,
        int delay, int destroy, string name, float scale, float innerscale = 0)
        => accessory.DrawStatic(center, (ulong)0,
        float.Pi * 2, 0, scale, scale, color, delay, destroy, name);

    // {
    //     var dp = accessory.DrawStatic(center, (uint)0, float.Pi * 2, 0, scale, scale, color, delay, destroy, name);
    //     dp.InnerScale = innerscale != 0f ? new Vector2(innerscale) : new Vector2(scale - 0.05f);
    //     return dp;
    // }

    /// <summary>
    /// 返回矩形
    /// </summary>
    /// <param name="ownerId">起始目标id，通常为自己或Boss</param>
    /// <param name="width">矩形宽度</param>
    /// <param name="length">矩形长度</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawRect(this ScriptAccessory accessory, ulong ownerId, float width, float length, int delay, int destroy, string name, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Owner = ownerId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回扇形
    /// </summary>
    /// <param name="ownerId">起始目标id，通常为自己或Boss</param>
    /// <param name="radian">扇形弧度</param>
    /// <param name="rotation">图形旋转角度</param>
    /// <param name="scale">扇形尺寸</param>
    /// <param name="innerScale">扇形内环空心尺寸</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <param name="accessory"></param>
    /// <returns></returns>
    public static DrawPropertiesEdit DrawFan(this ScriptAccessory accessory, ulong ownerId, float radian, float rotation, float scale, float innerScale, int delay, int destroy, string name, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(scale);
        dp.InnerScale = new Vector2(innerScale);
        dp.Radian = radian;
        dp.Rotation = rotation;
        dp.Owner = ownerId;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    /// <summary>
    /// 返回击退
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="target">击退源，可输入uint或Vector3</param>
    /// <param name="width">击退绘图宽度</param>
    /// <param name="length">击退绘图长度/距离</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="ownerId">起始目标ID，通常为自己或其他玩家</param>
    /// <param name="byTime">动画效果随时间填充</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static DrawPropertiesEdit DrawKnockBack(this ScriptAccessory accessory, ulong ownerId, object target, float length, int delay, int destroy, string name, float width = 1.5f, bool byTime = false)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Scale = new Vector2(width, length);
        dp.Owner = ownerId;

        if (target is uint or ulong)
        {
            dp.TargetObject = (ulong)target;
        }
        else if (target is Vector3 tpos)
        {
            dp.TargetPosition = tpos;
        }
        else
        {
            throw new ArgumentException("DrawKnockBack的目标类型输入错误");
        }

        dp.Rotation = float.Pi;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Delay = delay;
        dp.DestoryAt = destroy;
        dp.ScaleMode |= byTime ? ScaleMode.ByTime : ScaleMode.None;
        return dp;
    }

    public static DrawPropertiesEdit DrawKnockBack(this ScriptAccessory accessory, object target, float length,
        int delay, int destroy, string name, float width = 1.5f, bool byTime = false)
        => accessory.DrawKnockBack(accessory.Data.Me, target, length, delay, destroy, name, width, byTime);

    /// <summary>
    /// 返回背对
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="target">背对源，可输入uint或Vector3</param>
    /// <param name="delay">延时delay ms出现</param>
    /// <param name="destroy">绘图自出现起，经destroy ms消失</param>
    /// <param name="name">绘图名称</param>
    /// <param name="ownerId">起始目标ID，通常为自己或其他玩家</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static DrawPropertiesEdit DrawSightAvoid(this ScriptAccessory accessory, ulong ownerId, object target, int delay, int destroy, string name)
    {
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Color = accessory.Data.DefaultDangerColor;
        dp.Owner = ownerId;


        if (target is uint or ulong)
        {
            dp.TargetObject = (ulong)target;
        }
        else if (target is Vector3 tpos)
        {
            dp.TargetPosition = tpos;
        }
        else
        {
            throw new ArgumentException("DrawKnockBack的目标类型输入错误");
        }

        dp.Delay = delay;
        dp.DestoryAt = destroy;
        return dp;
    }

    public static DrawPropertiesEdit DrawSightAvoid(this ScriptAccessory accessory, object target, int delay,
        int destroy, string name)
        => accessory.DrawSightAvoid(accessory.Data.Me, target, delay, destroy, name);

    /// <summary>
    /// 返回多方向延伸指引
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="owner">分散源</param>
    /// <param name="extendDirs">分散角度</param>
    /// <param name="myDirIdx">玩家对应角度idx</param>
    /// <param name="width">指引箭头宽度</param>
    /// <param name="length">指引箭头长度</param>
    /// <param name="delay">绘图出现延时</param>
    /// <param name="destroy">绘图消失时间</param>
    /// <param name="name">绘图名称</param>
    /// <param name="colorPlayer">玩家对应箭头指引颜色</param>
    /// <param name="colorNormal">其他玩家对应箭头指引颜色</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<DrawPropertiesEdit> DrawExtendDirection(this ScriptAccessory accessory, object owner,
        List<float> extendDirs, int myDirIdx, float width, float length, int delay, int destroy, string name,
        Vector4 colorPlayer, Vector4 colorNormal)
    {
        List<DrawPropertiesEdit> dpList = [];


        if (owner is uint or ulong)
        {
            for (var i = 0; i < extendDirs.Count; i++)
            {
                var dp = accessory.DrawRect((ulong)owner, width, length, delay, destroy, $"{name}{i}");
                dp.Rotation = extendDirs[i];
                dp.Color = i == myDirIdx ? colorPlayer : colorNormal;
                dpList.Add(dp);
            }
        }
        else if (owner is Vector3 spos)
        {
            for (var i = 0; i < extendDirs.Count; i++)
            {
                var dp = accessory.DrawGuidance(spos, spos.ExtendPoint(extendDirs[i], length), delay, destroy,
                    $"{name}{i}", 0, width);
                dp.Color = i == myDirIdx ? colorPlayer : colorNormal;
                dpList.Add(dp);
            }
        }
        else
        {
            throw new ArgumentException("DrawExtendDirection的目标类型输入错误");
        }

        return dpList;
    }

    /// <summary>
    /// 返回多地点指路指引列表
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="positions">地点位置</param>
    /// <param name="delay">绘图出现延时</param>
    /// <param name="destroy">绘图消失时间</param>
    /// <param name="name">绘图名称</param>
    /// <param name="colorPosPlayer">对应位置标记行动颜色</param>
    /// <param name="colorPosNormal">对应位置标记准备颜色</param>
    /// <param name="colorGo">指路出发箭头颜色</param>
    /// <param name="colorPrepare">指路准备箭头颜色</param>
    /// <returns>dpList中的三个List：位置标记，玩家指路箭头，地点至下个地点的指路箭头</returns>
    public static List<List<DrawPropertiesEdit>> DrawMultiGuidance(this ScriptAccessory accessory,
        List<Vector3> positions, List<int> delay, List<int> destroy, string name,
        Vector4 colorGo, Vector4 colorPrepare, Vector4 colorPosNormal, Vector4 colorPosPlayer)
    {
        List<List<DrawPropertiesEdit>> dpList = [[], [], []];
        for (var i = 0; i < positions.Count; i++)
        {
            var dpPos = accessory.DrawStaticCircle(positions[i], colorPosPlayer, delay[i], destroy[i], $"{name}pos{i}");
            dpList[0].Add(dpPos);
            var dpGuide = accessory.DrawGuidance(positions[i], colorGo, delay[i], destroy[i], $"{name}guide{i}");
            dpList[1].Add(dpGuide);
            if (i == positions.Count - 1) break;
            var dpPrep = accessory.DrawGuidance(positions[i], positions[i + 1], delay[i], destroy[i], $"{name}prep{i}");
            dpList[2].Add(dpPrep);
        }
        return dpList;
    }

    public static void DebugMsg(this ScriptAccessory accessory, string str, bool debugMode = false, bool debugChat = false)
    {
        if (!debugMode)
            return;
        accessory.Log.Debug($"/e [DEBUG] {str}");

        if (!debugChat)
            return;
        accessory.Method.SendChat($"/e [DEBUG] {str}");
    }

    /// <summary>
    /// 将List内信息转换为字符串。
    /// </summary>
    /// <param name="accessory"></param>
    /// <param name="myList"></param>
    /// <param name="isJob">是职业，在转为字符串前调用转职业函数</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string BuildListStr<T>(this ScriptAccessory accessory, List<T> myList, bool isJob = false)
    {
        return string.Join(", ", myList.Select(item =>
        {
            if (isJob && item != null && item is int i)
                return accessory.GetPlayerJobByIndex(i);
            return item?.ToString() ?? "";
        }));
    }
}

#endregion 绘图函数

