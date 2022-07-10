using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

public class PopupWindowYesNo : WindowBase
{
    public Button CloseButton, ConfirmButton, CancelButton;
    public Text TitleText, MessageText;

    private Func<Task> onConfirm;

    public async void Awake()
    {
        this.CloseButton.onClick.AddListener(async delegate { await this.HideAsync(); });
        this.CancelButton.onClick.AddListener(async delegate { await this.HideAsync(); });
        this.ConfirmButton.onClick.AddListener(async delegate
        {
            await this.HideAsync();
            await this.onConfirm.Invoke();
        });
    }

    public async UniTask ShowPopupAsync(string message, string title, Func<Task> onConfirm)
    {
        this.MessageText.text = message;
        this.TitleText.text = title;
        this.onConfirm = onConfirm;

        await this.ShowAsync(false);
    }
}
