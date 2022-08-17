using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

public class PopupWindowYesNo : WindowBase
{
    public Button CloseButton, ConfirmButton, CancelButton;
    public Text TitleText, MessageText, ConfirmButtonText, CancelButtonText;

    private Func<Task> onConfirm;
    private Func<Task> onDeny;

    public async void Awake()
    {
        this.ConfirmButton.onClick.AddListener(async delegate
        {
            await this.HideAsync();
            await this.onConfirm?.Invoke();
        });

        this.CloseButton.onClick.AddListener(async delegate
        {
            await this.HideAsync();
        });

        this.CancelButton.onClick.AddListener(async delegate
        {
            await this.HideAsync();
            await this.onDeny?.Invoke();
        });
    }

    public async UniTask ShowPopupAsync(string message, string title, Func<Task> onConfirm)
    {
        await this.ShowPopupAsync(message, title, onConfirm, null);
    }

    public async UniTask ShowPopupAsync(string message, string title,Func<Task> onConfirm, Func<Task> onDeny, string confirmButtonText = "Confirm", string denyButtonText = "Cancel")
    {
        this.MessageText.text = message;
        this.TitleText.text = title;
        this.onConfirm = onConfirm;
        this.onDeny = onDeny;
        this.CancelButtonText.text = denyButtonText;
        this.ConfirmButtonText.text = confirmButtonText;

        await this.ShowAsync(false);
    }
}
