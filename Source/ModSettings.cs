using UnityEngine;
using Verse;

namespace HideFactionTerritoryLabelsIndoors
{
    /// <summary>
    /// 设置窗口挂在这里。
    ///
    /// 联动关系：
    ///   游戏启动 → LoadedModManager.CreateModClasses 构造本类 → GetSettings 读出设置文件
    ///   玩家改设置 → DoSettingsWindowContents → Settings.DoWindowContents
    ///   关闭设置窗口 → Dialog_ModSettings.PreClose → Mod.WriteSettings 写回设置文件
    ///   绘制名称时 → Patch_HideTerritoryLabelsUnderRoof.Prefix 每次现读 disableTerritoryLabels，改完立刻生效
    ///
    /// 注意：RimWorld 只认「继承 Mod、且构造函数只有一个 ModContentPack 参数」的类型，本类必须 public。
    /// Settings 是静态字段；补丁在它还没建好时不把名称整段关掉，只保留室内遮挡。
    /// </summary>
    public class HideFactionTerritoryLabelsMod : Mod
    {
        public static HideFactionTerritoryLabelsSettings Settings;

        public HideFactionTerritoryLabelsMod(ModContentPack content)
            : base(content)
        {
            Settings = GetSettings<HideFactionTerritoryLabelsSettings>();
        }

        public override string SettingsCategory()
        {
            return Content.Name;
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings?.DoWindowContents(inRect);
        }
    }

    /// <summary>
    /// 是否整段关掉 Faction Territories and Vassalage 的派系名称。
    ///
    /// 默认关掉名称：这个文字是屏幕界面，太空地点的屋顶挡不住，也是玩家要这个开关的原因。
    /// 取消勾选后名称恢复，室内遮挡仍由补丁负责。领土色块和边界不归这个开关管。
    /// </summary>
    public class HideFactionTerritoryLabelsSettings : ModSettings
    {
        public bool disableTerritoryLabels = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref disableTerritoryLabels, "disableTerritoryLabels", defaultValue: true);
        }

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.CheckboxLabeled(
                "HideTerritoryLabels_Disable".Translate(),
                ref disableTerritoryLabels,
                "HideTerritoryLabels_DisableTip".Translate());
            listing.Gap(8f);
            listing.Label(disableTerritoryLabels
                ? "HideTerritoryLabels_DisabledNote".Translate()
                : "HideTerritoryLabels_EnabledNote".Translate());
            listing.End();
        }
    }
}
