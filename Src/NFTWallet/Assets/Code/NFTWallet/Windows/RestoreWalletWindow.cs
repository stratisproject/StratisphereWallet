using System;
using Cysharp.Threading.Tasks;
using NBitcoin;
using UnityEngine.UI;

public class RestoreWalletWindow : WindowBase
{
    public Button BackButton, RestoreWalletButton;

    public InputField MnemonicInputField;

    void Awake()
    {
        BackButton.onClick.AddListener(async () =>
        {
            await NFTWalletWindowManager.Instance.WelcomeWindow.ShowAsync();
        });

        RestoreWalletButton.onClick.AddListener(async () =>
        {
            await this.RestoreWallet();
        });
    }

    private async UniTask RestoreWallet()
    {
        string mnemonic = MnemonicInputField.text;

        // Validate mnemonic
        try
        {
            Wordlist wordlist = Wordlist.AutoDetect(mnemonic);
            new Mnemonic(mnemonic, wordlist);
        }
        catch (Exception e)
        {
            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync(e.Message, "INCORRECT MNEMONIC");
            return;
        }
        MnemonicInputField.text = string.Empty;


        NFTWallet.Instance.SaveMnemonic(mnemonic);

        bool success = await NFTWallet.Instance.InitializeAsync(mnemonic, NFTWallet.Instance.DefaultNetwork);

        if (success)
            await NFTWalletWindowManager.Instance.WalletWindow.ShowAsync();
        else
            await NFTWalletWindowManager.Instance.PopupWindow.ShowPopupAsync("Can't initialize NFT wallet. Probably can't reach API server.", "INITIALIZATION ERROR");

        await NFTWallet.Instance.AddKnownContractsIfMissingAsync();
    }
}
