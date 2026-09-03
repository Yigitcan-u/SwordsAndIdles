using SwordsAndIdles.Config;
using UnityEngine;

namespace SwordsAndIdles.Core
{
    /// <summary>
    /// Oyunun açılış noktası. Sahneler arası yaşar ve config tablolarını
    /// ilk kare çizilmeden önce belleğe alır.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("Açılışta yüklenen config tablolarının özetini konsola yaz.")]
        [SerializeField] private bool logConfigSummary = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ConfigManager.Preload();

            if (logConfigSummary)
            {
                Debug.Log($"[GameManager] Config yüklendi — " +
                          $"{ConfigManager.Tables.TbItem.DataList.Count} eşya.", this);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
