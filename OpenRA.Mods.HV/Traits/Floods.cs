#region Copyright & License Information
/*
 * Copyright 2019-2021 The OpenHV Developers (see CREDITS)
 * This file is part of OpenHV, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.HV.Traits
{
	[Desc("Lets the actor spread liquid tiles around it in straight lines.")]
	class FloodsInfo : ConditionalTraitInfo
	{
		[Desc("Speed in ticks between each wave.")]
		public readonly int Interval = 10;

		[Desc("After which amount of flooded tiles to stop traversing further.")]
		public readonly int MaxTiles = 10000;

		[Desc("Which tile ID to replace with which flooded variant")]
		public readonly Dictionary<ushort, ushort> ReplacementTiles = new Dictionary<ushort, ushort>();

		[Desc("Terrain type that should be converted albeit unpathable.")]
		public readonly string[] StopTerrainTypes = Array.Empty<string>();

		[FieldLoader.Require]
		[Desc("Locomotor to test pathability of barriers against.")]
		public readonly string Locomotor = null;

		public override object Create(ActorInitializer init) { return new Floods(init.Self, this); }
	}

	class Floods : ConditionalTrait<FloodsInfo>, ITick
	{
		readonly FloodsInfo info;
		readonly LiquidTerrainLayer liquidTerrainLayer;
		readonly List<CPos> floodedCells = new List<CPos>();
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly Map map;
		readonly DomainIndex domainIndex;
		readonly CPos origin;
		readonly Locomotor locomotor;

		public Floods(Actor self, FloodsInfo info)
			: base(info)
		{
			this.info = info;
			liquidTerrainLayer = self.World.WorldActor.Trait<LiquidTerrainLayer>();
			origin = self.Location;
			floodedCells.Add(origin);
			map = self.World.Map;
			domainIndex = self.World.WorldActor.Trait<DomainIndex>();
			locomotor = self.World.WorldActor.TraitsImplementing<Locomotor>().First(l => l.Info.Name == info.Locomotor);

			if (!(map.Rules.TerrainInfo is ITemplatedTerrainInfo terrainInfo))
				throw new InvalidDataException($"{nameof(Floods)} requires a template-based tileset.");

			this.terrainInfo = terrainInfo;
		}

		int ticks;
		bool steadyWaveFront;

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (steadyWaveFront)
				return;

			if (--ticks <= 0)
			{
				Flood();
				ticks = info.Interval;
			}
		}

		int previousWaveFront;

		public void Flood()
		{
			var waveFront = Util.ExpandFootprint(floodedCells, true)
				.Take(info.MaxTiles)
				.SkipWhile(p => !map.Contains(p) || liquidTerrainLayer.ContainsTile(p))
				.ToArray();

			if (previousWaveFront == waveFront.Length)
			{
				steadyWaveFront = true;
				return;
			}

			previousWaveFront = waveFront.Length;

			foreach (var cell in waveFront)
			{
				if (!info.StopTerrainTypes.Contains(map.GetTerrainInfo(cell).Type) && !domainIndex.IsPassable(origin, cell, locomotor))
					continue;

				var originalTile = map.Tiles[cell];
				var replacement = info.ReplacementTiles[originalTile.Type];
				var replacementTile = new TerrainTile(replacement, 0x00);
				liquidTerrainLayer.AddTile(cell, replacementTile);
				floodedCells.Add(cell);
			}
		}
	}
}
