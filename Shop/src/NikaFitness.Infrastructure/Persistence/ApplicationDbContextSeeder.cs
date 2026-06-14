using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NikaFitness.Domain.Catalog;
using NikaFitness.Domain.Settings;
using NikaFitness.Domain.ValueObjects;
using NikaFitness.Infrastructure.Persistence;

namespace NikaFitness.Infrastructure.Persistence;

/// <summary>
/// Applies migrations and seeds a starter Nika Fitness catalogue on startup. Idempotent:
/// it only seeds when the catalogue is empty.
/// </summary>
public sealed class ApplicationDbContextSeeder(
    ApplicationDbContext db,
    ILogger<ApplicationDbContextSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.CompanySettings.AnyAsync(cancellationToken))
        {
            db.CompanySettings.Add(CompanySettings.CreateDefault());
            await db.SaveChangesAsync(cancellationToken);
        }

        if (await db.Categories.AnyAsync(cancellationToken))
            return;

        logger.LogInformation("Seeding starter catalogue...");

        var apparel = new Category("Apparel", "Gym clothes engineered for training.");
        var accessories = new Category("Accessories", "Gear that completes your kit.");
        var footwear = new Category("Footwear", "Trainers built for the grind.");

        db.Categories.AddRange(apparel, accessories, footwear);

        // Real fitness/gym product photos (Unsplash, no API key needed).
        static string Img(string id) => $"https://images.unsplash.com/photo-{id}?w=800&h=800&fit=crop&q=80";

        db.Products.AddRange(
            // --- Apparel (gym clothes) ---
            BuildProduct(
                "Nika Performance Training Tee",
                "Breathable, moisture-wicking tee built for high-intensity sessions.",
                apparel.Id,
                [Img("1521572163474-6864f9cf17ab"), Img("1503342217505-b0a15ec3261c")],
                [
                    ("TEE-BLU-S", "Royal Blue / S", 2500m, 25),
                    ("TEE-BLU-M", "Royal Blue / M", 2500m, 30),
                    ("TEE-BLU-L", "Royal Blue / L", 2500m, 20)
                ]),
            BuildProduct(
                "Nika Compression Leggings",
                "Four-way stretch compression leggings with a hidden waistband pocket.",
                apparel.Id,
                [Img("1571019613454-1cb2f99b2d8b"), Img("1506629082955-511b1aa562c8")],
                [
                    ("LEG-BLK-S", "Black / S", 3500m, 18),
                    ("LEG-BLK-M", "Black / M", 3500m, 22),
                    ("LEG-BLK-L", "Black / L", 3500m, 15)
                ]),
            BuildProduct(
                "Nika Pro Training Shorts",
                "Lightweight 7-inch shorts with zip pockets and a secure liner.",
                apparel.Id,
                [Img("1483721310020-03333e577078")],
                [
                    ("SHR-NVY-S", "Navy / S", 2200m, 20),
                    ("SHR-NVY-M", "Navy / M", 2200m, 26),
                    ("SHR-NVY-L", "Navy / L", 2200m, 19)
                ]),
            BuildProduct(
                "Nika Tech Fleece Hoodie",
                "Brushed-back fleece hoodie that layers for warm-ups and rest days.",
                apparel.Id,
                [Img("1556821840-3a63f95609a7"), Img("1620799140408-edc6dcb6d633")],
                [
                    ("HOD-BLU-S", "Blue / S", 4800m, 12),
                    ("HOD-BLU-M", "Blue / M", 4800m, 16),
                    ("HOD-BLU-L", "Blue / L", 4800m, 10)
                ]),
            BuildProduct(
                "Nika Seamless Sports Bra",
                "Medium-support seamless bra with moisture-wicking ribbed knit.",
                apparel.Id,
                [Img("1518310383802-640c2de311b2")],
                [
                    ("BRA-BLU-S", "Sky Blue / S", 2300m, 24),
                    ("BRA-BLU-M", "Sky Blue / M", 2300m, 24),
                    ("BRA-BLU-L", "Sky Blue / L", 2300m, 18)
                ]),

            // --- Accessories ---
            BuildProduct(
                "Nika Lifting Gloves",
                "Padded grip gloves with wrist support for heavy pulls and presses.",
                accessories.Id,
                [Img("1583454110551-21f2fa2afe61")],
                [
                    ("GLV-S", "Small", 1500m, 30),
                    ("GLV-M", "Medium", 1500m, 35),
                    ("GLV-L", "Large", 1500m, 28)
                ]),
            BuildProduct(
                "Nika Resistance Band Set",
                "Five stackable bands from light to extra-heavy with a carry pouch.",
                accessories.Id,
                [Img("1638536532686-d610adfc8e5c")],
                [
                    ("BND-SET", "5-Band Set", 1800m, 40)
                ]),
            BuildProduct(
                "Nika Gym Duffel Bag",
                "35L duffel with a ventilated shoe compartment and wet pocket.",
                accessories.Id,
                [Img("1547949003-9792a18a2601"), Img("1553062407-98eeb64c6a62")],
                [
                    ("BAG-NVY", "Navy / 35L", 3900m, 22)
                ]),
            BuildProduct(
                "Nika Insulated Water Bottle 750ml",
                "Double-wall stainless bottle that keeps drinks cold for 24 hours.",
                accessories.Id,
                [Img("1602143407151-7111542de6e8")],
                [
                    ("BTL-BLU-750", "Blue / 750ml", 1200m, 50)
                ]),

            // --- Footwear ---
            BuildProduct(
                "Nika Trainer Pro",
                "Stable, flat-sole trainer for lifting, HIIT and everyday gym days.",
                footwear.Id,
                [Img("1542291026-7eec264c27ff"), Img("1460353581641-37baddab0fa2")],
                [
                    ("SHO-UK8", "UK 8", 6500m, 14),
                    ("SHO-UK9", "UK 9", 6500m, 16),
                    ("SHO-UK10", "UK 10", 6500m, 11)
                ]));

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Starter catalogue seeded.");
    }

    private static Product BuildProduct(
        string name,
        string description,
        Guid categoryId,
        string[] images,
        (string Sku, string Name, decimal Price, int Stock)[] variants)
    {
        var product = new Product(name, description, categoryId);
        foreach (var image in images)
            product.AddImage(image);
        foreach (var v in variants)
            product.AddVariant(v.Sku, v.Name, new Money(v.Price), v.Stock);
        product.Publish();
        return product;
    }
}
