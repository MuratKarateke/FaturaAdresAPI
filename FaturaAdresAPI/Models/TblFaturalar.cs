using System;
using System.Collections.Generic;

namespace FaturaAdresAPI.Models;

public partial class TblFaturalar
{
    public int Id { get; set; }

    public string? FaturaNo { get; set; }

    public string? AdresMetni { get; set; }

    public int? AtananSubeId { get; set; }

    public DateTime? KayitTarihi { get; set; }

    public virtual TblSubeler? AtananSube { get; set; }
}
