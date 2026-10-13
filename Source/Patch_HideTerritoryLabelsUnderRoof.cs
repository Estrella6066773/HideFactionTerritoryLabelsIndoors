using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace HideFactionTerritoryLabelsIndoors
{
    /// <summary>
    /// 在太空地点的室内，不绘制 Faction Territories and Vassalage 的派系名称。
    ///
    /// 联动关系：原模组在 <c>WorldLayer_MapMode.OnGUI</c> 的后缀里，用
    /// <c>GenWorldUI.WorldToUIPosition</c> 把领土中心投到屏幕，再调用私有方法
    /// <c>DrawOutlinedLabel</c> 画带描边的名称。每个名称会连画九次（八向描边加正文）。
    /// 奥德赛太空地点的 <c>MapGeneratorDef.renderWorld</c> 为真，星球作为背景继续绘制，
    /// 所以上面这条界面文字路径在室内也会跑。屋顶只挡住地图网格，挡不住屏幕文字。
    ///
    /// 本类在 <c>DrawOutlinedLabel</c> 之前判断，两层：
    /// 1. <see cref="HideFactionTerritoryLabelsSettings.disableTerritoryLabels"/> 为真时，
    ///    世界地图和太空地点上的名称一律不画。设置窗口每次改完立刻生效。
    /// 2. 开关关掉之后，仅当 <see cref="WorldRendererUtility.WorldBackgroundNow"/>
    ///    （正在看太空地点，而不是打开世界地图）且文字矩形压到有屋顶的格子时，跳过这次绘制。
    ///    世界地图、以及室外能看见星球的位置仍画名称。
    ///
    /// 注意：
    /// 1. 目标方法是原模组的私有方法，不能写成编译期 HarmonyPatch，只能在加载后按类型名挂钩。
    ///    原模组若改名或把绘制内联进后缀，本补丁会挂不上，并在日志里报一次错误。
    /// 2. 文字坐标是界面坐标（左上角为原点，已除过界面缩放）。
    ///    <see cref="UI.UIToMapPosition"/> 要的是屏幕坐标（左下角为原点），转换时必须把 Y 翻回来。
    /// 3. 同一名称的九次绘制中心只差 1 像素。同一帧里中心距离很近时复用第一次的判断，
    ///    避免描边和正文一边画一边不画。
    /// </summary>
    public static class Patch_HideTerritoryLabelsUnderRoof
    {
        private const string TargetTypeName = "FactionTerritories.WorldLayer_MapMode_OnGUI_FactionTerritoriesLabels";

        private const string TargetMethodName = "DrawOutlinedLabel";

        /// <summary>描边相对正文最多偏 1 像素，对角线距离的平方是 2。4 留一点余量，又远小于两个名称之间的间距。</summary>
        private const float SameLabelCenterDistanceSqr = 4f;

        private static int cachedFrame = -1;

        private static Vector2 cachedCenter;

        private static bool cachedHide;

        public static void Apply(Harmony harmony)
        {
            System.Reflection.MethodBase method = AccessTools.Method(AccessTools.TypeByName(TargetTypeName), TargetMethodName);
            if (method == null)
            {
                Log.Error("[HideFactionTerritoryLabelsIndoors] 没有找到 Faction Territories and Vassalage 的领土名称绘制方法。原模组可能已更新，室内名称补丁没有生效。");
                return;
            }

            harmony.Patch(method, prefix: new HarmonyMethod(typeof(Patch_HideTerritoryLabelsUnderRoof), nameof(Prefix)));
        }

        /// <summary>
        /// 返回 false 时跳过原方法，这一次描边或正文都不会画。
        /// 原方法自己保存并恢复 <c>GUI.color</c>，跳过它不会把颜色留在描边色上。
        ///
        /// 总开关优先于室内判断：名称整段关掉时不必再做屋顶检测。
        /// Settings 尚未构造时不走总开关，避免启动顺序把名称误关。
        /// </summary>
        public static bool Prefix(Rect rect)
        {
            if (HideFactionTerritoryLabelsMod.Settings?.disableTerritoryLabels == true)
            {
                return false;
            }

            Vector2 center = rect.center;
            int frame = Time.frameCount;
            if (frame == cachedFrame && (center - cachedCenter).sqrMagnitude <= SameLabelCenterDistanceSqr)
            {
                return !cachedHide;
            }

            cachedFrame = frame;
            cachedCenter = center;
            cachedHide = ShouldHide(rect);
            return !cachedHide;
        }

        private static bool ShouldHide(Rect rect)
        {
            if (!WorldRendererUtility.WorldBackgroundNow)
            {
                return false;
            }

            Map map = Find.CurrentMap;
            if (map?.roofGrid == null)
            {
                return false;
            }

            if (CoversRoof(rect.center, map))
            {
                return true;
            }

            if (CoversRoof(new Vector2(rect.xMin, rect.yMin), map))
            {
                return true;
            }

            if (CoversRoof(new Vector2(rect.xMax, rect.yMin), map))
            {
                return true;
            }

            if (CoversRoof(new Vector2(rect.xMin, rect.yMax), map))
            {
                return true;
            }

            return CoversRoof(new Vector2(rect.xMax, rect.yMax), map);
        }

        private static bool CoversRoof(Vector2 guiPoint, Map map)
        {
            float screenYFromBottom = UI.screenHeight - guiPoint.y;
            IntVec3 cell = UI.UIToMapPosition(new Vector2(guiPoint.x, screenYFromBottom)).ToIntVec3();
            if (!cell.InBounds(map))
            {
                return false;
            }

            return map.roofGrid.Roofed(cell);
        }
    }
}
