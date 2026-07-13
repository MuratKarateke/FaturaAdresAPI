using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FaturaAdresAPI.Models;

public partial class FaturaYonetimDbContext : DbContext
{
    public FaturaYonetimDbContext()
    {
    }

    public FaturaYonetimDbContext(DbContextOptions<FaturaYonetimDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblFaturalar> TblFaturalars { get; set; }

    public virtual DbSet<TblHizmetbolgeleri> TblHizmetbolgeleris { get; set; }

    public virtual DbSet<TblIlceler> TblIlcelers { get; set; }

    public virtual DbSet<TblIller> TblIllers { get; set; }

    public virtual DbSet<TblSubeler> TblSubelers { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblFaturalar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TBL_FATU__3214EC0781D660D1");

            entity.ToTable("TBL_FATURALAR");

            entity.Property(e => e.AdresMetni).HasMaxLength(100);
            entity.Property(e => e.FaturaNo).HasMaxLength(100);
            entity.Property(e => e.KayitTarihi)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.AtananSube).WithMany(p => p.TblFaturalars)
                .HasForeignKey(d => d.AtananSubeId)
                .HasConstraintName("FK__TBL_FATUR__Atana__4F7CD00D");
        });

        modelBuilder.Entity<TblHizmetbolgeleri>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TBL_HIZM__3214EC07EDA11043");

            entity.ToTable("TBL_HIZMETBOLGELERI");

            entity.Property(e => e.IlceAdi).HasMaxLength(100);

            entity.HasOne(d => d.Sube).WithMany(p => p.TblHizmetbolgeleris)
                .HasForeignKey(d => d.SubeId)
                .HasConstraintName("FK__TBL_HIZME__SubeI__4CA06362");
        });

        modelBuilder.Entity<TblIlceler>(entity =>
        {
            entity.ToTable("TBL_ILCELER");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.IlId).HasColumnName("IL_ID");
            entity.Property(e => e.IlceAdi).HasColumnName("ILCE_ADI");
            entity.Property(e => e.IlceKodu).HasColumnName("ILCE_KODU");
            entity.Property(e => e.PAdres).HasColumnName("P_ADRES");
            entity.Property(e => e.YedekAdi).HasColumnName("YEDEK_ADI");

            entity.HasOne(d => d.Il).WithMany(p => p.TblIlcelers).HasForeignKey(d => d.IlId);
        });

        modelBuilder.Entity<TblIller>(entity =>
        {
            entity.ToTable("TBL_ILLER");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.IlAdi).HasColumnName("IL_ADI");
            entity.Property(e => e.IlKodu).HasColumnName("IL_KODU");
            entity.Property(e => e.UlkeKodu).HasColumnName("ULKE_KODU");
        });

        modelBuilder.Entity<TblSubeler>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TBL_SUBE__3214EC079435E36F");

            entity.ToTable("TBL_SUBELER");

            entity.Property(e => e.SubeAdi).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
