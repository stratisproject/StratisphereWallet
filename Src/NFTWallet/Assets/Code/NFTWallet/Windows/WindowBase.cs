using Cysharp.Threading.Tasks;
using UnityEngine;

public class WindowBase : MonoBehaviour
{
    public virtual async UniTask ShowAsync(bool hideOtherWindows = true)
    {
        if (hideOtherWindows)
            await NFTWalletWindowManager.Instance.HideAllWindowsAsync(this);

        this.gameObject.SetActive(true);
        this.SetParentState(true);
    }

    public virtual async UniTask HideAsync()
    {
        this.gameObject.SetActive(false);
        this.SetParentState(false);
    }

    private void SetParentState(bool active)
    {
        var parent = this.transform.parent.gameObject;

        if (parent != null && parent.GetComponent<WindowParent>() != null) {
            parent.SetActive(active);
        }
    }
}
