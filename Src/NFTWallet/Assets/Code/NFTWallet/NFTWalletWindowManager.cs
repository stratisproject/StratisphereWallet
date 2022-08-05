using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class NFTWalletWindowManager : MonoBehaviour
{
    public static NFTWalletWindowManager Instance;

    public LoginWindow LoginWindow;
    public PopupWindow PopupWindow;
    public PopupWindowYesNo PopupWindowYesNo;

    public WalletWindow WalletWindow;
    public MyCollectionWindow MyCollectionWindow;
    public CreateNFTWindow CreateNftWindow;
    public MintWindow MintWindow;
    public BurnWindow BurnWindow;
    public WaitTransactionWindow WaitTransactionWindow;
    public MarketplaceWindow MarketplaceWindow;
    public QRWindow QRWindow;
    public DisplayAnimationWIndow AnimationWindow;

    // Onboarding windows
    public WelcomeWindow WelcomeWindow;
    public RestoreWalletWindow RestoreWalletWindow;
    public CheckMnemonicWindow CheckMnemonicWindow;
    public DisplayMnemonicWindow DisplayMnemonicWindow;

    public bool IsMobile;

    private List<WindowBase> allWindows;

    void Awake()
    {
        Instance = this;

        this.allWindows = new List<WindowBase>() { LoginWindow, PopupWindow, WalletWindow, MyCollectionWindow, CreateNftWindow,
            MintWindow, BurnWindow, WaitTransactionWindow, MarketplaceWindow, QRWindow, AnimationWindow,
            WelcomeWindow, RestoreWalletWindow, CheckMnemonicWindow, DisplayMnemonicWindow, PopupWindowYesNo
        };
    }

    async void Start()
    {
        //await this.LoginWindow.InitializeAsync();

        if (NFTWallet.Instance.IsMnemonicSaved())
            await this.LoginWindow.LogInAsync();
        else
            await this.WelcomeWindow.ShowAsync();
    }

    public async UniTask HideAllWindowsAsync(WindowBase windowNotToHide)
    {
        foreach (WindowBase window in this.allWindows.Where(x => x != windowNotToHide))
        {
            await window.HideAsync();
        }
    }
}
