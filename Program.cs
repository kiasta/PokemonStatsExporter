using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PokemonStatsExporter
{
    class BaseStats
    {
        public byte Hp { get; set; }
        public byte Atk { get; set; }
        public byte Def { get; set; }
        public byte SpA { get; set; }
        public byte SpD { get; set; }
        public byte Spe { get; set; }
    }

    class PokemonEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public BaseStats Stats { get; set; } = new BaseStats();
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            const string url = "https://pokemondb.net/pokedex/all";
            const string outputPath = "PokemonBaseStatsArray.txt";

            try
            {
                Console.WriteLine("Downloading Pokemon table from pokemondb.net...");
                string html = await DownloadPageAsync(url);

                Console.WriteLine("Parsing table...");
                var pokemonList = ParsePokemonWithNames(html);

                Console.WriteLine($"Successfully parsed {pokemonList.Count - 1} Pokemon.");

                Console.WriteLine("Generating C++ array with names...");
                string cppArray = GenerateCppArrayWithNames(pokemonList);

                await File.WriteAllTextAsync(outputPath, cppArray, Encoding.UTF8);

                Console.WriteLine($"Done! File saved to:\n{Path.GetFullPath(outputPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static async Task<string> DownloadPageAsync(string url)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; PokemonStatsExporter/1.0)");
            return await client.GetStringAsync(url);
        }

        static List<PokemonEntry> ParsePokemonWithNames(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var rows = doc.DocumentNode.SelectNodes("//table[@id='pokedex']/tbody/tr");
            if (rows == null) throw new Exception("Pokedex table not found.");

            var result = new List<PokemonEntry>();

            // Index 0 = placeholder
            result.Add(new PokemonEntry { Id = 0, Name = "(placeholder)", Stats = new BaseStats() });

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells == null || cells.Count < 10) continue;

                // Column 0: National #
                string natIdText = CleanText(cells[0].InnerText);
                if (!int.TryParse(natIdText, out int natId)) continue;

                // Column 1: Name (may contain <small> for forms)
                string fullName = cells[1].SelectSingleNode(".//a")?.InnerText.Trim() ?? "Unknown";
                string formText = cells[1].SelectSingleNode(".//small")?.InnerText.Trim();
                string name = string.IsNullOrEmpty(formText) ? fullName : $"{fullName} {formText}";

                // Stats (columns 4–9)
                if (!byte.TryParse(CleanText(cells[4].InnerText), out byte hp)) continue;
                if (!byte.TryParse(CleanText(cells[5].InnerText), out byte atk)) continue;
                if (!byte.TryParse(CleanText(cells[6].InnerText), out byte def)) continue;
                if (!byte.TryParse(CleanText(cells[7].InnerText), out byte spa)) continue;
                if (!byte.TryParse(CleanText(cells[8].InnerText), out byte spd)) continue;
                if (!byte.TryParse(CleanText(cells[9].InnerText), out byte spe)) continue;

                result.Add(new PokemonEntry
                {
                    Id = natId,
                    Name = CleanPokemonName(name),
                    Stats = new BaseStats { Hp = hp, Atk = atk, Def = def, SpA = spa, SpD = spd, Spe = spe }
                });
            }

            return result;
        }

        static string CleanText(string text) => text.Trim().Replace("\n", "").Replace("\r", "");

        static string CleanPokemonName(string name)
        {
            // Remove unwanted characters and normalize
            return name.Replace("é", "e")           // Pokemon → Pokemon
                       .Replace("’", "'")           // Farfetch’d
                       .Replace(":", " -")          // Type: Null → Type - Null
                       .Replace("  ", " ")
                       .Trim();
        }

        static string GenerateCppArrayWithNames(List<PokemonEntry> pokemonList)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("// Pokemon Base Stats Table (National Dex #1 - #1025)");
            stringBuilder.AppendLine("// Generated from https://pokemondb.net/pokedex/all");
            stringBuilder.AppendLine("// Format: {ID, HP, ATK, DEF, SPA, SPD, SPE}, // ID Name");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE[] = {");

            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                // We ignore alternate forms and make separate lists
                if (!pokemon.Name.Contains("Mega ")
                    && !pokemon.Name.Contains("Alolan")
                    && !pokemon.Name.Contains("Galarian")
                    && !pokemon.Name.Contains("Hisuian")
                    && !pokemon.Name.Contains("Paldean")
                    // Tauros Forms
                    && !pokemon.Name.Contains("Combat Breed")
                    && !pokemon.Name.Contains("Blaze Breed")
                    && !pokemon.Name.Contains("Aqua Breed")
                    // Let's Go Pikachu/Eevee
                    && !pokemon.Name.Contains("Partner")
                    // Castform Forms
                    && !pokemon.Name.Contains("Sunny Form")
                    && !pokemon.Name.Contains("Rainy Form")
                    && !pokemon.Name.Contains("Snowy Form")
                    // Primal Forms (Kyogre and Groudon)
                    && !pokemon.Name.Contains("Primal")
                    // Deoxys Forms
                    && !pokemon.Name.Contains("Deoxys")
                    // Burmy/Wormadam Forms
                    && !pokemon.Name.Contains("Burmy")
                    && !pokemon.Name.Contains("Wormadam")
                    // Rotom Forms
                    && !pokemon.Name.Contains("Heat Rotom")
                    && !pokemon.Name.Contains("Wash Rotom")
                    && !pokemon.Name.Contains("Frost Rotom")
                    && !pokemon.Name.Contains("Fan Rotom")
                    && !pokemon.Name.Contains("Mow Rotom")
                    // Dialga/Palkia/Giratina Forms
                    && !pokemon.Name.Contains("Origin Forme")
                    // Giratina Form
                    && !pokemon.Name.Contains("Altered Forme")
                    // Shaymin Forms
                    && !pokemon.Name.Contains("Shaymin")
                    // Basculin Forms
                    && !pokemon.Name.Contains("Basculin")
                    // Darmanitan Forms
                    && !pokemon.Name.Contains("Darmanitan")
                    // Tornadus/Thundurus/Landorus Forms
                    && !pokemon.Name.Contains("Tornadus")
                    && !pokemon.Name.Contains("Thundurus")
                    && !pokemon.Name.Contains("Landorus")
                    // Kyurem Forms
                    && !pokemon.Name.Contains("Black Kyurem")
                    && !pokemon.Name.Contains("White Kyurem")
                    // Keldeo Forms
                    && !pokemon.Name.Contains("Keldeo")
                    // Meloetta Forms
                    && !pokemon.Name.Contains("Meloetta")
                    // Greninja Forms
                    && !pokemon.Name.Contains("Ash-Greninja")
                    // Meowstic Forms
                    && !pokemon.Name.Contains("Meowstic")
                    // Aegislash Forms
                    && !pokemon.Name.Contains("Aegislash")
                    // Pumpkaboo/Gourgeist Forms
                    && !pokemon.Name.Contains("Pumpkaboo")
                    && !pokemon.Name.Contains("Gourgeist")
                    // Zygarde Forms
                    && !pokemon.Name.Contains("Zygarde")
                    // Hoopa Forms
                    && !pokemon.Name.Contains("Confined")
                    && !pokemon.Name.Contains("Unbound")
                    // Oricorio Forms
                    && !pokemon.Name.Contains("Baile Style")
                    && !pokemon.Name.Contains("Pom-Pom Style")
                    && !pokemon.Name.Contains("Pa'u Style")
                    && !pokemon.Name.Contains("Sensu Style")
                    // Rockruff Forms
                    && !pokemon.Name.Contains("Own Tempo")
                    // Lycanroc Forms
                    && !pokemon.Name.Contains("Midday Form")
                    && !pokemon.Name.Contains("Midnight Form")
                    && !pokemon.Name.Contains("Dusk Form")
                    // Wishiwashi Forms
                    && !pokemon.Name.Contains("Solo Form")
                    && !pokemon.Name.Contains("School Form")
                    // Minior Forms
                    && !pokemon.Name.Contains("Meteor Form")
                    && !pokemon.Name.Contains("Core Form")
                    // Necrozma Forms
                    && !pokemon.Name.Contains("Dusk Mane")
                    && !pokemon.Name.Contains("Dawn Wings")
                    && !pokemon.Name.Contains("Ultra Necrozma")
                    // Toxtricity Forms
                    && !pokemon.Name.Contains("Amped Form")
                    && !pokemon.Name.Contains("Low Key Form")
                    // Eiscue Forms
                    && !pokemon.Name.Contains("Ice Face")
                    && !pokemon.Name.Contains("Noice Face")
                    // Indeedee Forms
                    && !pokemon.Name.Contains("Indeedee Male")
                    && !pokemon.Name.Contains("Indeedee Female")
                    // Morpeko Forms
                    && !pokemon.Name.Contains("Full Belly Mode")
                    && !pokemon.Name.Contains("Hangry Mode")
                    // Zacian/Zamazenta Forms
                    && !pokemon.Name.Contains("Hero of Many Battles")
                    // Zacian Forms
                    && !pokemon.Name.Contains("Crowned Sword")
                    // Zamazenta Forms
                    && !pokemon.Name.Contains("Crowned Shield")
                    // Eternatus Forms
                    && !pokemon.Name.Contains("Eternamax")
                    // Urshifu Forms
                    && !pokemon.Name.Contains("Single Strike Style")
                    && !pokemon.Name.Contains("Rapid Strike Style")
                    // Calyrex Forms
                    && !pokemon.Name.Contains("Ice Rider")
                    && !pokemon.Name.Contains("Shadow Rider")
                    //Ursaluna Forms
                    && !pokemon.Name.Contains("Bloodmoon")
                    // Basculegion Forms
                    && !pokemon.Name.Contains("Basculegion Male")
                    && !pokemon.Name.Contains("Basculegion Female")
                    // Enamorus Forms
                    && !pokemon.Name.Contains("Incarnate Forme")
                    && !pokemon.Name.Contains("Therian Forme")
                    // Oinkologne Forms
                    && !pokemon.Name.Contains("Oinkologne Male")
                    && !pokemon.Name.Contains("Oinkologne Female")
                    // Maushold Forms
                    && !pokemon.Name.Contains("Family of Three")
                    && !pokemon.Name.Contains("Family of Four")
                    // Squawkabilly Forms
                    && !pokemon.Name.Contains("Green Plumage")
                    && !pokemon.Name.Contains("Blue Plumage")
                    && !pokemon.Name.Contains("Yellow Plumage")
                    && !pokemon.Name.Contains("White Plumage")
                    // Palafin Forms
                    && !pokemon.Name.Contains("Zero Form")
                    && !pokemon.Name.Contains("Hero Form")
                    // Tatsugiri Forms
                    && !pokemon.Name.Contains("Curly Form")
                    && !pokemon.Name.Contains("Droopy Form")
                    && !pokemon.Name.Contains("Stretchy Form")
                    // Dudunsparce Forms
                    && !pokemon.Name.Contains("Two-Segment Form")
                    && !pokemon.Name.Contains("Three-Segment Form")
                    // Gimmighoul Forms
                    && !pokemon.Name.Contains("Chest Form")
                    && !pokemon.Name.Contains("Roaming Form")
                    // Ogerpon Forms
                    && !pokemon.Name.Contains("Teal Mask")
                    && !pokemon.Name.Contains("Wellspring Mask")
                    && !pokemon.Name.Contains("Hearthflame Mask")
                    && !pokemon.Name.Contains("Cornerstone Mask")
                    // Terapagos Forms
                    && !pokemon.Name.Contains("Normal Form")
                    && !pokemon.Name.Contains("Terastal Form")
                    && !pokemon.Name.Contains("Stellar Form")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Megas (Do we need this list, is Mega an actual static form or just a battle form?)
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_MEGAS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Mega "))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} Mega {pokemon.Name.Split("Mega")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Alolan Regional Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_ALOLAN[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Alolan"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Alolan")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Galarian Regional Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_GALARIAN[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Galarian")
                    // There are multiple forms of Darmanitan both non-Galarian and Galarian variations
                    // So we just skip Darmanitan here and handle it later
                    && !pokemon.Name.Contains("Darmanitan")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Galarian")[0].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Hisuian Regional Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_HISUIAN[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Hisuian"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Hisuian")[0].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Paldean Regional Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_PALDEAN[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Paldean"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Paldean")[0].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Tauros Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_TAUROS_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Combat Breed")
                    || pokemon.Name.Contains("Blaze Breed")
                    || pokemon.Name.Contains("Aqua Breed")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Replace("Tauros ", "").Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Partner Forms (Only applicable to Gen 7 Let's Go Pikachu/Eevee)
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_PARTNER_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Partner"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Partner")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Castform Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_CASTFORM_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Sunny Form")
                    || pokemon.Name.Contains("Rainy Form")
                    || pokemon.Name.Contains("Snowy Form")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Replace("Castform ", "").Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Primal Forms (Kyogre and Groudon)
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_PRIMAL_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Primal"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} Primal {pokemon.Name.Split("Primal")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Deoxys Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_DEOXYS_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Deoxys"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Deoxys")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Burmy/Wormadam Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_BURMY_WORMADAM_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Burmy")
                    || pokemon.Name.Contains("Wormadam")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Rotom Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_ROTOM_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Heat Rotom")
                    || pokemon.Name.Contains("Wash Rotom")
                    || pokemon.Name.Contains("Frost Rotom")
                    || pokemon.Name.Contains("Fan Rotom")
                    || pokemon.Name.Contains("Mow Rotom")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Rotom")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Dialga/Palkia/Giratina Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_DIALGA_PALKIA_GIRATINA_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Origin Forme")
                    // Giratina Form
                    || pokemon.Name.Contains("Altered Forme")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Shaymin Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_SHAYMIN_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Shaymin"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Shaymin")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Basculin Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_BASCULIN_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Basculin"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Basculin")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Darmanitan Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_DARMANITAN_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Darmanitan")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Darmanitan")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Tornadus/Thundurus/Landorus Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_TORNADUS_THUNDURUS_LANDORUS_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Tornadus")
                    || pokemon.Name.Contains("Thundurus")
                    || pokemon.Name.Contains("Landorus")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Kyurem Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_KYUREM_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Black Kyurem")
                    || pokemon.Name.Contains("White Kyurem")
                )
                {
                    var nameText = string.Empty;
                    if (pokemon.Name.Contains("Black")) { nameText = "Black Kyurem"; }
                    else if (pokemon.Name.Contains("White")) { nameText = "White Kyurem"; }
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {nameText}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Keldeo Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_KELDEO_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Keldeo"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Keldeo")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Meloetta Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_MELOETTA_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Meloetta"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Meloetta")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Greninja Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_ASH_GRENINJA_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Ash-Greninja"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} Ash-Greninja";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Meowstic Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_MEOWSTIC_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Meowstic"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Meowstic")[1]}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Aegislash Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_AEGISLASH_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Aegislash"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Aegislash")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Pumpkaboo/Gourgeist Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_PUMPKABOO_GOURGEIST_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Pumpkaboo")
                    || pokemon.Name.Contains("Gourgeist")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Zygarde Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_ZYGARDE_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Zygarde"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Zygarde")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Hoopa Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_HOOPA_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Hoopa"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {"Hoopa " + pokemon.Name.Split("Hoopa")[2].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Oricorio Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_ORICORIO_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Oricorio"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Oricorio")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Rockruff Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_ROCKRUFF_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Own Tempo"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Rockruff")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Lycanroc Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_LYCANROC_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Lycanroc"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Lycanroc")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Wishiwashi Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_WISHIWASHI_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Wishiwashi"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Wishiwashi")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Minior Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_MINIOR_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Minior"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Minior")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Necrozma Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_NECROZMA_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Dusk Mane")
                    || pokemon.Name.Contains("Dawn Wings")
                    || pokemon.Name.Contains("Ultra Necrozma")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Necrozma")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Toxtricity Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_TOXTRICITY_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Toxtricity"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Toxtricity")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Eiscue Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_EISCUE_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Eiscue"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Eiscue")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Indeedee Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_INDEEDEE_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Indeedee"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Indeedee")[1]}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Morpeko Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_MORPEKO_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Morpeko"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Morpeko")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Zacian/Zamazenta Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_ZACIAN_ZAMAZENTA_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Hero of Many Battles")
                    // Zacian Only
                    || pokemon.Name.Contains("Crowned Sword")
                    // Zamazenta Only
                    || pokemon.Name.Contains("Crowned Shield")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Eternatus Forms  (Is this necessary? I believe this pokemon can only be caught by cheating or exploits, but fine)
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_ETERNATUS_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Eternamax"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Eternatus")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Urshifu Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_URSHIFU_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Urshifu"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Urshifu")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Calyrex Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_CALYREX_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Ice Rider")
                    || pokemon.Name.Contains("Shadow Rider")
                )
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Calyrex")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Ursaluna Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_URSALUNA_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Bloodmoon"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Ursaluna")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Basculegion Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_BASCULEGION_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Basculegion"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Basculegion")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Enamorus Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_ENAMORUS_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Enamorus"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Enamorus")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Oinkologne Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_OINKOLOGNE_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Oinkologne"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Oinkologne")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Maushold Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_MAUSHOLD_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Maushold"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Maushold")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Squawkabilly Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_SQUAWKABILLY_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Squawkabilly"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Squawkabilly")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Palafin Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_PALAFIN_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Palafin"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Palafin")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Tatsugiri Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_TATSUGIRI_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Tatsugiri"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Tatsugiri")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Dudunsparce Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_DUDUNSPARCE_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Dudunsparce"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Dudunsparce")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Gimmighoul Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_GIMMIGHOUL_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Gimmighoul"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Gimmighoul")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Ogerpon Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_OGERPON_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Ogerpon"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Ogerpon")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            // Terapagos Forms
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("static const BaseStats BASE_STATS_TABLE_TERAPAGOS_FORMS[] = {");
            for (int i = 0; i < pokemonList.Count; i++)
            {
                var pokemon = pokemonList[i];
                if (pokemon.Name.Contains("Terapagos"))
                {
                    var stats = pokemon.Stats;
                    var comment = pokemon.Id == 0
                        ? "  // 0 (placeholder)"
                        : $"  // {pokemon.Id} {pokemon.Name.Split("Terapagos")[1].Trim()}";

                    stringBuilder.AppendLine($"    {{ {pokemon.Id,3}, {stats.Hp,3}, {stats.Atk,3}, {stats.Def,3}, {stats.SpA,3}, {stats.SpD,3}, {stats.Spe,3} }},{comment}");
                }
            }
            stringBuilder.AppendLine("};");

            return stringBuilder.ToString();
        }
    }
}