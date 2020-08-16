#region Copyright & License Information
/*
 * Copyright 2019-2020 The OpenHV Developers (see CREDITS)
 * This file is part of OpenHV, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.HV.Traits
{
	public class TerrainTileAnimationInfo : TraitInfo, ILobbyCustomRulesIgnore
	{
		[FieldLoader.Require]
		public readonly ushort[] Tiles = null;

		[Desc("Average time (ticks) between animations.")]
		public readonly int[] Interval = { 7 * 25, 13 * 25 };

		[Desc("Delay (in ticks) before the first animation happens.")]
		public readonly int InitialDelay = 0;

		[FieldLoader.Require]
		[Desc("Which image to use.")]
		public readonly string Image;

		[FieldLoader.Require]
		[Desc("Which sequence to use.")]
		[SequenceReference("Image")]
		public readonly string Sequence;

		[FieldLoader.Require]
		[Desc("Which palette to use.")]
		[PaletteReference]
		public readonly string Palette;

		public override object Create(ActorInitializer init) { return new TerrainTileAnimation(init.Self, this); }
	}

	public class TerrainTileAnimation : ITick
	{
		readonly TerrainTileAnimationInfo info;
		readonly CPos[] cells;

		int ticks;

		public TerrainTileAnimation(Actor self, TerrainTileAnimationInfo info)
		{
			this.info = info;

			ticks = info.InitialDelay;

			var map = self.World.Map;
			cells = map.AllCells.Where(cell => info.Tiles.Contains(map.Tiles[cell].Type)).ToArray();
		}

		void ITick.Tick(Actor self)
		{
			if (!cells.Any())
				return;

			if (--ticks <= 0)
			{
				ticks = Common.Util.RandomDelay(self.World, info.Interval);

				var world = self.World;
				var position = world.Map.CenterOfCell(cells.Random(world.LocalRandom));
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(position, w, info.Image, info.Sequence, info.Palette)));
			}
		}
	}
}
