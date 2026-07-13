using FaturaAdresAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

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
            //Önce dosyanın geçerli olup olmadığını kontrol et eğer geçersiz bir dosya ise uyarı verecek.
     
            if (dosya == null || dosya.Length == 0)
            {
                return BadRequest("Lütfen geçerli bir dosya seçiniz.");
            }
            // yüklenen içeriği RAM'de tutabilmemiz için. 
            string TumMetin = "";
            using (var stream = new MemoryStream())
            {
                await dosya.CopyToAsync(stream);
                
                var StreamByte = stream.ToArray();
                
                using var belge = PdfDocument.Open(StreamByte) ;
                
                foreach(Page sayfa in belge.GetPages())
                {
                    TumMetin += sayfa.Text;
                }
          
            }
            //veritabanındaki ilçeleri çekiyoruz
            var IlcelerListesi = _context.TblIlcelers.ToList();

            string BulunanIlce = "Adres Bulunamadı"; // Başlangıçta boş kalmasın, bulamazsa bu yazsın
            TumMetin = TumMetin.ToUpper(); // Döngüden önce bir kere büyüttüm

            foreach (var i in IlcelerListesi)
            {
                if (i.IlceAdi != null)
                {
                    // \b kelimenin sınırlarını belirler. C#'ta \ ve $ işaretlerini beraber kullanmak için $@ ile başlarız.
                    bool IlceVarMi = Regex.IsMatch(TumMetin, $@"\b{i.IlceAdi.ToUpper()}\b");

                    if (IlceVarMi == true)
                    {
                        BulunanIlce = i.IlceAdi;
                        break;
                    }
                }
            }
            // 1. Önce bulduğumuz ilçeye hangi şubenin baktığını TBL_HIZMETBOLGELERI tablosundan soruyoruz.
            
            var hizmetBolgesi = _context.TblHizmetbolgeleris.FirstOrDefault(h => h.IlceAdi != null && h.IlceAdi.ToUpper() == BulunanIlce);

            // Eğer eşleşen bir bölge bulursak onun SubeId'sini alacağız, bulamazsak boş (null) bırakacağız.
            int? atanacakSubeId = null;
            if (hizmetBolgesi != null)
            {
                atanacakSubeId = hizmetBolgesi.SubeId;
            }

            //  Şimdi Fatura tablomuz (TblFaturalar) için yeni kaydımızı oluşturuyoruz
            //  Önce İli de bul (İlçe ID'si üzerinden)
            var ilceBilgisi = _context.TblIlcelers.FirstOrDefault(i => i.IlceAdi != null && i.IlceAdi.ToUpper() == BulunanIlce);
            var ilBilgisi = _context.TblIllers.FirstOrDefault(i => ilceBilgisi != null && i.Id == ilceBilgisi.IlId);

            // 2. Şubeyi buluyoruz
            
            int? bulunanSubeId = hizmetBolgesi?.SubeId;

            // 3. kayıtı oluşturma
            var yeniKayit = new TblFaturalar
            {
                FaturaNo = dosya.FileName,
                AdresMetni = $"{BulunanIlce}/{ilBilgisi.IlAdi}", // Örnek: ESENYURT/İSTANBUL
                AtananSubeId = bulunanSubeId,
                KayitTarihi = DateTime.Now
            };

            _context.TblFaturalars.Add(yeniKayit);
            _context.SaveChanges();

            return Ok(new
            {
                FaturaAdi = dosya.FileName,
                TespitEdilenAdres = $"{BulunanIlce}/{ilBilgisi.IlAdi}",
                AtananSube = bulunanSubeId,
                Durum = "Başarılı"
            });

        }
    }

}
