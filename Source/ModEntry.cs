using System.Reflection;
using HarmonyLib;
using Verse;

namespace HideFactionTerritoryLabelsIndoors
{
    /// <summary>
    /// 模组入口。RimWorld 启动时执行带 StaticConstructorOnStartup 标记的静态构造函数。
    ///
    /// 联动关系：这里只挂载补丁。名称画不画由
    /// <see cref="Patch_HideTerritoryLabelsUnderRoof"/> 决定，它读取
    /// <see cref="HideFactionTerritoryLabelsMod.Settings"/> 的总开关，再处理太空地点室内遮挡。
    /// 设置类本身由 RimWorld 反射构造，不经过这里。
    ///
    /// 注意：必须排在 jaeger972.factionterritories 之后加载，否则目标类型还不在程序集里，补丁会挂空。
    /// HarmonyId 与 About.xml 的 packageId 相同，避免和别的模组抢同一个 Harmony 实例。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ModEntry
    {
        public const string HarmonyId = "Estrella6066.hideFactionTerritoryLabelsIndoors";

        static ModEntry()
        {
            Harmony harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Patch_HideTerritoryLabelsUnderRoof.Apply(harmony);
        }
    }
}
