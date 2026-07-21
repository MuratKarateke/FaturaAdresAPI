using FaturaAdresAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;
using System.Globalization;
using System.Linq;

namespace FaturaAdresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaturaController : ControllerBase
    {
        private readonly FaturaYonetimDbContext _context;

        public FaturaController(FaturaYonetimDbContext context)
        {
            _context = context;
        }

        [HttpPost("yukle")]
        public async Task<IActionResult> FaturaYukle(IFormFile dosya)
        {
            if (dosya == null || dosya.Length == 0)
                return BadRequest("Lütfen geçerli bir dosya seçiniz.");

            // PDF'den metin çıkarma
            string TumMetin = "";
            using (var stream = new MemoryStream())
            {
                await dosya.CopyToAsync(stream);
                var StreamByte = stream.ToArray();
                using var belge = PdfDocument.Open(StreamByte);
                foreach (Page sayfa in belge.GetPages())
                {
                    TumMetin += sayfa.Text + " ";
                }
            }

            // Veritabanındaki ilçe kayıtlarını al (alternatif adlar dahil)
            var ilceler = _context.TblIlcelers.ToList();

            // Normalize edilmiş metin: büyük harf, diakritik kaldır, fazla boşluk temizleme
            string metinNorm = NormalizeForSearch(TumMetin);

            // Metinden alınabilecek kelime tokenları (sadece harf dizileri)
            var metinTokens = Regex.Matches(metinNorm, @"\p{L}+")
                                   .Cast<Match>()
                                   .Select(m => m.Value)
                                   .ToArray();

            string bulunanIlce = null;
            double bestScore = 0;

            // Her ilçe için alias (ana ad + yedek ad + pAdres gibi) listesi oluştur
            foreach (var ilce in ilceler)
            {
                if (string.IsNullOrWhiteSpace(ilce.IlceAdi))
                    continue;

                var aliases = new List<string>();
                aliases.Add(ilce.IlceAdi);
                if (!string.IsNullOrWhiteSpace(ilce.YedekAdi)) aliases.Add(ilce.YedekAdi);
                if (!string.IsNullOrWhiteSpace(ilce.PAdres)) aliases.Add(ilce.PAdres);

                // Normalize edilmiş alias listesi
                var aliasesNorm = aliases.Select(a => NormalizeForSearch(a)).Distinct().ToArray();

                // 1) Tam substring kontrolü (en güçlü sinyal)
                foreach (var a in aliasesNorm)
                {
                    if (string.IsNullOrWhiteSpace(a)) continue;
                    if (metinNorm.Contains(a))
                    {
                        // çok yüksek skor ver ve seç
                        double score = 100;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bulunanIlce = ilce.IlceAdi;
                        }
                    }
                }

                // 2) N-gram / token-sequence karşılaştırması: çok kelimeli ilçe adları için
                foreach (var a in aliasesNorm)
                {
                    if (string.IsNullOrWhiteSpace(a)) continue;
                    var aliasTokens = Regex.Matches(a, @"\p{L}+")
                                           .Cast<Match>()
                                           .Select(m => m.Value)
                                           .ToArray();
                    if (aliasTokens.Length == 0) continue;

                    // sliding window over metinTokens with same length as aliasTokens (and +/-1)
                    for (int window = Math.Max(1, aliasTokens.Length - 1); window <= aliasTokens.Length + 1; window++)
                    {
                        if (window > metinTokens.Length) break;
                        for (int start = 0; start + window <= metinTokens.Length; start++)
                        {
                            var slice = metinTokens.Skip(start).Take(window).ToArray();
                            var sliceJoined = string.Join(" ", slice);
                            // score calculation
                            double score = ComputeAliasScore(a, sliceJoined);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bulunanIlce = ilce.IlceAdi;
                            }
                        }
                    }
                }

                // 3) Token bazlı yakın eşleşme: her alias ile tek token karşılaştırmaları
                foreach (var a in aliasesNorm)
                {
                    if (string.IsNullOrWhiteSpace(a)) continue;
                    foreach (var t in metinTokens)
                    {
                        double score = ComputeAliasScore(a, t);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bulunanIlce = ilce.IlceAdi;
                        }
                    }
                }
            }

            // Kabul eşiği: yeterince iyi skor yoksa "Adres Bulunamadı"
            if (bestScore < 70)
                bulunanIlce = "Adres Bulunamadı";

            // Hizmet bölgesi, ilçe ve il bilgilerini güvenli şekilde al
            // EF Core string.Equals(..., StringComparison) çevirmez -> ToUpper() ile karşılaştırma yapıyoruz.
            var bulunanIlceKey = (bulunanIlce ?? "").Trim().ToUpper();
            var hizmetBolgesi = _context.TblHizmetbolgeleris
                .FirstOrDefault(h => !string.IsNullOrWhiteSpace(h.IlceAdi) && h.IlceAdi.Trim().ToUpper() == bulunanIlceKey);

            int? atanacakSubeId = hizmetBolgesi?.SubeId;

            var ilceBilgisi = _context.TblIlcelers
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.IlceAdi) && x.IlceAdi.Trim().ToUpper() == bulunanIlceKey);

            var ilBilgisi = ilceBilgisi != null
                ? _context.TblIllers.FirstOrDefault(i => i.Id == ilceBilgisi.IlId)
                : null;

            string ilAdi = ilBilgisi?.IlAdi ?? "Bilinmiyor";

            var yeniKayit = new TblFaturalar
            {
                FaturaNo = dosya.FileName,
                AdresMetni = $"{bulunanIlce}/{ilAdi}",
                AtananSubeId = atanacakSubeId,
                KayitTarihi = DateTime.Now
            };

            _context.TblFaturalars.Add(yeniKayit);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                FaturaAdi = dosya.FileName,
                TespitEdilenAdres = $"{bulunanIlce}/{ilAdi}",
                AtananSube = atanacakSubeId,
                Durum = "Başarılı",
                Score = bestScore
            });
        }

        // Skor hesaplama: tam eşleşme, contains, Levenshtein temelli yakınlık
        private static double ComputeAliasScore(string aliasNorm, string candidateNorm)
        {
            if (string.IsNullOrWhiteSpace(aliasNorm) || string.IsNullOrWhiteSpace(candidateNorm))
                return 0;

            // Tam eşleşme
            if (aliasNorm == candidateNorm) return 100;

            // Alias içinde candidate veya tam tersi
            if (aliasNorm.Contains(candidateNorm) || candidateNorm.Contains(aliasNorm))
            {
                return 90;
            }

            // Levenshtein normalleştirilmiş skor: 0..100 (küçük mesafe = yüksek skor)
            int dist = LevenshteinDistance(aliasNorm, candidateNorm);
            int maxLen = Math.Max(aliasNorm.Length, candidateNorm.Length);
            if (maxLen == 0) return 0;
            double normalized = (double)dist / maxLen; // 0 iyi, 1 kötü
            double score = 100 * (1 - normalized);

            // Kısa kelimeler için sertleşme (yanlış pozitifleri azalt)
            if (Math.Min(aliasNorm.Length, candidateNorm.Length) <= 3 && score < 85)
                score -= 15;

            return Math.Max(0, Math.Min(100, score));
        }

        // Normalize: büyük harf, diakritik kaldır, kısa ilçe eklerini temizle, fazla boşlukları azalt
        private static string NormalizeForSearch(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var withoutDiacritics = RemoveDiacritics(input);
            var upper = withoutDiacritics.ToUpperInvariant();

            // Yaygın kısaltma/ekleri temizle (MAH, MH, CAD, SOK, SK vb.)
            // DİKKAT: Burada regex desenindeki kaçışlar geçerli olmalı — '\.' ile nokta escape edilmiştir.
            upper = Regex.Replace(upper, @"\b(MAHALLE(SI)?|MH\.?|MAH\.?|CADDE|CAD\.?|CD\.?|SOKAK|SOK\.?|SK\.?|CAD|KMH|K\.MH)\b", " ");

            // Tekrarlı boşlukları temizle
            var collapsed = Regex.Replace(upper, @"\s+", " ").Trim();
            return collapsed;
        }

        // Diakritik kaldırma (Ş->S, İ->I vb.)
        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // Levenshtein (basit)
        private static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[a.Length, b.Length];
        }
    }
}