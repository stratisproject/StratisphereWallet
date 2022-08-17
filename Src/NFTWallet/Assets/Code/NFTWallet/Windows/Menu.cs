using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public Button Wallet_Button, MyCollection_Button, CreateNFT_Button, Send_Button, Mint_Button, Burn_Button, LogOutButton, MarketplaceButton;

    async void Awake()
    {
        Wallet_Button.onClick.AddListener(async delegate { await NFTWalletWindowManager.Instance.WalletWindow.ShowAsync(); });
        MyCollection_Button.onClick.AddListener(async delegate { await NFTWalletWindowManager.Instance.MyCollectionWindow.ShowAsync(); });
        CreateNFT_Button.onClick.AddListener(async delegate { await NFTWalletWindowManager.Instance.CreateNftWindow.ShowAsync(); });
        //Send_Button.onClick.AddListener(async delegate { await NFTWalletWindowManager.Instance.SendWindow.ShowAsync(); });
        Mint_Button.onClick.AddListener(async delegate { await NFTWalletWindowManager.Instance.MintWindow.ShowAsync(); });
        Burn_Button.onClick.AddListener(async delegate { await NFTWalletWindowManager.Instance.BurnWindow.ShowAsync(); });
        LogOutButton.onClick.AddListener(async delegate
        {
            await NFTWalletWindowManager.Instance.PopupWindowYesNo.ShowPopupAsync("Would you like to remove persisted mnemonic from database?", "Delete account?",
                async () =>
                {
                    NFTWallet.Instance.DeleteMnemonic();
                    await NFTWalletWindowManager.Instance.WelcomeWindow.ShowAsync();
                }, async () =>
                {
                    await NFTWalletWindowManager.Instance.WelcomeWindow.ShowAsync();
                }, "Yes, delete", "Just logout");
        });
        MarketplaceButton.onClick.AddListener(async delegate { await NFTWalletWindowManager.Instance.MarketplaceWindow.ShowAsync(); });
    }
}
