using System;
using System.Collections.Generic;

namespace FaturaAdresAPI.Models;

public partial class TblIlceler
{
    public int Id { get; set; }

    public string? YedekAdi { get; set; }

    public int IlId { get; set; }

    public string? IlceAdi { get; set; }

    public string? IlceKodu { get; set; }

    public string? PAdres { get; set; }

    public virtual TblIller Il { get; set; } = null!;
}
