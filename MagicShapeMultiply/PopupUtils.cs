using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MagicShapeMultiply
{
    public static class PopupUtils
    {
        public static GameObject popup;
        public static GameObject paramsPopup;

        public static void Show(string message, bool skipAnim = false, Action onClose = null)
        {
            if (popup == null)
                return;
            popup.transform.SetParent(null,  false);
            popup.SetActive(true);
            scnEditor.instance.ShowPopup(true, (scnEditor.PopupType)100, skipAnim);
            popup.transform.SetParent(scnEditor.instance.popupWindow.transform, false);
            popup.transform.Find("popupText").GetComponent<TMP_Text>().text = message;
            Button button = popup.transform.Find("buttonOk").GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Hide());
            if (onClose != null)
                button.onClick.AddListener(new UnityAction(onClose));
        }

        public static void ShowParams(string title, string[] @params, bool skipAnim = false, Action onClose = null)
        {
            if (paramsPopup == null)
                return;
            paramsPopup.transform.SetParent(null, false);
            paramsPopup.SetActive(true);
            scnEditor.instance.ShowPopup(true, (scnEditor.PopupType)100, skipAnim);
            paramsPopup.transform.SetParent(scnEditor.instance.popupWindow.transform, false);
            scnEditor.instance.popupWindow.GetComponent<RectTransform>().SetAnchorPosY(450);
            paramsPopup.transform.Find("title").GetComponent<TMP_Text>().text = title;
            paramsPopup.transform.Find("params").GetComponent<TMP_Text>().text = string.Join("", @params.Select(param => "- " + param + "\n"));
            Button button = paramsPopup.transform.Find("buttonOK").GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => {
                Hide();
                (DOTween.TweensByTarget(scnEditor.instance.popupWindow.GetComponent<RectTransform>())[0] as TweenerCore<Vector2, Vector2, VectorOptions>).endValue = new Vector2(0, 450);
            });
            if (onClose != null)
                button.onClick.AddListener(new UnityAction(onClose));
        }

        public static void Hide(bool skipAnim = false)
        {
            scnEditor.instance.ShowPopup(false, (scnEditor.PopupType)100, skipAnim);
        }
    }
}
