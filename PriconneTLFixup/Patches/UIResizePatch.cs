﻿using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

/**
 * The Grotto button text is much smaller than the surrounding ones. Scale it up to match.
 */
[HarmonyPatch(typeof(ViewQuestTop), nameof(ViewQuestTop.StartView))]
[HarmonyWrapSafe]
public class GrottoButtonPatch
{
    public static void Postfix(ViewQuestTop __instance)
    {
        __instance.buttonSpecialQuest.Button.GetChildUILabel().lineWidth = 95;
    }
}

/**
 * Same with the settings button.
 */
[HarmonyPatch(typeof(ViewMenuTop), nameof(ViewMenuTop.StartView))]
[HarmonyWrapSafe]
public class SettingsButtonPatch
{
    public static void Postfix(ViewMenuTop __instance)
    {
        __instance.systemButton.GetChildUILabel().lineWidth = 115;
        __instance.cartoonButton.GetChildUILabel().lineWidth = 190;
    }
}

[HarmonyPatch(typeof(StoryScene), nameof(StoryScene.ViewPlaceName))]
[HarmonyPriority(Priority.VeryLow)]
[HarmonyWrapSafe]
public class StoryPlaceNamePatch2
{
    public static bool Prefix(StoryScene __instance, string _placeName)
    {
        var storyIcon = __instance.placeIcon;
        storyIcon.placeObjAnimation.enabled = false;
        storyIcon.placeHeaderLabel.gameObject.SetActive(false);
        storyIcon.placeSubLabel.SetText(_placeName);
        var transform = storyIcon.placeSubLabel.transform;
        var pos = transform.localPosition;
        pos.x = 40;
        transform.localPosition = pos;
        storyIcon.lineSprite.width = 83 + storyIcon.placeSubLabel.width;
        storyIcon.StartAnimation();
        return false;
    }
}

/**
 * Header underline length is determined by assuming a certain character width that works for CJK characters.
 * For the latin alphabet, we instead first set the text and then calculate the actual width of that label.
 */
[HarmonyPatch(typeof(PartsHeaderBackButton), nameof(PartsHeaderBackButton.SetTitleText))]
[HarmonyPriority(Priority.VeryLow)]
[HarmonyWrapSafe]
public class TitleTextPatch
{
    public static bool Prefix(PartsHeaderBackButton __instance, string _setTitleText)
    {
        if (_setTitleText.IsNullOrEmpty())
        {
            __instance.subTitleLabel.SetActiveWithCheck(false);
            return false;
        }
        var activeSelf = __instance.gameObject.activeSelf;
        __instance.gameObject.SetActive(false);
        __instance.gameObject.SetActive(true);
        __instance.titleLabel.SetText("");
        __instance.titleLabel2nd.SetText(_setTitleText);
        __instance.titleLabel.SetActive(false);
        __instance.titleLabel2nd.SetActive(false);
        __instance.titleLabel.SetActive(true);
        __instance.titleLabel2nd.SetActive(true);
        __instance.gameObject.SetActive(activeSelf);
        var matchCollection = Regex.Matches(__instance.titleLabel2nd.text, "[a-zA-Z0-9]");
        var num = __instance.titleLabel2nd.text.Length - matchCollection.Count;
        var fontSize = __instance.titleLabel2nd.fontSize;
        var num2 = Mathf.CeilToInt(fontSize * 0.75f);
        var num3 = __instance.titleLabel.text.Length * __instance.titleLabel.fontSize + num * fontSize + matchCollection.Count * num2;
        __instance.underLine.width = __instance.leftOffset + num3 + __instance.rightOffset;
        __instance.underLine.gameObject.SetActive(true);
        __instance.subTitleLabel.SetActiveWithCheck(false);
        
        Log.Debug($"TitleTextPatch: _setTitleText = {_setTitleText}, text = {__instance.titleLabel2nd.text}, fontSize = {__instance.titleLabel2nd.fontSize}");

        var transform = __instance.titleLabel.transform;
        var pos = transform.localPosition;
        pos.x = __instance.backButton == null ? 22 : 72;
        transform.localPosition = pos;

        var newUnderlineWidth =
            Convert.ToInt16(__instance.titleLabel2nd.text.Length * __instance.titleLabel2nd.fontSize) + 20;
        __instance.underLine.width = newUnderlineWidth;
        var headerController = SingletonMonoBehaviour<HeaderController>.Instance;
        headerController.campaignIcons.SetIconPosition(
            headerController.viewManager.CurrentViewId,
            newUnderlineWidth 
        );
        
        CoroutineStarter.Instance.StartCoroutine(
            WaitForTranslationCoroutine(__instance.titleLabel2nd, __instance.underLine, _setTitleText).WrapToIl2Cpp());

        return false;
    }

    private static IEnumerator WaitForTranslationCoroutine(UILabel label, UIWidget underline, string originalText)
    {
        Log.Debug($"Before: text = {label.text}, fontSize = {label.fontSize}");
        var yieldInstruction = new Util.WaitForSecondsOrPredicate(5.0f, () => originalText != label.text);
        while (yieldInstruction.keepWaiting)
        {
            yield return null;
        }

        var labelSize = label.mCalculatedSize;
        var newUnderlineWidth = labelSize.x + (label.fontSize == 30 ? 20 : 80);
        Log.Debug($"TitleTextCoroutine: newUnderlineWidth = {newUnderlineWidth}, oldUnderlineWidth = {underline.width}, text = {label.text}, fontSize = {label.fontSize}, labelWidth = {label.lineWidth}");
        underline.width = (int)Math.Round(newUnderlineWidth);
        var headerController = SingletonMonoBehaviour<HeaderController>.Instance;
        headerController.campaignIcons.SetIconPosition(headerController.viewManager.CurrentViewId, newUnderlineWidth);
    }
}

/**
 * "Special" battles have a mode indicator that the HP can overlap with, so we scale the HP text down.
 */
[HarmonyPatch(typeof(PartsBossGauge), nameof(PartsBossGauge.InitGauge))]
[HarmonyWrapSafe]
public class BossGaugePatch
{
    public static void Postfix(PartsBossGauge __instance)
    {
        if (__instance.battleManager.IsSpecialBattle)
        {
            __instance.hpLabel.fontSize = 16;
        }
    }
}

[HarmonyPatch(typeof(PartsDialogShopMemoryPieceDealConfirm),
    nameof(PartsDialogShopMemoryPieceDealConfirm.InitializeParam))]
[HarmonyWrapSafe]
public class MemoryPieceDealConfirmPatch
{
    public static void Postfix(PartsDialogShopMemoryPieceDealConfirm __instance)
    {
        __instance.alertLabel.overflowMethod = UILabel.Overflow.ResizeFreely;
        for (var i = 0; i < __instance.alertObject.transform.childCount; i++)
        {
            var child = __instance.alertObject.transform.GetChild(i);
            if (child.name != "common_icon_alert")
            {
                continue;
            }

            child.gameObject.SetActive(false);
        }
    }
}

[HarmonyPatch]
[HarmonyWrapSafe]
public class UnitIconPatch
{
    static IEnumerable<MethodBase> TargetMethods()
   {
       yield return AccessTools.Method(typeof(UnitIcon), nameof(UnitIcon.updateTable));
   }
    
    public static void Postfix(UnitIcon __instance)
    {
        if (__instance.statusValueLabel == null)
        {
            return;
        }
        __instance.statusNameLabel.fontSize = 18;
        __instance.statusValueLabel.fontSize = 18;

        while (true)
        {
            var nameSize = __instance.statusNameLabel.mCalculatedSize;
            var valueSize = __instance.statusValueLabel.mCalculatedSize;
            var newValueX = nameSize.x;
            if (newValueX + valueSize.x > 122)
            {
                __instance.statusNameLabel.fontSize -= 1;
                __instance.statusValueLabel.fontSize -= 1;
                continue;
            }
            __instance.statusValueLabel.SetLocalPosX(newValueX);
            break;
        }
    }
}

[HarmonyPatch(typeof(PartsProfileCard), nameof(PartsProfileCard.Initialize))]
[HarmonyWrapSafe]
public class ProfileCardPatch
{
    public static void Postfix(PartsProfileCard __instance)
    {
        var labelGo =
            GameObject.Find(
                    "_Game(Clone)/UI Root/ViewsArea/View/ViewMyProfileCard(Clone)/TopProfileImage/MyProfileCard/ProfileLayer/ProfileNode/Profile/Tower/ReachingFloor/common_dt_bg_pink/Label");
        if (!labelGo)
        {
            Log.Debug("Profile tower label GO not found");
            return;
        }
        
        var label = labelGo.GetComponent<CustomUILabel>();
        if (!label)
        {
            Log.Debug("Profile tower label not found");
            return;
        }

        label.lineWidth = 180;
        var pos = labelGo.transform.localPosition;
        pos.x = 3.4f;
        labelGo.transform.localPosition = pos;
        Log.Debug("Fixed profile tower label");
    }
}
[HarmonyPatch(typeof(PartsGoldShopPlate), nameof(PartsGoldShopPlate.SetUseJewel))]
[HarmonyWrapSafe]
public class GoldShopPlatePatch
{
    public static void Postfix(PartsGoldShopPlate __instance)
    {
        var label = __instance.useJewelTypeLabel;
        label.overflowMethod = UILabel.Overflow.ResizeFreely;
    }
}

[HarmonyPatch(typeof(PartsClanBattleRankingSelf), nameof(PartsClanBattleRankingSelf.SetData))]
[HarmonyWrapSafe]
public class ClanBattleRankingPatch
{
    public static void Postfix(PartsClanBattleRankingSelf __instance)
    {
        var label = __instance.clanTotalDamage;
        label.lineWidth = 180;
    }
}

//PartsEquipmentDetail.SetItemId
[HarmonyPatch(typeof(PartsEquipmentDetail), nameof(PartsEquipmentDetail.setInfoStatusAndUI))]
[HarmonyWrapSafe]
public class EquipmentDetailPatch
{
    public static void Postfix(PartsEquipmentDetail __instance)
    {
        var labelGos = __instance.gameObject.transform.GetComponentsInChildren<CustomUILabel>();
        if (labelGos == null)
        {
            return;
        }

        var labelArr = labelGos.ToArray();
        for (var i = 0; i < labelArr.Length; i++)
        {
            var label = labelArr[i];
            if (label == null)
            {
                continue;
            }
            
            if (label.curTextId == eTextId.EQUIP_STATUS_LABEL || label.text == "Equipment Stats")
            {
                label.overflowMethod = UILabel.Overflow.ResizeFreely;
            }
        }
    }
}