using System;
using cfg;
using Luban.SimpleJSON;
using UnityEngine;

namespace SwordsAndIdles.Config
{
    /// <summary>
    /// Luban ile üretilen config tablolarına tek giriş noktası.
    /// Veri <c>Assets/_Project/Resources/Config</c> altından okunur; bu klasörün
    /// içeriğini elle düzenleme, <c>Configs/gen.bat</c> her çalıştığında yeniden yazılır.
    /// </summary>
    public static class ConfigManager
    {
        /// <summary>Resources içindeki config klasörü (uzantısız, Resources'a göreli).</summary>
        private const string ResourceRoot = "Config";

        private static Tables _tables;

        /// <summary>Tablolar belleğe alındı mı.</summary>
        public static bool IsLoaded => _tables != null;

        /// <summary>
        /// Tüm config tabloları. İlk erişimde yüklenir, sonrasında bellekten döner.
        /// </summary>
        public static Tables Tables => _tables ??= new Tables(LoadJson);

        /// <summary>
        /// Yüklemeyi öne çeker. Oyunun ilk karesinde takılma olmasın diye
        /// açılışta bir kez çağrılır; davranışı <see cref="Tables"/> ile aynıdır.
        /// </summary>
        public static void Preload() => _ = Tables;

        /// <summary>
        /// Tabloları bellekten atar; bir sonraki erişimde diskten yeniden okunur.
        /// gen.bat çalıştırdıktan sonra Play mode'u yeniden başlatmadan tazelemek için.
        /// </summary>
        public static void Reload() => _tables = null;

        private static JSONNode LoadJson(string tableName)
        {
            var path = $"{ResourceRoot}/{tableName}";
            var asset = Resources.Load<TextAsset>(path);

            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Config tablosu bulunamadı: Resources/{path}.json — " +
                    "Configs/gen.bat çalıştırıldı mı?");
            }

            try
            {
                return JSONNode.Parse(asset.text);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Config tablosu okunamadı: Resources/{path}.json", e);
            }
            finally
            {
                // Ham metin artık gerekmiyor; parse edilmiş hali bellekte tutuluyor.
                Resources.UnloadAsset(asset);
            }
        }
    }
}
