using UnityEngine.UI;

public class MarketplaceWindow : WindowBase
{
    public InputField LoginData_InputField;

    public Button Login_Button;

    void Awake()
    {
        this.Login_Button.onClick.AddListener(async delegate
        {
            string loginData = LoginData_InputField.text;

            await MarketplaceIntegration.Instance.LogInToNFTMarketplace(loginData);
        });
    }
}
