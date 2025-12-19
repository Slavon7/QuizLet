using UnityEngine;

namespace SimpleUIBlur
{
    [HelpURL("https://assetstore.unity.com/packages/slug/225428")]
    public class BlurController : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] blurImages;

        /// <summary>
        /// Enabling and disabling background blur
        /// </summary>
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Space))
                return;
            for (int i = 0; i < blurImages.Length; i++)
                blurImages[i].SetActive(!blurImages[i].activeSelf);
        }
    }
}