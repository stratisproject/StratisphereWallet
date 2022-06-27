using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class NFTWalletWindowManager : MonoBehaviour
{
    public static NFTWalletWindowManager Instance;

    public LoginWindow LoginWindow; // TODO remove
    public PopupWindow PopupWindow;

    public WalletWindow WalletWindow;
    public MyCollectionWindow MyCollectionWindow;
    public CreateNFTWindow CreateNftWindow;
    public SendWindow SendWindow;
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
            SendWindow, MintWindow, BurnWindow, WaitTransactionWindow, MarketplaceWindow, QRWindow, AnimationWindow,
            WelcomeWindow, RestoreWalletWindow, CheckMnemonicWindow, DisplayMnemonicWindow
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

    public async UniTask HideAllWindowsAsync()
    {
        foreach (WindowBase window in this.allWindows)
        {
            await window.HideAsync();
        }
    }
}
