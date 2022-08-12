using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

public class SendNFTPopupWindow : WindowBase
{
    public Button CloseButton, CancelButton, SendButton;

    public Text NftContractAddressText, NftIdText;

    public InputField destinationAddressInputField;

    private Func<string, Task> onConfirm;

    public async void Awake()
    {
        this.CloseButton.onClick.AddListener(async delegate { await this.HideAsync(); });
        this.CancelButton.onClick.AddListener(async delegate { await this.HideAsync(); });
        this.SendButton.onClick.AddListener(async delegate
        {
            await this.HideAsync();
            await this.onConfirm?.Invoke(destinationAddressInputField.text);
        });
    }

    public async UniTask ShowPopupAsync(string contractAddress, long nftId, Func<string, Task> onConfirm)
    {
        this.NftContractAddressText.text = contractAddress;
        this.NftIdText.text = nftId.ToString();
        this.onConfirm = onConfirm;
        this.destinationAddressInputField.text = string.Empty;

        await this.ShowAsync(false);
    }
}
