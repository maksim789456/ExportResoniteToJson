using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;

namespace ExportNeosToJson
{
    /// <summary>
    /// Problem: ExportDialog to small to fit all export option with this mod
    /// </summary>
    [HarmonyPatch(typeof(ExportDialog), "Setup")]
    public class FixExportDialogMinHeight
    {
        static void Postfix(ExportDialog __instance)
        {
            //ExportDialog -> Image/Content/VerticalLayout/ScrollArea/Content/VerticalLayout
            var layoutElement = __instance.Slot[0][1][0][0][0][0].GetComponent<LayoutElement>();
            layoutElement.MinHeight.Value = 600;
            layoutElement.PreferredHeight.Value = 600;
        }
    }
}