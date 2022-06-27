using UnityEngine.UI;

public class WelcomeWindow : WindowBase
{
    public Button CreateWalletButton, RestoreWalletButton;

    void Awake()
    {
        CreateWalletButton.onClick.AddListener(async () =>
        {
            NFTWalletWindowManager.Instance.DisplayMnemonicWindow.GenerateAndSetNewMnemonic();

            await NFTWalletWindowManager.Instance.DisplayMnemonicWindow.ShowAsync();
        });

        RestoreWalletButton.onClick.AddListener(async () =>
        {
            await NFTWalletWindowManager.Instance.RestoreWalletWindow.ShowAsync();
        });
    }
}
