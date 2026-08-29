namespace SixKeyToolbox.Models;

public class DanSection
{
	public required string Name { get; set; }
	public int StartMilliseconds { get; set; } = 0;
	public required int NoteCount { get; set; }
	public required int LNCount { get; set; }
	public int GetTotalCount(bool isScoreV2) => this.NoteCount + (this.LNCount * (isScoreV2 ? 2 : 1));
}
public class DanDefinition
{
	// oh god, why am i embedding this
	// hope i didn't make a mistake in the numbers
	public static List<DanDefinition> Defaults =>
	[
		new()
		{
			Name = "6k Regular 11th Dan (celestial)",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 2652, LNCount = 3 },
				new() { Name = "Tech", StartMilliseconds = 115551, NoteCount = 3144, LNCount = 15 },
				new() { Name = "Stamina", StartMilliseconds = 274451, NoteCount = 3341, LNCount = 7 },
				new() { Name = "Speed", StartMilliseconds = 409051, NoteCount = 3242, LNCount = 11 }
			]
		},
		new()
		{
			Name = "6k Regular 10th Dan (terra, v3)",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 2627, LNCount = 0 },
				new() { Name = "Tech", StartMilliseconds = 107495, NoteCount = 2939, LNCount = 18 },
				new() { Name = "Stamina", StartMilliseconds = 267895, NoteCount = 3481, LNCount = 0 },
				new() { Name = "Speed", StartMilliseconds = 420495, NoteCount = 2630, LNCount = 18 }
			]
		},
		new()
		{
			Name = "6k Regular 10th Dan (terra, v2)",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 3187, LNCount = 12 },
				new() { Name = "Tech", StartMilliseconds = 165056, NoteCount = 2734, LNCount = 1 },
				new() { Name = "Stamina", StartMilliseconds = 306556, NoteCount = 2942, LNCount = 0 },
				new() { Name = "Speed", StartMilliseconds = 433756, NoteCount = 2572, LNCount = 0 }
			]
		},
		new()
		{
			Name = "6k Regular 9th Dan",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 2958, LNCount = 0 },
				new() { Name = "Tech", StartMilliseconds = 144083, NoteCount = 2390, LNCount = 17 },
				new() { Name = "Stamina", StartMilliseconds = 266383, NoteCount = 2775, LNCount = 5 },
				new() { Name = "Speed", StartMilliseconds = 406883, NoteCount = 2970, LNCount = 5 }
			]
		},
		new()
		{
			Name = "6k Regular 8th Dan",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 2683, LNCount = 0 },
				new() { Name = "Tech", StartMilliseconds = 126329, NoteCount = 2433, LNCount = 16 },
				new() { Name = "Stamina", StartMilliseconds = 251848, NoteCount = 3001, LNCount = 0 },
				new() { Name = "Speed", StartMilliseconds = 390110, NoteCount = 2016, LNCount = 36 }
			]
		},
		new()
		{
			Name = "6k Regular 7th Dan",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 2612, LNCount = 2 },
				new() { Name = "Tech", StartMilliseconds = 119951, NoteCount = 2117, LNCount = 3 },
				new() { Name = "Stamina", StartMilliseconds = 238451, NoteCount = 2751, LNCount = 1 },
				new() { Name = "Speed", StartMilliseconds = 365551, NoteCount = 2775, LNCount = 7 }
			]
		},
		new()
		{
			Name = "6k Regular 4th Dan",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 2235, LNCount = 16 },
				new() { Name = "Tech", StartMilliseconds = 140170, NoteCount = 2347, LNCount = 12 },
				new() { Name = "Stamina", StartMilliseconds = 294870, NoteCount = 2316, LNCount = 0 },
				new() { Name = "Speed", StartMilliseconds = 432370, NoteCount = 2002, LNCount = 0 }
			]
		},
		new()
		{
			Name = "6k Regular 5th Dan",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 2327, LNCount = 7 },
				new() { Name = "Tech", StartMilliseconds = 154685, NoteCount = 2315, LNCount = 1 },
				new() { Name = "Stamina", StartMilliseconds = 293885, NoteCount = 2195, LNCount = 1 },
				new() { Name = "Speed", StartMilliseconds = 419385, NoteCount = 2409, LNCount = 0 }
			]
		},
		new()
		{
			Name = "6k Regular 6th Dan",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 2751, LNCount = 0 },
				new() { Name = "Tech", StartMilliseconds = 139374, NoteCount = 2374, LNCount = 6 },
				new() { Name = "Stamina", StartMilliseconds = 299074, NoteCount = 2947, LNCount = 4 },
				new() { Name = "Speed", StartMilliseconds = 444574, NoteCount = 2033, LNCount = 20 }
			]
		},
		new()
		{
			Name = "6k Regular 1st Dan",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 1512, LNCount = 8 },
				new() { Name = "Tech", StartMilliseconds = 131392, NoteCount = 1700, LNCount = 5 },
				new() { Name = "Stamina", StartMilliseconds = 261739, NoteCount = 1594, LNCount = 1 },
				new() { Name = "Speed", StartMilliseconds = 397745, NoteCount = 1372, LNCount = 13 }
			]
		},
		new()
		{
			Name = "6k Regular 2nd Dan",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 1663, LNCount = 14 },
				new() { Name = "Tech", StartMilliseconds = 116457, NoteCount = 1392, LNCount = 4 },
				new() { Name = "Stamina", StartMilliseconds = 223850, NoteCount = 1820, LNCount = 0 },
				new() { Name = "Speed", StartMilliseconds = 348473, NoteCount = 1588, LNCount = 46 }
			]
		},
		new()
		{
			Name = "6k Regular 3rd Dan",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 1961, LNCount = 21 },
				new() { Name = "Tech", StartMilliseconds = 127067, NoteCount = 1686, LNCount = 10 },
				new() { Name = "Stamina", StartMilliseconds = 239067, NoteCount = 2090, LNCount = 0 },
				new() { Name = "Speed", StartMilliseconds = 360067, NoteCount = 1566, LNCount = 24 }
			]
		},
		new()
		{
			Name = "6k Regular Start Dan",
			Sections =
			[
				new() { Name = "Jack", StartMilliseconds = 0, NoteCount = 1174, LNCount = 11 },
				new() { Name = "Tech", StartMilliseconds = 102727, NoteCount = 1580, LNCount = 3 },
				new() { Name = "Stamina", StartMilliseconds = 243640, NoteCount = 1276, LNCount = 13 },
				new() { Name = "Speed", StartMilliseconds = 365246, NoteCount = 1285, LNCount = 14 }
			]
		},
		new()
		{
			Name = "6k LN 0th Dan (start)",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 259, LNCount = 216 },
				new() { Name = "Speed", StartMilliseconds = 94347, NoteCount = 389, LNCount = 243 },
				new() { Name = "Inverse", StartMilliseconds = 198547, NoteCount = 229, LNCount = 201 },
				new() { Name = "Release", StartMilliseconds = 306547, NoteCount = 286, LNCount = 341 }
			]
		},
		new()
		{
			Name = "6k LN 1st Dan",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 885, LNCount = 553 },
				new() { Name = "Speed", StartMilliseconds = 147838, NoteCount = 545, LNCount = 363 },
				new() { Name = "Inverse", StartMilliseconds = 247038, NoteCount = 78, LNCount = 319 },
				new() { Name = "Release", StartMilliseconds = 362938, NoteCount = 457, LNCount = 191 }
			]
		},
		new()
		{
			Name = "6k LN 2nd Dan",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 820, LNCount = 373 },
				new() { Name = "Speed", StartMilliseconds = 152042, NoteCount = 602, LNCount = 271 },
				new() { Name = "Inverse", StartMilliseconds = 251442, NoteCount = 39, LNCount = 348 },
				new() { Name = "Release", StartMilliseconds = 390942, NoteCount = 381, LNCount = 334 }
			]
		},
		new()
		{
			Name = "6k LN 3rd Dan",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 617, LNCount = 1054 },
				new() { Name = "Speed", StartMilliseconds = 156591, NoteCount = 1426, LNCount = 563 },
				new() { Name = "Inverse", StartMilliseconds = 327948, NoteCount = 25, LNCount = 501 },
				new() { Name = "Release", StartMilliseconds = 507791, NoteCount = 440, LNCount = 649 }
			]
		},
		new()
		{
			Name = "6k LN 4th Dan",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 800, LNCount = 569 },
				new() { Name = "Speed", StartMilliseconds = 103445, NoteCount = 419, LNCount = 1272 },
				new() { Name = "Inverse", StartMilliseconds = 226945, NoteCount = 11, LNCount = 368 },
				new() { Name = "Release", StartMilliseconds = 354445, NoteCount = 561, LNCount = 699 }
			]
		},
		new()
		{
			Name = "6k LN 5th Dan",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 943, LNCount = 942 },
				new() { Name = "Speed", StartMilliseconds = 150620, NoteCount = 641, LNCount = 1224 },
				new() { Name = "Inverse", StartMilliseconds = 276720, NoteCount = 8, LNCount = 772 },
				new() { Name = "Release", StartMilliseconds = 415820, NoteCount = 518, LNCount = 1429 }
			]
		},
		new()
		{
			Name = "6k LN 6th Dan",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 1024, LNCount = 1123 },
				new() { Name = "Speed", StartMilliseconds = 128586, NoteCount = 820, LNCount = 1187 },
				new() { Name = "Inverse", StartMilliseconds = 276486, NoteCount = 6, LNCount = 857 },
				new() { Name = "Release", StartMilliseconds = 376186, NoteCount = 494, LNCount = 976 }
			]
		},
		new()
		{
			Name = "6k LN 7th Dan",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 743, LNCount = 987 },
				new() { Name = "Speed", StartMilliseconds = 128818, NoteCount = 463, LNCount = 1020 },
				new() { Name = "Inverse", StartMilliseconds = 229618, NoteCount = 48, LNCount = 1749 },
				new() { Name = "Release", StartMilliseconds = 382918, NoteCount = 649, LNCount = 875 }
			]
		},
		new()
		{
			Name = "6k LN 8th Dan",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 990, LNCount = 970 },
				new() { Name = "Speed", StartMilliseconds = 113784, NoteCount = 1187, LNCount = 1152 },
				new() { Name = "Inverse", StartMilliseconds = 242484, NoteCount = 252, LNCount = 1994 },
				new() { Name = "Release", StartMilliseconds = 408284, NoteCount = 412, LNCount = 1083 }
			]
		},
		new()
		{
			Name = "6k LN 9th Dan",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 857, LNCount = 1268 },
				new() { Name = "Speed", StartMilliseconds = 127713, NoteCount = 836, LNCount = 1147 },
				new() { Name = "Inverse", StartMilliseconds = 228713, NoteCount = 6, LNCount = 1432 },
				new() { Name = "Release", StartMilliseconds = 384513, NoteCount = 345, LNCount = 1318 }
			]
		},
		new()
		{
			Name = "6k LN 10th Dan (terra)",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 1027, LNCount = 1405 },
				new() { Name = "Speed", StartMilliseconds = 135827, NoteCount = 1111, LNCount = 2151 },
				new() { Name = "Inverse", StartMilliseconds = 300127, NoteCount = 106, LNCount = 2246 },
				new() { Name = "Release", StartMilliseconds = 440827, NoteCount = 0, LNCount = 2203 }
			]
		},
		new()
		{
			Name = "6k LN 11th Dan (celestial)",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 1380, LNCount = 1439 },
				new() { Name = "Speed", StartMilliseconds = 138461, NoteCount = 370, LNCount = 2371 },
				new() { Name = "Inverse", StartMilliseconds = 253561, NoteCount = 36, LNCount = 2815 },
				new() { Name = "Release", StartMilliseconds = 412361, NoteCount = 4, LNCount = 2385 }
			]
		},
		new()
		{
			Name = "6k LN 12th Dan (mystery)",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 370, LNCount = 2024 },
				new() { Name = "Speed", StartMilliseconds = 131371, NoteCount = 391, LNCount = 2551 },
				new() { Name = "Inverse", StartMilliseconds = 254371, NoteCount = 12, LNCount = 2120 },
				new() { Name = "Release", StartMilliseconds = 395471, NoteCount = 0, LNCount = 2149 }
			]
		},
		new()
		{
			Name = "6k LN 13th Dan (nihility)",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 949, LNCount = 2470 },
				new() { Name = "Speed", StartMilliseconds = 189689, NoteCount = 531, LNCount = 2218 },
				new() { Name = "Inverse", StartMilliseconds = 312189, NoteCount = 43, LNCount = 2571 },
				new() { Name = "Release", StartMilliseconds = 460289, NoteCount = 4, LNCount = 2780 }
			]
		},
		new()
		{
			Name = "6k LN 14th Dan (finish)",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 525, LNCount = 3319 },
				new() { Name = "Speed", StartMilliseconds = 144052, NoteCount = 634, LNCount = 2844 },
				new() { Name = "Inverse", StartMilliseconds = 276852, NoteCount = 40, LNCount = 3091 },
				new() { Name = "Release", StartMilliseconds = 412952, NoteCount = 11, LNCount = 2883 }
			]
		},
		new()
		{
			Name = "6k LN G-th Dan (icy, meme dan)",
			Sections =
			[
				new() { Name = "Hybrid", StartMilliseconds = 0, NoteCount = 419, LNCount = 806 },
				new() { Name = "Speed", StartMilliseconds = 106973, NoteCount = 398, LNCount = 1345 },
				new() { Name = "Inverse", StartMilliseconds = 231273, NoteCount = 0, LNCount = 719 },
				new() { Name = "Release", StartMilliseconds = 349073, NoteCount = 126, LNCount = 1014 }
			]
		}
	];

	public required string Name { get; set; }
	public required List<DanSection> Sections { get; set; }
	public int GetTotalCount(bool isScoreV2) => this.Sections.Sum(s => s.GetTotalCount(isScoreV2));
}
