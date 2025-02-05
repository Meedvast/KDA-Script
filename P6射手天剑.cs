using System;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Script;
using KodakkuAssist.Module.GameEvent.Struct;
using KodakkuAssist.Module.Draw;
using System.Windows.Forms;
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
using ECommons.GameFunctions;

namespace Meva.EndWalker.TheOmegaProtocol;

[ScriptType(name: "欧米茄P6射手天剑", territorys: [1122], guid: "120df6f8-d8ce-44f7-9fb0-431eca0f2825",
    version: "0.0.0.1", author: "Meva", note: noteStr)]
public class P6射手天剑
{
    public enum Pattern { Unknown, InOut, OutIn }
    private Pattern _curPattern = Pattern.Unknown;
    
    [UserSetting("天剑颜色")]
    public ScriptColor ArrorColor { get; set; } = new() { V4 = new(1, 0, 0, 1) };
    private Vector3 MapCenter = new(100, 0, 100);
    public int ArrowNum = 0;
    public bool isSet = false;
    // 0为先十字，1为先外圈
    public int arrowMode = -1;
    const string InOut = "InOut";
    const string OutIn = "OutIn";
    const string noteStr =
        """
        v0.0.0.1:
        """;
    
    public void Init(ScriptAccessory accessory)
    {
        arrowMode = -1;
        ArrowNum = 0;
        accessory.Method.RemoveDraw(".*");
    }

    [ScriptMethod(name: "宇宙天箭计数", eventType: EventTypeEnum.ActionEffect, eventCondition: ["ActionId:31650"], userControl: false)]
    public void 宇宙天箭计数(Event @event, ScriptAccessory accessory)
    {
        ArrowNum++;
        isSet = false;
    }
    [ScriptMethod(name: "宇宙天箭", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31651"])]
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
            }
        }
        else if (Math.Abs(offset.X) < 18) // 侧边垂直线
        {
            GenerateLine(accessory, casterPos, new Vector2(offset.X < 0 ? 1 : -1, 0), 7, OutIn);
            if (!isSet)
            {
                arrowMode = 1;
                isSet = true;
            }
        }
        else if (Math.Abs(offset.Z) < 18) // 侧边水平线
        {
            GenerateLine(accessory, casterPos, new Vector2(0, offset.Z < 0 ? 1 : -1), 7, OutIn);
            if (!isSet)
            {
                arrowMode = 1;
                isSet = true;
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

    [ScriptMethod(name: "宇宙天箭指路", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:31650"])]
    public async void 宇宙天箭指路(Event @event, ScriptAccessory accessory)
    {
        await Task.Delay(500);
        var myindex = ArrowNum < 1 ? 4 : accessory.Data.PartyList.IndexOf(accessory.Data.Me);
        int offset1 = arrowMode < 1 ? 13 : 17;
        int offset2 = arrowMode < 1 ? 7: 23;
        int offset3 = arrowMode < 1 ? 23 : 17;
        var offset4 = 12.5f;
        var offset5 = 7.5f;
        var delayMode = arrowMode < 1 ? 4000 : 2000;
        var delayModeTN = arrowMode < 1 && myindex < 4 ? 2000 : 0;
        accessory.Method.TextInfo($"{arrowMode}_{delayMode}_{delayModeTN}", duration: 5000, true);
        
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
            // MT和D3（基准坐标）
            0 or 6 => coordinate2,
            
            // ST和D4
            1 or 7 => new Vector3(coordinate2.X + offset3, 0, coordinate2.Z),
            
            // H1和D1
            2 or 4 => new Vector3(coordinate2.X, 0, coordinate2.Z + offset3),
            
            // H2和D2
            3 or 5 => new Vector3(coordinate2.X + offset3, 0, coordinate2.Z + offset3),
                
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
            dp.Scale = new(1);
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
            dp.Scale = new(1);
            dp.Position = dealpos0;
            dp.TargetPosition = dealpos1;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 7500;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P6_射手天箭_2";
            dp.Scale = new(1);
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
            dp.Scale = new(1);
            dp.Position = dealpos1;
            dp.TargetPosition = dealpos2;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 11500;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P6_射手天箭_3";
            dp.Scale = new(1);
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
            dp.Scale = new(1);
            dp.Position = dealpos2;
            dp.TargetPosition = dealpos3;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 13500 + delayModeTN;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P6_射手天箭_4";
            dp.Scale = new(1);
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
            dp.Scale = new(1);
            dp.Position = dealpos3;
            dp.TargetPosition = dealpos4;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultDangerColor;
            dp.DestoryAt = 13500 + delayMode;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Name = "P6_射手天箭_5";
            dp.Scale = new(1);
            dp.Owner = accessory.Data.Me;
            dp.TargetPosition = dealpos4;
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.Delay = 13500 + delayMode;
            dp.DestoryAt = 2000;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
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