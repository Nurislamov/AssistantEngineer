using AssistantEngineer.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Data.Configurations;

public class WindowConfiguration : IEntityTypeConfiguration<Window>
{
    public void Configure(EntityTypeBuilder<Window> builder)
    {
        builder.ToTable("Windows");
        builder.HasKey(w => w.Id);

        builder.OwnsOne(w => w.Area, area =>
        {
            area.Property(a => a.SquareMeters).HasColumnName("AreaM2").IsRequired();
        });

        builder.OwnsOne(w => w.UValue, u =>
        {
            u.Property(u => u.Value).HasColumnName("UValue").IsRequired();
        });

        builder.OwnsOne(w => w.Shgc, s =>
        {
            s.Property(s => s.Value).HasColumnName("SHGC").IsRequired();
        });

        builder.OwnsOne(w => w.Shading, shading =>
        {
            shading.Property(s => s.OverhangDepthM).HasColumnName("ShadingOverhangDepthM").IsRequired();
            shading.Property(s => s.SideFinDepthM).HasColumnName("ShadingSideFinDepthM").IsRequired();
            shading.Property(s => s.RevealDepthM).HasColumnName("ShadingRevealDepthM").IsRequired();
            shading.Property(s => s.WindowHeightM).HasColumnName("ShadingWindowHeightM").IsRequired();
            shading.Property(s => s.WindowWidthM).HasColumnName("ShadingWindowWidthM").IsRequired();
            shading.Property(s => s.MinimumDirectSolarReductionFactor)
                .HasColumnName("ShadingMinimumDirectSolarReductionFactor")
                .IsRequired();
            shading.Property(s => s.DiffuseSolarShareUnaffected)
                .HasColumnName("ShadingDiffuseSolarShareUnaffected")
                .IsRequired();
        });

        builder.Property(w => w.Orientation).IsRequired();
        builder.Property(w => w.Orientation).HasConversion<string>();

        builder.HasOne(w => w.Room)
            .WithMany(r => r.Windows)
            .HasForeignKey(w => w.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
