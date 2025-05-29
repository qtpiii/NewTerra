using System.Collections.Generic;
using DevInterface;
using Pom;
using RWCustom;
using UnityEngine;

namespace NewTerra.DayCycle;

public class DayCyclePanel : Panel, IDevUISignals
{
	public PalettesPanel mainPalettesPanel;
	public PalettesPanel fadePalettesPanel;
	public EffectColorPanel effectColorAsPanel;
	public EffectColorPanel effectColorBsPanel;

	public PercentageSlider timeSlider;
	public Button timeOverrideToggler;

	public DayCycleExtensions.RoomSettingsExtension RoomSettingsExt => DayCycleExtensions.settingsExtensionTable.GetOrCreateValue(RoomSettings);
	
	public DayCyclePanel(DevUI owner, string id, DevUINode parentNode, Vector2 pos) : base(owner, id, parentNode, pos, new Vector2(210f, 5f), "DAYCYCLE")
	{
		Vector2 nodePos = new Vector2(5f, 5f);
		
		subNodes.Add(timeSlider = new PercentageSlider(owner, "DayCycle_Time", this, nodePos, "time", RoomSettingsExt.time));
		nodePos.y += 20f;
		size.y += 20f;
		subNodes.Add(timeOverrideToggler = new Button(owner, "Toggle_Time_Override", this, nodePos, 150f, ""));
		nodePos.y += 40f;
		size.y += 40f;
		
		subNodes.Add(new Button(owner, "Toggle_Effect_Color_Bs_Panel", this, nodePos, 190f, "toggle effect color b panel"));
		nodePos.y += 20f;
		size.y += 20f;
		subNodes.Add(new Button(owner, "Toggle_Effect_Color_As_Panel", this, nodePos, 190f, "toggle effect color a panel"));
		nodePos.y += 20f;
		size.y += 20f;
		subNodes.Add(new Button(owner, "Toggle_Fade_Palettes_Panel", this, nodePos, 190f, "toggle fade palettes panel"));
		nodePos.y += 20f;
		size.y += 20f;
		subNodes.Add(new Button(owner, "Toggle_Main_Palettes_Panel", this, nodePos, 190f, "toggle main palettes panel"));
		nodePos.y += 20f;
		size.y += 20f;
		
		/*
		// debugviz
		const int segments = 50;
		fSprites.Add(TriangleMesh.MakeLongMesh(segments, false, true));
		owner.placedObjectsContainer.AddChild(fSprites[^1]);
		fSprites[^1].color = Color.yellow;
		fSprites.Add(TriangleMesh.MakeLongMesh(segments, false, true));
		owner.placedObjectsContainer.AddChild(fSprites[^1]);
		fSprites[^1].color = Color.red;
		fSprites.Add(TriangleMesh.MakeLongMesh(segments, false, true));
		owner.placedObjectsContainer.AddChild(fSprites[^1]);
		fSprites[^1].color = Color.green;
		fSprites.Add(TriangleMesh.MakeLongMesh(segments, false, true));
		owner.placedObjectsContainer.AddChild(fSprites[^1]);
		fSprites[^1].color = Color.magenta;

		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < segments; j++)
			{
				const float width = 800f;
				const float height = 100f;
				Vector2 currPos = new Vector2((float)j / segments * width, RoomSettingsExt.PaletteIntensityAtTime((float)j / segments)[i] * height);
				Vector2 nextPos = new Vector2(((float)j + 1) / segments * width, RoomSettingsExt.PaletteIntensityAtTime(((float)j + 1) / segments)[i] * height);
				Vector2 perpendicular = Custom.PerpendicularVector(currPos, nextPos);
				
				((TriangleMesh)fSprites[^(i + 1)]).MoveVertice(j * 4 + 0, currPos - perpendicular);
				((TriangleMesh)fSprites[^(i + 1)]).MoveVertice(j * 4 + 1, currPos + perpendicular);
				((TriangleMesh)fSprites[^(i + 1)]).MoveVertice(j * 4 + 2, nextPos - perpendicular);
				((TriangleMesh)fSprites[^(i + 1)]).MoveVertice(j * 4 + 3, nextPos + perpendicular);
			}
		}
		*/
	}

	public override void Update()
	{
		base.Update();
		
		RoomSettingsExt.time = timeSlider.value;

		timeOverrideToggler.Text = $"override time: {RoomSettingsExt.timeOverride}";
	}

	public void Signal(DevUISignalType type, DevUINode sender, string message)
	{
		if (type == DevUISignalType.ButtonClick)
		{
			if (sender.IDstring == "Toggle_Main_Palettes_Panel")
			{
				if (mainPalettesPanel == null)
				{
					mainPalettesPanel = new PalettesPanel(owner, this, false);
					subNodes.Add(mainPalettesPanel);
				}
				else
				{
					mainPalettesPanel.ClearSprites();
					subNodes.Remove(mainPalettesPanel);
					mainPalettesPanel = null;
				}
			}
			if (sender.IDstring == "Toggle_Fade_Palettes_Panel")
			{
				if (fadePalettesPanel == null)
				{
					fadePalettesPanel = new PalettesPanel(owner, this, true);
					subNodes.Add(fadePalettesPanel);
				}
				else
				{
					fadePalettesPanel.ClearSprites();
					subNodes.Remove(fadePalettesPanel);
					fadePalettesPanel = null;
				}
			}
			if (sender.IDstring == "Toggle_Effect_Color_As_Panel")
			{
				if (effectColorAsPanel == null)
				{
					effectColorAsPanel = new EffectColorPanel(owner, this, true);
					subNodes.Add(effectColorAsPanel);
				}
				else
				{
					effectColorAsPanel.ClearSprites();
					subNodes.Remove(effectColorAsPanel);
					effectColorAsPanel = null;
				}
			}
			if (sender.IDstring == "Toggle_Effect_Color_Bs_Panel")
			{
				if (effectColorBsPanel == null)
				{
					effectColorBsPanel = new EffectColorPanel(owner, this, false);
					subNodes.Add(effectColorBsPanel);
				}
				else
				{
					effectColorBsPanel.ClearSprites();
					subNodes.Remove(effectColorBsPanel);
					effectColorBsPanel = null;
				}
			}

			if (sender == timeOverrideToggler)
			{
				RoomSettingsExt.timeOverride = !RoomSettingsExt.timeOverride;
			}
		}
	}
	
	public class PalettesPanel : Panel
	{
		private List<PaletteControl> paletteControls = new();
		private List<PercentageSlider> amounts = new();

		public int lineSprite;
		
		public DayCyclePanel dayCyclePanel => parentNode as DayCyclePanel;

		public bool fades;
		
		public PalettesPanel(DevUI owner, DayCyclePanel parentNode, bool fades) : base(owner, $"DayCycle_{(fades ? "Fade" : "Main")}_Palettes_Panel", parentNode, new Vector2(0, 0), new Vector2(210f, 5f), fades ? "FADE PALETTES" : "MAIN PALETTES")
		{
			this.fades = fades;
			
			Vector2 nodePos = new Vector2(5f, 5f);
			
			AddAmountSlider(new PercentageSlider(owner, "DayCycle_Night_Palette_Amount" + (fades ? "_Fade" : ""), this, nodePos, "", parentNode.RoomSettingsExt.paletteIntensities[fades ? 1 : 0, 3]), ref nodePos);
			AddControl(new PaletteControl(owner, "DayCycle_Night_Palette" + (fades ? "_Fade" : ""), this, nodePos, "night palette", parentNode.RoomSettingsExt.palettes[fades ? 1 : 0, 3]), ref nodePos);
			
			AddAmountSlider(new PercentageSlider(owner, "DayCycle_Dusk_Palette_Amount" + (fades ? "_Fade" : ""), this, nodePos, "", parentNode.RoomSettingsExt.paletteIntensities[fades ? 1 : 0, 2]), ref nodePos);
			AddControl(new PaletteControl(owner, "DayCycle_Dusk_Palette" + (fades ? "_Fade" : ""), this, nodePos, "dusk palette", parentNode.RoomSettingsExt.palettes[fades ? 1 : 0, 2]), ref nodePos);
			
			AddAmountSlider(new PercentageSlider(owner, "DayCycle_Day_Palette_Amount" + (fades ? "_Fade" : ""), this, nodePos, "", parentNode.RoomSettingsExt.paletteIntensities[fades ? 1 : 0, 1]), ref nodePos);
			AddControl(new PaletteControl(owner, "DayCycle_Day_Palette" + (fades ? "_Fade" : ""), this, nodePos, "day palette", parentNode.RoomSettingsExt.palettes[fades ? 1 : 0, 1]), ref nodePos);
			
			AddAmountSlider(new PercentageSlider(owner, "DayCycle_Dawn_Palette_Amount" + (fades ? "_Fade" : ""), this, nodePos, "", parentNode.RoomSettingsExt.paletteIntensities[fades ? 1 : 0, 0]), ref nodePos);
			AddControl(new PaletteControl(owner, "DayCycle_Dawn_Palette" + (fades ? "_Fade" : ""), this, nodePos, "dawn palette", parentNode.RoomSettingsExt.palettes[fades ? 1 : 0, 0]), ref nodePos);
			
			fSprites.Add(new FSprite("pixel"));
			lineSprite = fSprites.Count - 1;
			owner.placedObjectsContainer.AddChild(fSprites[lineSprite]);
			fSprites[lineSprite].anchorY = 0f;

			pos.y -= size.y + 30;
		}

		public override void Refresh()
		{
			base.Refresh();
			MoveSprite(lineSprite, absPos + new Vector2(0, size.y + 20));
			fSprites[lineSprite].scaleY = (pos + new Vector2(0, size.y + 20)).magnitude;
			fSprites[lineSprite].rotation = Custom.VecToDeg(-(pos + new Vector2(0, size.y + 20)));

			dayCyclePanel.RoomSettingsExt.palettes[fades ? 1 : 0, 3] = paletteControls[0].value;
			dayCyclePanel.RoomSettingsExt.palettes[fades ? 1 : 0, 2] = paletteControls[1].value;
			dayCyclePanel.RoomSettingsExt.palettes[fades ? 1 : 0, 1] = paletteControls[2].value;
			dayCyclePanel.RoomSettingsExt.palettes[fades ? 1 : 0, 0] = paletteControls[3].value;
			dayCyclePanel.RoomSettingsExt.paletteIntensities[fades ? 1 : 0, 3] = amounts[0].value;
			dayCyclePanel.RoomSettingsExt.paletteIntensities[fades ? 1 : 0, 2] = amounts[1].value;
			dayCyclePanel.RoomSettingsExt.paletteIntensities[fades ? 1 : 0, 1] = amounts[2].value;
			dayCyclePanel.RoomSettingsExt.paletteIntensities[fades ? 1 : 0, 0] = amounts[3].value;
		}

		public void AddControl(PaletteControl node, ref Vector2 nodePos)
		{
			subNodes.Add(node);
			paletteControls.Add(node);
			size.y += 20f;
			nodePos.y += 20f;
		}
		
		public void AddAmountSlider(PercentageSlider node, ref Vector2 nodePos)
		{
			subNodes.Add(node);
			amounts.Add(node);
			size.y += 20f;
			nodePos.y += 20f;
		}
	}

	public class EffectColorPanel : Panel
	{
		private List<RGBSelectPanel.RGBSelectButton> colorPickerButtons = new();
		private List<PercentageSlider> amounts = new();
		
		public int lineSprite;

		public DayCyclePanel dayCyclePanel => parentNode as DayCyclePanel;
		
		public bool a;
		
		public EffectColorPanel(DevUI owner, DayCyclePanel parentNode, bool a) : base(owner, $"DayCycle_Effect_Color_{(a ? "A" : "B")}s_Panel", parentNode, new Vector2(0, 0), new Vector2(210f, 5f), $"EFFECT COLOR {(a ? "A" : "B")}S")
		{
			this.a = a;	
			
			Vector2 nodePos = new Vector2(5f, 5f);
			
			AddAmountSlider(new PercentageSlider(owner, $"DayCycle_Night_Effect_Color_{(a ? "A" : "B")}_Amount", this, nodePos, "", parentNode.RoomSettingsExt.effectColorIntensities[a ? 0 : 1, 3]), ref nodePos);
			AddColorPicker(new RGBSelectPanel.RGBSelectButton(owner, $"DayCycle_Night_Effect_Color_{(a ? "A" : "B")}_Button", this, nodePos, 60f, "night", parentNode.RoomSettingsExt.effectColors[a ? 0 : 1, 3], $"NIGHT EFFECT COLOR {(a ? "A" : "B")}"), ref nodePos);
			
			AddAmountSlider(new PercentageSlider(owner, $"DayCycle_Dusk_Effect_Color_{(a ? "A" : "B")}_Amount", this, nodePos, "", parentNode.RoomSettingsExt.effectColorIntensities[a ? 0 : 1, 2]), ref nodePos);
			AddColorPicker(new RGBSelectPanel.RGBSelectButton(owner, $"DayCycle_Dusk_Effect_Color_{(a ? "A" : "B")}_Button", this, nodePos, 60f, "dusk", parentNode.RoomSettingsExt.effectColors[a ? 0 : 1, 2], $"DAWN EFFECT COLOR {(a ? "A" : "B")}"), ref nodePos);
			
			AddAmountSlider(new PercentageSlider(owner, $"DayCycle_Day_Effect_Color_{(a ? "A" : "B")}_Amount", this, nodePos, "", parentNode.RoomSettingsExt.effectColorIntensities[a ? 0 : 1, 1]), ref nodePos);
			AddColorPicker(new RGBSelectPanel.RGBSelectButton(owner, $"DayCycle_Day_Effect_Color_{(a ? "A" : "B")}_Button", this, nodePos, 60f, "day", parentNode.RoomSettingsExt.effectColors[a ? 0 : 1, 1], $"DAY EFFECT COLOR {(a ? "A" : "B")}"), ref nodePos);
			
			AddAmountSlider(new PercentageSlider(owner, $"DayCycle_Dawn_Effect_Color_{(a ? "A" : "B")}_Amount", this, nodePos, "", parentNode.RoomSettingsExt.effectColorIntensities[a ? 0 : 1, 0]), ref nodePos);
			AddColorPicker(new RGBSelectPanel.RGBSelectButton(owner, $"DayCycle_Dawn_Effect_Color_{(a ? "A" : "B")}_Button", this, nodePos, 60f, "dawn", parentNode.RoomSettingsExt.effectColors[a ? 0 : 1, 0], $"DAWN EFFECT COLOR {(a ? "A" : "B")}"), ref nodePos);
			
			fSprites.Add(new FSprite("pixel"));
			lineSprite = fSprites.Count - 1;
			owner.placedObjectsContainer.AddChild(fSprites[lineSprite]);
			fSprites[lineSprite].anchorY = 0f;

			pos.y -= size.y + 30;
		}

		public override void Update()
		{
			base.Update();
			MoveSprite(lineSprite, absPos + new Vector2(0, size.y + 20));
			fSprites[lineSprite].scaleY = (pos + new Vector2(0, size.y + 20)).magnitude;
			fSprites[lineSprite].rotation = Custom.VecToDeg(-(pos + new Vector2(0, size.y + 20)));
			
			dayCyclePanel.RoomSettingsExt.effectColors[a ? 0 : 1, 3] = colorPickerButtons[0].actualValue;
			dayCyclePanel.RoomSettingsExt.effectColors[a ? 0 : 1, 2] = colorPickerButtons[1].actualValue;
			dayCyclePanel.RoomSettingsExt.effectColors[a ? 0 : 1, 1] = colorPickerButtons[2].actualValue;
			dayCyclePanel.RoomSettingsExt.effectColors[a ? 0 : 1, 0] = colorPickerButtons[3].actualValue;
			dayCyclePanel.RoomSettingsExt.effectColorIntensities[a ? 0 : 1, 3] = amounts[0].value;
			dayCyclePanel.RoomSettingsExt.effectColorIntensities[a ? 0 : 1, 2] = amounts[1].value;
			dayCyclePanel.RoomSettingsExt.effectColorIntensities[a ? 0 : 1, 1] = amounts[2].value;
			dayCyclePanel.RoomSettingsExt.effectColorIntensities[a ? 0 : 1, 0] = amounts[3].value;
		}

		public void AddColorPicker(RGBSelectPanel.RGBSelectButton node, ref Vector2 nodePos)
		{
			subNodes.Add(node);
			colorPickerButtons.Add(node);
			size.y += 20f;
			nodePos.y += 20f;
		}
		
		public void AddAmountSlider(PercentageSlider node, ref Vector2 nodePos)
		{
			subNodes.Add(node);
			amounts.Add(node);
			size.y += 20f;
			nodePos.y += 20f;
		}
	}

	public class PercentageSlider(DevUI owner, string id, DevUINode parentNode, Vector2 pos, string title, float startVal) : Slider(owner, id, parentNode, pos, title, false, title == "" ? 0f : 65f)
	{
		public float value = startVal;
		
		public override void Refresh()
		{
			base.Refresh();

			NumberText = $"{Mathf.FloorToInt(value * 100)}%";
			
			RefreshNubPos(value);
		}

		public override void NubDragged(float nubPos)
		{
			value = nubPos;
			Refresh();
			parentNode.Refresh();
		}
	}

	public class PaletteControl(DevUI owner, string id, DevUINode parentNode, Vector2 pos, string title, int? startVal) : IntegerControl(owner, id, parentNode, pos, title)
	{
		public int? value = startVal;

		public override void Refresh()
		{
			if (value != null)
			{
				NumberLabelText = value.Value.ToString();
			}
			else
			{
				NumberLabelText = "N/A";
			}
			base.Refresh();
		}

		public override void Increment(int change)
		{
			if (value != null)
			{
				value += change;
				if (value < 0)
				{
					value = null;
				}
			}
			else
			{
				value = 0;
			}
			Refresh();
			parentNode.Refresh();
			DayCycleExtensions.cameraExtensionTable.GetOrCreateValue(owner.game.cameras[0]).ResetPaletteTextures(owner.game.cameras[0], ((PalettesPanel)parentNode).dayCyclePanel.RoomSettingsExt);
		}
	}
}
