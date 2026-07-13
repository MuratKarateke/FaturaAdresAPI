using System;
using System.Collections.Generic;

namespace FaturaAdresAPI.Models;

public partial class TblSubeler
{
    public int Id { get; set; }

    public string? SubeAdi { get; set; }

    public bool? AktifMİ { get; set; }

    public virtual ICollection<TblFaturalar> TblFaturalars { get; set; } = new List<TblFaturalar>();

    public virtual ICollection<TblHizmetbolgeleri> TblHizmetbolgeleris { get; set; } = new List<TblHizmetbolgeleri>();
}
