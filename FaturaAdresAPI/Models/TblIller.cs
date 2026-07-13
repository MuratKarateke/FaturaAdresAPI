using System;
using System.Collections.Generic;

namespace FaturaAdresAPI.Models;

public partial class TblIller
{
    public int Id { get; set; }

    public string? UlkeKodu { get; set; }

    public string? IlAdi { get; set; }

    public string? IlKodu { get; set; }

    public virtual ICollection<TblIlceler> TblIlcelers { get; set; } = new List<TblIlceler>();
}
