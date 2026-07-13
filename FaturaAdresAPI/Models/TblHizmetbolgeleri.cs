using System;
using System.Collections.Generic;

namespace FaturaAdresAPI.Models;

public partial class TblHizmetbolgeleri
{
    public int Id { get; set; }

    public string? IlceAdi { get; set; }

    public int? SubeId { get; set; }

    public virtual TblSubeler? Sube { get; set; }
}
