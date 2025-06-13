using System;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Script;
using KodakkuAssist.Module.GameEvent.Struct;
using KodakkuAssist.Module.Draw;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommons;
using System.Numerics;
using Newtonsoft.Json;
using System.Linq;
using System.ComponentModel;
using System.Xml.Linq;
using Dalamud.Utility.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ECommons.GameFunctions;
using KodakkuAssist.Module.GameOperate;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace MyScriptNamespace
{
    
    [ScriptType(name: "绝欧精装抢先体验版", territorys: [1122],guid: "e0bfb4db-0d38-909f-5088-b23f09b7585e", version:"0.0.0.1", author:"Karlin",note: noteStr)]
    public class OmegaProtocolUltimate
    {
        const string noteStr =
        """
        欧米茄验证绝境战(基于K佬原有脚本添加P5二三运，P6指路)
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
        int P52_MarkType;
        int P53_MarkType;
        float P53_4_HW;
        int P5_3_MF = 0;

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

        public void Init(ScriptAccessory accessory)
        {
            parse = 0;
            arrowMode = -1;
            ArrowNum = 0;
            CannonNum = 0;
            StepCannonIndex = 0;
            ArrowModeConfirmed = new System.Threading.AutoResetEvent(false);
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
                    dp.Offset = new(-5, 0, 0);
                    dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                    dp.DestoryAt = 5500;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = "P1_协同程序PT_女辣翅2";
                    dp.Scale = new(60, 20);
                    dp.Owner = sid;
                    dp.Rotation = float.Pi / -2;
                    dp.Offset = new(5, 0, 0);
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
        }
        
        [ScriptMethod(name: "P5_二运_分P", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:32788"], userControl: false)]
        public void P5_二运_分P(Event @event, ScriptAccessory accessory)
        {
            parse = 5.2;
            P52_semaphoreTowersWereConfirmed = new AutoResetEvent(false);
        }
        
        [ScriptMethod(name: "P5_二运_男人位置", eventType: EventTypeEnum.AddCombatant, eventCondition: ["DataId:15724"],userControl: false)]
        public void P5_二运_男人位置(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.2) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            P52_OmegaMDir = (RoundPositionTo8Dir(pos, new(100, 0, 100))+4)%8;
            
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
        public void P5_二运_塔位置(Event @event, ScriptAccessory accessory)
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

            int count = P52_Towers.Count(x => x != 0);
            if (count == 6 && !P5_SigmaBuffIsFar)
            {
                P52_semaphoreTowersWereConfirmed.Set();
            }
            else if(count == 5 && P5_SigmaBuffIsFar)
            {
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

            int tempCwDirIndex;
            int tempCcwDirIndex;
            int tempSide = 0;
            int targetIndex = 0;
            

            if (P5_SigmaBuffIsFar)
            {
                tempCwDirIndex = (P52_Self_Dir + 15) % 16;
                tempCcwDirIndex = (P52_Self_Dir + 1) % 16;
            }
            else
            {
                tempCwDirIndex = (P52_Self_Dir + 14) % 16;
                tempCcwDirIndex = (P52_Self_Dir + 2) % 16;
            }

            if (P52_Towers[tempCwDirIndex] == 2)
            {
                tempSide = 1;
            }
            else if (P52_Towers[tempCcwDirIndex] == 2)
            {
                tempSide = 2;
            }
            else
            {
                bool cwIsSingle = P52_Towers[tempCwDirIndex] == 1;
                bool ccwIsNotDouble = P52_Towers[tempCcwDirIndex] != 2;

                if (cwIsSingle && ccwIsNotDouble)
                {
                    tempSide = 1;
                }
                else
                {
                    bool ccwIsSingle = P52_Towers[tempCwDirIndex] == 1;
                    bool cwIsNotDouble = P52_Towers[tempCcwDirIndex] != 2;

                    if (ccwIsSingle && cwIsNotDouble)
                    {
                        tempSide = 2;
                    }
                }
            }

            if (tempSide != 0)
            {
                targetIndex = tempSide == 1 ? tempCwDirIndex : tempCcwDirIndex;
            }
            
            var dp1 = accessory.Data.GetDefaultDrawProperties();
            dp1.Name = "P52_击退指路";
            dp1.Scale = new(2);
            dp1.Owner = accessory.Data.Me;
            dp1.TargetPosition = P52_TowerPos[targetIndex];
            dp1.ScaleMode |= ScaleMode.YByDistance;
            dp1.Color = accessory.Data.DefaultSafeColor;
            dp1.DestoryAt = 6500;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp1);
        }
        
        
        [ScriptMethod(name: "P5_二运_后半", eventType: EventTypeEnum.StatusRemove, eventCondition: ["StatusID:regex:^(3427|3428)$"],userControl: false)]
        public void P5_二运_后半(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.2) return;
            parse = 5.21;
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
            }
        }
        
        [ScriptMethod(name: "P5_二运_旋转激光", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:regex:^(009C|009D)$"],userControl: true)]
        public void P5_二运_旋转激光(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.21) return;
            if (!ParseObjectId(@event["TargetId"], out var tid)) return;
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
                dp.Position = new Vector3(100, 0, 100);
                dp.Rotation = ((float.Pi / 4 * P52_OmegaFDir + float.Pi / 2) + (0.152f * i * dx)) % float.Pi; ;
                dp.Color = accessory.Data.DefaultDangerColor;
                dp.DestoryAt = 10000 + 620*i;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Straight, dp);
            }
        }
        
        [ScriptMethod(name: "P5_二运_后半起跑点指路", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:regex:^(009C|009D)$"],userControl: true)]
        public void P5_二运_后半起跑点指路(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.21) return;
            Vector3 dealpos = Vector3.Zero;
            if (@event["Id"] == "009C")
            {
                if (P52_MarkType == 7 || P52_MarkType == 8 || P52_MarkType == 1)
                {
                    dealpos = RotatePoint(new Vector3(93f, 0f, 82f), new Vector3(100,0,100), float.Pi / 4 * P52_OmegaFDir);
                }
                else
                {
                    dealpos = RotatePoint(new Vector3(107f, 0f, 118f), new Vector3(100,0,100), float.Pi / 4 * P52_OmegaFDir);
                }

            }else if (@event["Id"] == "009D")
            {
                if (P52_MarkType == 7 || P52_MarkType == 8 || P52_MarkType == 1)
                {
                    dealpos = RotatePoint(new Vector3(107f, 0f, 82f), new Vector3(100,0,100), float.Pi / 4 * P52_OmegaFDir);
                }
                else
                {
                    dealpos = RotatePoint(new Vector3(93f, 0f, 118f), new Vector3(100,0,100), float.Pi / 4 * P52_OmegaFDir);
                }
            }
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = $"P5_二运_后半起跑点";
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
            if (parse != 5.21) return;
            if (!ParseObjectId(@event["SourceId"], out var sid)) return;
            var transformationID = GetTransformationID(sid, accessory);
            if (transformationID == null) return;
            if (transformationID == 4)
            {
                var dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P5_二运_女辣翅1";
                dp.Scale = new(60, 20);
                dp.Owner = sid;
                dp.Rotation = float.Pi / 2;
                dp.Offset = new(-5, 0, 0);
                dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                dp.Delay = 12000;
                dp.DestoryAt = 3000;
                accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);

                dp = accessory.Data.GetDefaultDrawProperties();
                dp.Name = "P5_二运_女辣翅2";
                dp.Scale = new(60, 20);
                dp.Owner = sid;
                dp.Rotation = float.Pi / -2;
                dp.Offset = new(5, 0, 0);
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
            parse = 5.22;
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
            dp.Delay = 10000;
            dp.DestoryAt = 9000;
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
            float rot = JsonConvert.DeserializeObject<int>(@event["ActionId"]) == 31644 ? 90 : 0;
            
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
                    dp.Offset = new(-5, 0, 0);
                    dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                    dp.Delay = P5_3_MF < 3 ? 0 : 9000;
                    dp.DestoryAt = P5_3_MF < 3? 13000 : 4000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);

                    dp = accessory.Data.GetDefaultDrawProperties();
                    dp.Name = $"P5_三运_女辣翅2{P5_3_MF}";
                    dp.Scale = new(60, 20);
                    dp.Owner = sid;
                    dp.Rotation = float.Pi / -2;
                    dp.Offset = new(5, 0, 0);
                    dp.Color = accessory.Data.DefaultDangerColor.WithW(2);
                    dp.Delay = P5_3_MF < 3 ? 0 : 9000;
                    dp.DestoryAt = P5_3_MF < 3? 13000 : 4000;
                    accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
                }
            }
        }
        
        [ScriptMethod(name: "P5_三运_三传指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(31638|31639)$"], userControl: true)]
        public void P5_三运_三传(Event @event, ScriptAccessory accessory)
        {
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
            
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_三传";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 10000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            parse = 5.4;
        }
        
        [ScriptMethod(name: "P5_三运_四传指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:32374"], userControl: true)]
        public void P5_三运_四传(Event @event, ScriptAccessory accessory)
        {
            Task.Delay(1000).Wait();
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
            
            var dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P5_三运_四传";
            dp.Scale = new(2);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = 10000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
        
        [ScriptMethod(name: "P5_三运_四传方向获取", eventType: EventTypeEnum.AddCombatant, eventCondition: ["DataId:15724"], userControl: false)]
        public void P5_三运_四传方向获取(Event @event, ScriptAccessory accessory)
        {
            if (parse != 5.3) return;
            var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            P53_4_HW = RoundPositionTo8Dir(pos, new(100, 0, 100));
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

