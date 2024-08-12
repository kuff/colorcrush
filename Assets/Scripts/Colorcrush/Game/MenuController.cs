using UnityEngine;
using UnityEngine.UI;

namespace Colorcrush.Game
{
    public class MenuController : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollViewToReset;

        private void Awake()
        {
            ResetScrollViewToBeginning();
        }

        private void ResetScrollViewToBeginning()
        {
            if (scrollViewToReset != null)
            {
                // Reset the horizontal scroll position to 0 (beginning)
                scrollViewToReset.horizontalNormalizedPosition = 0f;
                
                // Force the scroll view to update immediately
                Canvas.ForceUpdateCanvases();
                //scrollViewToReset.content.anchoredPosition = Vector2.zero;
                scrollViewToReset.velocity = Vector2.zero;
            }
            else
            {
                Debug.LogWarning("ScrollRect to reset is not assigned in the inspector.");
            }
        }
    }
}
