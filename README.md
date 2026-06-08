# zSwax — Güvenlik & Sistem Araçları

Windows için **key korumalı**, koyu temalı tek dosyalık bir sistem aracı. Secure Boot / TPM / Çekirdek Yalıtımı / Windows Defender durumunu yönetir; oyun, gizlilik, ağ ve bakım tweak'leri içerir.

> **Geliştiren:** elifnazpamks · **Discord:** zSwaxx

---

## ⚠️ Uyarı

- Bu araç **güvenlik özelliklerini değiştirir** (Defender, TPM sürücüsü, Çekirdek Yalıtımı vb.). Sadece **kendi bilgisayarında** ve **ne yaptığını bilerek** kullan. Sorumluluk kullanıcıya aittir.
- Windows Defender bu tür araçları çoğu zaman **HackTool/PUA** olarak işaretler (yanlış pozitif). `Defender-Izin-Ver.bat` ile klasörü taramadan hariç tutabilir ya da Windows Güvenliği → Koruma geçmişi → "İzin ver" diyebilirsin.
- Araç hiçbir güvenlik korumasını **gizlice bypass etmez**; örn. Defender'ı kapatmak için Tamper Protection'ı senin elle kapatman gerekir.

## ✨ Özellikler (8 sekme)

| Sekme | Ne yapar |
|---|---|
| **Secure Boot** | Gerçek UEFI durumunu gösterir; tek tıkla UEFI firmware ekranına yeniden başlatır (`shutdown /r /fw`). |
| **TPM** | `Get-Tpm` ile gerçek TPM varlığını okur; Windows TPM sürücüsünü aç/kapat (`Services\Tpm\Start`). |
| **Çekirdek Yalıtımı** | WMI `Win32_DeviceGuard` ile **gerçek çalışan** Bellek Bütünlüğü (HVCI) durumunu gösterir; aç/kapat. |
| **Defender** | `Get-MpComputerStatus` ile canlı durum + Tamper algılama; `Set-MpPreference` ile tam aç/kapat; klasör exclusion. |
| **Oyun** | Game DVR, HAGS, fare ivmesi, görsel efektler (canlı uygulanır). |
| **Gizlilik** | Telemetri (+DiagTrack), reklam kimliği, Cortana, etkinlik geçmişi, öneriler. |
| **Ağ** | DNS temizle/değiştir (Cloudflare/Google/oto), Nagle kapat/geri al, ağ sıfırla. |
| **Bakım** | Sistem geri yükleme noktası, disk temizliği, başlangıç yöneticisi, Explorer restart, tweak'leri kaldır. |

İşlem sonrası isteğe bağlı **log temizliği** (olay günlükleri + geçici dosyalar).

## 🚀 Kullanım

1. `zSwax.exe`'yi çalıştır → UAC "Evet" (yönetici şart).
2. Sekme seç, **erişim anahtarını** gir, işlemi uygula.
3. Erişim anahtarı `key.txt` dosyasından okunur (yoksa varsayılan).

## 🛠 Derleme

Ekstra kurulum gerekmez — Windows'un yerleşik C# derleyicisini kullanır:

```
Derle.bat
```

Çıktı: `zSwax.exe` (.NET Framework 4.x, her Windows 10/11'de çalışır).

## 📁 Dosyalar

| Dosya | Açıklama |
|---|---|
| `zSwax.cs` | Kaynak kod (tek dosya WinForms) |
| `zSwax.exe` | Derlenmiş program |
| `Derle.bat` | Derleyici |
| `Defender-Izin-Ver.bat` | Klasörü Defender'a izinli yapar |
| `app.manifest` / `app.ico` | Yönetici izni + ikon |
| `key.txt` | Erişim anahtarı |

---

🤖 Geliştirme sürecine [Claude Code](https://claude.com/claude-code) yardımcı oldu.
