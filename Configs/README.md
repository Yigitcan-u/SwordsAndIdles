# Configs — Luban yapılandırma alanı

Bu klasör `Assets` dışında duruyor; Unity bu Excel'leri import etmesin diye kasıtlı.

## Klasör düzeni

```
Configs/
├─ luban.conf        # şema dosyaları, gruplar, hedefler
├─ gen.bat           # kod + veri üretimi
├─ Defines/          # (opsiyonel) XML şema tanımları
└─ Data/
   ├─ __tables__.xlsx   # tablo kaydı — her yeni tablo buraya bir satır
   ├─ __beans__.xlsx    # elle bean tanımları (şimdilik boş)
   ├─ __enums__.xlsx    # enum tanımları
   └─ item.xlsx         # örnek veri tablosu
```

Çıktılar:

| Ne | Nereye |
|---|---|
| Üretilen C# | `Assets/_Project/Scripts/Config/Generated` |
| Üretilen JSON | `Assets/StreamingAssets/Config` |

> **Uyarı:** Luban, `outputCodeDir` içindeki *tüm* dosyaları silip yeniden yazar.
> O klasöre asla elle kod koyma.

## Günlük akış

1. `Data/` içindeki bir Excel'i düzenle
2. `gen.bat` çalıştır
3. Unity'ye dön, derlemeyi bekle

## Yeni tablo ekleme

1. `Data/` altına yeni bir `.xlsx` aç. İlk üç satır şema:

   | | id | name | price |
   |---|---|---|---|
   | `##var` | id | name | price |
   | `##type` | int | string | int |
   | `##` | ID | İsim | Fiyat |
   | | 1 | Kılıç | 100 |

   A sütunu işaret sütunudur — veri satırlarında boş bırak.

2. `__tables__.xlsx`'e bir satır ekle:
   - `full_name`: `item.TbItem` (modül.TbTabloAdı)
   - `value_type`: `Item` (kayıt sınıfı adı)
   - `read_schema_from_file`: `true` (şema Excel başlığından okunsun)
   - `input`: `item.xlsx`

3. `gen.bat`

## Konvansiyonlar

- Tablo adları `TbXxxYyy`, alan adları `xx_yy_zz` (Luban C#'ta otomatik `XxYyZz` yapar)
- Modül başına ayrı klasör/dosya — `item`, `hero`, `skill` gibi
- Geliştirme sırasında `-d json` (okunabilir, diff'lenebilir). Sürüm alırken `-c cs-bin -d bin`'e geç.

## Üretilen dosyaları commit'lemeli miyim?

Evet. `gen` çalıştırmayı unutan biri (veya CI) bayat veriyle derlemesin diye
üretilen kodu ve JSON'u repoya dahil et.
