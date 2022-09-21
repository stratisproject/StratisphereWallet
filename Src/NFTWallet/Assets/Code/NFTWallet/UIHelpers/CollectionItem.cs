using UnityEngine;
using UnityEngine.UI;

public class CollectionItem : MonoBehaviour
{
    public Text TitleText, DescriptionText;

    public Image NFTImage;

    public Button Send_Button;

    public Button DisplayAnimationButton;

    [HideInInspector]
    public long NFTID;

    [HideInInspector]
    public string ContractAddr;

    [HideInInspector]
    public string NFTUri;

    [HideInInspector]
    public bool ImageLoadedOrAttemptedToLoad = false;
}
