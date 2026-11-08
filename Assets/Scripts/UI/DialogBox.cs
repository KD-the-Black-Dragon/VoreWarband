// Modified for the VoreWar mercenary-company conversion.
// 2026-11-08: Place confirmation dialogs above campaign, save-browser and other active UI canvases (40E UI follow-up).
// 2026-11-03: Use the shared blue popup palette without changing dialog actions (spec 17).
// See CHANGES.md for the fork change record and License.txt for GPL-3.0 terms.
using System;
using UnityEngine;
using UnityEngine.UI;

public class DialogBox : MonoBehaviour
{
    const int ModalSortingOrder = 2000;

    private void Awake()
    {
        UITheme.StylePopup(gameObject);
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = ModalSortingOrder;
        }
    }

    Action YesAction;
    Action NoAction;
    public Button Yes;
    public Button No;
    public Text Text;

    public void SetData(Action action, string yesText, string noText, string mainText, Action noAction = null)
    {
        YesAction = action;
        Yes.GetComponentInChildren<Text>().text = yesText;
        No.GetComponentInChildren<Text>().text = noText;
        Text.text = mainText;
        NoAction = noAction;
    }

    public void YesClicked()
    {
        YesAction?.Invoke();
        Destroy(gameObject);
    }

    public void NoClicked()
    {
        NoAction?.Invoke();
        Destroy(gameObject);
    }

}
